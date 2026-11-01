// Production environment for PakEchtMee in one resource group, sized to stay within Azure's free grants:
// Container Apps (Consumption, scale to zero), Azure SQL Database (free offer, pauses instead of billing) and a
// capped Log Analytics workspace. Images come from GitHub Container Registry, so there is no registry cost.
// Deployed by .github/workflows/deploy.yml; one-time setup in docs/azure-hosting.md.

targetScope = 'resourceGroup'

@description('Azure region for all resources. West Europe accepts no new customers on this subscription, so North Europe it is.')
param location string = 'northeurope'

@description('Full image reference of the API, e.g. ghcr.io/owner/pakechtmee-api:<sha>.')
param apiImage string

@description('Full image reference of the frontend, e.g. ghcr.io/owner/pakechtmee-web:<sha>.')
param webImage string

@description('GitHub account that owns the GHCR packages and the pull token.')
param ghcrUsername string

@secure()
@description('Classic GitHub token with only read:packages, used by Container Apps to pull the private images.')
param ghcrPullToken string

@secure()
@description('Google OAuth client id.')
param googleClientId string

@secure()
@description('Google OAuth client secret.')
param googleClientSecret string

@secure()
@description('Secret that signs the NextAuth session cookie.')
param nextAuthSecret string

@secure()
@description('Shared HS256 key for the short-lived tokens the frontend sends to the API (at least 32 characters).')
param apiTokenKey string

// The SQL server name is global DNS, so it carries a suffix that is stable per resource group.
var suffix = uniqueString(resourceGroup().id)
var databaseName = 'pakechtmee'

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-pakechtmee'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    // About 4.5 GB a month at most, below the free 5 GB of ingestion.
    workspaceCapping: { dailyQuotaGb: json('0.15') }
  }
}

// The API's identity is also the database server's Entra administrator, so no SQL password exists anywhere.
resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-pakechtmee-api'
  location: location
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-pakechtmee-${suffix}'
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: apiIdentity.name
      sid: apiIdentity.properties.principalId
      tenantId: tenant().tenantId
      principalType: 'Application'
    }
  }
}

// 0.0.0.0 is Azure's convention for "Azure services only"; nothing on the internet can connect.
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // The free offer: 100,000 vCore seconds and 32 GB a month; when used up the database pauses until next month.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    requestedBackupStorageRedundancy: 'Local'
    zoneRedundant: false
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-pakechtmee'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

var registry = {
  server: 'ghcr.io'
  username: ghcrUsername
  passwordSecretRef: 'ghcr-pull-token'
}

var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};Authentication=Active Directory Managed Identity;User Id=${apiIdentity.properties.clientId};Encrypt=True;Connect Timeout=60'

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'api'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${apiIdentity.id}': {} }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      // Internal only: just the frontend, inside the environment, reaches the API. Plain HTTP stays inside it.
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      registries: [registry]
      secrets: [
        { name: 'ghcr-pull-token', value: ghcrPullToken }
        { name: 'api-token-key', value: apiTokenKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ConnectionStrings__pakechtmee', value: connectionString }
            { name: 'AZURE_CLIENT_ID', value: apiIdentity.properties.clientId }
            { name: 'Auth__TokenKey', secretRef: 'api-token-key' }
          ]
          probes: [
            // Startup migrates and seeds before the app listens; a paused database can take a minute to resume.
            {
              type: 'Startup'
              httpGet: { path: '/alive', port: 8080 }
              initialDelaySeconds: 5
              periodSeconds: 20
              failureThreshold: 10
            }
            {
              type: 'Liveness'
              httpGet: { path: '/alive', port: 8080 }
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health', port: 8080 }
              periodSeconds: 15
            }
          ]
        }
      ]
      // One replica at most: startup migrations then never run concurrently.
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
  dependsOn: [database, allowAzureServices]
}

// Known before the app exists, so NextAuth's URL and Google's redirect URI never depend on a second deployment.
var frontendUrl = 'https://web.${environment.properties.defaultDomain}'

resource web 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'web'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      // allowInsecure false: the platform redirects plain HTTP to HTTPS.
      ingress: {
        external: true
        targetPort: 3000
        transport: 'auto'
        allowInsecure: false
      }
      registries: [registry]
      secrets: [
        { name: 'ghcr-pull-token', value: ghcrPullToken }
        { name: 'google-client-id', value: googleClientId }
        { name: 'google-client-secret', value: googleClientSecret }
        { name: 'nextauth-secret', value: nextAuthSecret }
        { name: 'api-token-key', value: apiTokenKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'API_BASE_URL', value: 'http://${api.name}' }
            { name: 'NEXTAUTH_URL', value: frontendUrl }
            { name: 'NEXTAUTH_SECRET', secretRef: 'nextauth-secret' }
            { name: 'GOOGLE_CLIENT_ID', secretRef: 'google-client-id' }
            { name: 'GOOGLE_CLIENT_SECRET', secretRef: 'google-client-secret' }
            { name: 'API_TOKEN_KEY', secretRef: 'api-token-key' }
          ]
        }
      ]
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
}

output frontendUrl string = frontendUrl
output googleRedirectUri string = '${frontendUrl}/api/auth/callback/google'
