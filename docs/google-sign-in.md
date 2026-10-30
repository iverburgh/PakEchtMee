# Inloggen met Google instellen (lokaal)

PakEchtMee laat mensen inloggen met hun Google-account. Daarvoor is eenmalig een OAuth-client in Google Cloud nodig. Deze stappen doe je zelf; de code kan dit niet voor je regelen.

## 1. OAuth-client aanmaken

1. Ga naar <https://console.cloud.google.com/> en maak een project aan, bijvoorbeeld `PakEchtMee`.
2. Open **APIs & Services → OAuth consent screen** (in nieuwere consoles: **Google Auth Platform → Branding/Audience**).
   - User type: **External**.
   - App name: `PakEchtMee`, support e-mail: je eigen adres.
   - Scopes: alleen de standaard `openid`, `email` en `profile` zijn nodig.
3. Zolang de app in **Testing** staat, mogen alleen **test users** inloggen. Voeg daar elk Google-account toe dat je later aan een familie koppelt (maximaal 100). Zonder deze stap weigert Google de login nog voordat PakEchtMee eraan te pas komt.
4. Open **APIs & Services → Credentials → Create credentials → OAuth client ID**.
   - Application type: **Web application**.
   - Authorized JavaScript origins: `http://localhost:3000`
   - Authorized redirect URIs: `http://localhost:3000/api/auth/callback/google`
5. Kopieer de **Client ID** en het **Client secret**.

## 2. Aspire-parameters invullen

De AppHost verwacht twee parameters. Zet ze in de user secrets van de AppHost:

```bash
dotnet user-secrets set "Parameters:google-client-id" "<client-id>" --project src/AppHost
```

```bash
dotnet user-secrets set "Parameters:google-client-secret" "<client-secret>" --project src/AppHost
```

Je kunt ze ook invullen in het Aspire-dashboard: zolang ze ontbreken, toont het dashboard een melding om ze op te geven.

De twee andere geheimen, `nextauth-secret` en `api-token-key`, maakt de AppHost zelf aan bij de eerste start en bewaart ze in dezelfde user secrets.

## 3. Controleren

1. Start de app met `aspire run --project src/AppHost/AppHost.csproj`.
2. Open <http://localhost:3000>. Je komt op de inlogpagina.
3. Log in met `ivoverburgh@gmail.com`. Je ziet de uitjes van familie Verburgh en het tabblad **Beheer**.

## Problemen

- **`redirect_uri_mismatch`**: de redirect-URI in Google wijkt af. Hij moet exact `http://localhost:3000/api/auth/callback/google` zijn.
- **"Access blocked: app has not completed verification"**: het account staat niet als test user in het consent screen.
- **Poort 3000 is bezet**: stop het andere proces. Kies je een andere poort in `src/AppHost/AppHost.cs`, pas dan ook de redirect-URI in Google aan.
