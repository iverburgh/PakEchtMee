# PakEchtMee online hosten op Azure

Elke push op `main` wordt automatisch gebouwd, getest en uitgerold naar Azure door
[.github/workflows/deploy.yml](../.github/workflows/deploy.yml). Alle Azure-resources staan in
[infra/main.bicep](../infra/main.bicep):

| Resource | Wat | Kosten |
| --- | --- | --- |
| Container Apps-omgeving `cae-pakechtmee` (Consumption) | `web` (Next.js, publiek via HTTPS) en `api` (.NET, alleen intern) | Gratis tot 180.000 vCPU-s, 360.000 GiB-s en 2 miljoen requests per maand. Beide schalen naar 0 |
| Azure SQL Database `pakechtmee` (free offer) | Serverless, pauzeert na 60 minuten zonder gebruik | Gratis: 100.000 vCore-seconden en 32 GB per maand. Daarna pauzeert hij tot de volgende maand |
| Log Analytics `log-pakechtmee` | Logs van beide apps | Begrensd op 0,15 GB per dag, binnen de gratis 5 GB per maand |
| Managed identity `id-pakechtmee-api` | Waarmee de API inlogt op SQL; er bestaat geen SQL-wachtwoord | Gratis |
| GitHub Container Registry | De images `pakechtmee-api` en `pakechtmee-web` (privé) | Gratis |

Hieronder staat de **eenmalige** setup. Daarna is een push op `main` genoeg.

## 1. Azure: resourcegroep en resource providers

```powershell
az login
az account set --subscription "<subscription-id>"
az group create --name rg-pakechtmee --location northeurope

# Eenmalig per subscription; de pipeline zelf heeft hier geen rechten voor.
"Microsoft.App", "Microsoft.OperationalInsights", "Microsoft.Sql", "Microsoft.ManagedIdentity" |
  ForEach-Object { az provider register --namespace $_ }
```

## 2. Azure: identiteit voor GitHub (OpenID Connect, zonder secret)

```powershell
$subscriptionId = az account show --query id -o tsv
$tenantId = az account show --query tenantId -o tsv
$appId = az ad app create --display-name "pakechtmee-github" --query appId -o tsv
az ad sp create --id $appId

# Alleen Contributor op de eigen resourcegroep.
az role assignment create --assignee $appId --role Contributor `
  --scope "/subscriptions/$subscriptionId/resourceGroups/rg-pakechtmee"

# Vertrouw alleen de GitHub-environment "production" van deze repository.
@'
{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:iverburgh/PakEchtMee:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}
'@ | Set-Content federated.json
az ad app federated-credential create --id $appId --parameters federated.json
Remove-Item federated.json

"AZURE_CLIENT_ID=$appId"; "AZURE_TENANT_ID=$tenantId"; "AZURE_SUBSCRIPTION_ID=$subscriptionId"
```

## 3. GitHub: token waarmee Azure de images ophaalt

1. Ga naar GitHub → **Settings → Developer settings → Personal access tokens → Tokens (classic)** →
   **Generate new token (classic)**.
2. Naam: `pakechtmee-ghcr-pull`, scope **alleen `read:packages`**. Kies een vervaldatum en zet die in je
   agenda, of kies "No expiration".
3. Kopieer het token. Je hebt het in stap 4 nodig als `GHCR_PULL_TOKEN`.

## 4. GitHub: environment `production` met variabelen en secrets

In de repository: **Settings → Environments → New environment** → `production`.

**Variables** (niet geheim):

| Naam | Waarde |
| --- | --- |
| `AZURE_CLIENT_ID` | Uitvoer van stap 2 |
| `AZURE_TENANT_ID` | Uitvoer van stap 2 |
| `AZURE_SUBSCRIPTION_ID` | Uitvoer van stap 2 |
| `AZURE_RESOURCE_GROUP` | `rg-pakechtmee` |

**Secrets**:

| Naam | Waarde |
| --- | --- |
| `GOOGLE_CLIENT_ID` | Dezelfde OAuth-client als lokaal (zie [google-sign-in.md](google-sign-in.md)) |
| `GOOGLE_CLIENT_SECRET` | Idem |
| `NEXTAUTH_SECRET` | Nieuwe willekeurige waarde, zie hieronder |
| `API_TOKEN_KEY` | Nieuwe willekeurige waarde van minstens 32 tekens, zie hieronder |
| `GHCR_PULL_TOKEN` | Het token uit stap 3 |

Willekeurige waarden maak je zo, elke keer een nieuwe:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

Gebruik voor productie andere waarden dan lokaal. Zet ze nooit in de repository.

## 5. Eerste deployment

Push naar `main`, of start de workflow **Deploy** handmatig onder **Actions**. De samenvatting van de run toont
de URL, bijvoorbeeld `https://web.<iets>.northeurope.azurecontainerapps.io`.

GitHub maakt de packages `pakechtmee-api` en `pakechtmee-web` aan bij de eerste push. Mislukt het ophalen van
de images bij de allereerste run, start de job dan nog één keer opnieuw (**Re-run failed jobs**).

## 6. Google: productieadres toevoegen

In [Google Cloud Console](https://console.cloud.google.com/apis/credentials), bij dezelfde OAuth-client:

- **Authorised JavaScript origins**: `https://web.<iets>.northeurope.azurecontainerapps.io`
- **Authorised redirect URIs**: `https://web.<iets>.northeurope.azurecontainerapps.io/api/auth/callback/google`

Staat het consent screen nog in *Testing*, voeg dan iedere gebruiker toe als test user.

## 7. Controle

- Open de URL en log in als beheerder. Familie Verburgh en de algemene catalogus (22 categorieën) staan klaar.
  Seed-uitjes zijn er in productie niet.
- Na een tijd zonder gebruik duurt het eerste bezoek langer. Web, API en database starten dan op; dat kan
  ongeveer een minuut duren.

## Aanrader: alert op het gratis databasetegoed

Azure Portal → SQL database `pakechtmee` → **Alerts → Create alert rule** → signal **Free amount remaining**
→ voorwaarde *Less than* `10000` (10% van het maandtegoed) → actiegroep met je e-mailadres.

## Beheer

**Token verlopen.** De nieuwe versie kan de images dan niet ophalen; de vorige versie blijft draaien. Maak een
nieuw token (stap 3), werk het secret `GHCR_PULL_TOKEN` bij en start de laatste run opnieuw.

**Secret wijzigen.** Pas het secret in de GitHub-environment aan en start de workflow **Deploy** handmatig.

**Terugdraaien.** Kies een van deze twee:

- Revert de commit op `main`; de pipeline rolt de vorige code uit.
- Zet direct een eerder image terug. De images zijn getagd met de commit-SHA:

  ```powershell
  az containerapp update -n web -g rg-pakechtmee --image ghcr.io/iverburgh/pakechtmee-web:<vorige-sha>
  ```

  Doe hetzelfde voor `api` met `pakechtmee-api`. Databasemigraties draaien alleen vooruit.

**Logs.**

```powershell
az containerapp logs show -n api -g rg-pakechtmee --tail 100
```

Of gebruik Log Analytics `log-pakechtmee` in de portal.
