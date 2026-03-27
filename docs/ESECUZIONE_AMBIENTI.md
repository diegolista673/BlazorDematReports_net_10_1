# BlazorDematReports — Avvio in Development e Production

> **.NET 10** · **Blazor Server** · **SQL Server**
> `UserSecretsId`: `2ad40ed9-cd4a-4f74-99d0-122f1c576463`

---

## Indice

1. [Gerarchia di configurazione](#1-gerarchia-di-configurazione)
2. [User Secrets — configurazione (Development e ProductionSim)](#2-user-secrets--configurazione-development-e-productionsim)
3. [Avvio in modalità Development](#3-avvio-in-modalità-development)
4. [Avvio in modalità Production](#4-avvio-in-modalità-production)
5. [Differenze comportamentali tra ambienti](#5-differenze-comportamentali-tra-ambienti)
6. [Verifica avvio](#6-verifica-avvio)
7. [Simulare l'ambiente Production in Visual Studio (debug)](#7-simulare-lambiente-production-in-visual-studio-debug)

---

## 1. Gerarchia di configurazione

L'applicazione risolve la configurazione in quest'ordine (priorità crescente):

```
appsettings.json
  └── appsettings.{ASPNETCORE_ENVIRONMENT}.json
        └── User Secrets          ← Development e ProductionSim
              └── Variabili d'ambiente ← Production e ProductionSim (sovrascrivono tutto)
```

> **Regola**: `appsettings.json` e `appsettings.Production.json` devono contenere valori **vuoti**
> per tutti i campi sensibili (password, connection strings). Non committare mai credenziali nel repository.

---

## 2. User Secrets — configurazione (Development e ProductionSim)

I User Secrets sono la soluzione sicura per gestire credenziali in locale **senza mai toccare i file `appsettings`**.

### Percorso fisico del file `secrets.json`

| OS | Percorso |
|----|----------|
| **Windows** | `%APPDATA%\Microsoft\UserSecrets\2ad40ed9-cd4a-4f74-99d0-122f1c576463\secrets.json` |
| **Linux/macOS** | `~/.microsoft/usersecrets/2ad40ed9-cd4a-4f74-99d0-122f1c576463/secrets.json` |

### Inizializzazione completa (PowerShell)

Eseguire dalla directory `BlazorDematReports\` (dove si trova il `.csproj`):

```powershell
cd C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports

# ── Connection Strings obbligatorie ────────────────────────────────────────
dotnet user-secrets set "ConnectionStrings:DematReportsContext"     "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:HangfireConnection"      "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:CnxnDematReports"        "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=PASSWORD;TrustServerCertificate=True;"

# ── Connection Strings secondarie (solo se le lavorazioni le usano) ────────
dotnet user-secrets set "ConnectionStrings:CnxnCaptiva206"          "Server=172.30.122.206;Database=RHM_POSTEL;User Id=read_user_db;Password=PASSWORD;"
dotnet user-secrets set "ConnectionStrings:CnxnUnicredit"           "Server=10.114.8.12;Database=ProduzioneGesimUnicredit;User Id=ProduzioneGesimUnicredit;Password=PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:CnxnPdp"                 "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnPraticheSuccessione" "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4SorterVips"     "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4Sorter1"        "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4Sorter2"        "Server=...;Database=...;User Id=...;Password=...;"

# ── Servizi Mail Exchange EWS ──────────────────────────────────────────────
dotnet user-secrets set "MailServices:HERA16:Username"              "verona.edp"
dotnet user-secrets set "MailServices:HERA16:Password"              "PASSWORD_HERA16"
dotnet user-secrets set "MailServices:HERA16:Domain"                "postel.it"
dotnet user-secrets set "MailServices:ADER4:Username"               "verona.edp"
dotnet user-secrets set "MailServices:ADER4:Password"               "PASSWORD_ADER4"
dotnet user-secrets set "MailServices:ADER4:Domain"                 "postel.it"

# ── Email SMTP (FluentEmail) ───────────────────────────────────────────────
dotnet user-secrets set "Email:Password"                            "PASSWORD_SMTP"
```

### Verifica secrets impostati

```powershell
dotnet user-secrets list
```

### Modifica manuale del file secrets.json

Aprire direttamente il file (utile per copiare/incollare blocchi):

```powershell
# Apri il file secrets.json in Notepad
notepad "$env:APPDATA\Microsoft\UserSecrets\2ad40ed9-cd4a-4f74-99d0-122f1c576463\secrets.json"
```

Struttura attesa del file:

```json
{
  "ConnectionStrings:DematReportsContext":     "Server=...;Database=DematReports;...",
  "ConnectionStrings:HangfireConnection":      "Server=...;Database=DematReports;...",
  "ConnectionStrings:CnxnDematReports":        "Server=...;Database=DematReports;...",
  "ConnectionStrings:CnxnCaptiva206":          "Server=...;...",
  "ConnectionStrings:CnxnUnicredit":           "Server=...;...",
  "ConnectionStrings:CnxnPdp":                "Server=...;...",
  "ConnectionStrings:CnxnPraticheSuccessione": "Server=...;...",
  "ConnectionStrings:CnxnAder4SorterVips":     "Server=...;...",
  "ConnectionStrings:CnxnAder4Sorter1":        "Server=...;...",
  "ConnectionStrings:CnxnAder4Sorter2":        "Server=...;...",
  "MailServices:HERA16:Username":             "verona.edp",
  "MailServices:HERA16:Password":              "...",
  "MailServices:HERA16:Domain":               "postel.it",
  "MailServices:ADER4:Username":              "verona.edp",
  "MailServices:ADER4:Password":              "...",
  "MailServices:ADER4:Domain":               "postel.it",
  "Email:Password":                            "..."
}
```

### Rimozione di un secret

```powershell
dotnet user-secrets remove "ConnectionStrings:CnxnPdp"

# Rimuovi tutti i secrets del progetto
dotnet user-secrets clear
```

---

## 3. Avvio in modalità Development

### Da Visual Studio

1. Selezionare il profilo **`https`** o **`http`** nel selettore di debug (in alto nella toolbar)
2. Premere **F5** (con debugger) oppure **Ctrl+F5** (senza debugger)

Il profilo `https` imposta automaticamente:
```
ASPNETCORE_ENVIRONMENT = Development
URL = https://localhost:7065 ; http://localhost:5177
```

### Da terminale (dotnet CLI)

```powershell
cd C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports

# Avvio con profilo https (consigliato)
dotnet run --launch-profile https

# Avvio con profilo http
dotnet run --launch-profile http

# Avvio esplicito impostando l'ambiente
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run
```

### Cosa si attiva in Development

| Comportamento | Development |
|---|---|
| User Secrets | **Caricati** (`AddUserSecrets`) |
| Pagina dettaglio errore (`UseDeveloperExceptionPage`) | **Attiva** |
| Active Directory | **Mock** (`MockActiveDirectoryService`) — nessun AD necessario |
| `LoginSettings.RequireActiveDirectory` | `false` (da `appsettings.json`) |
| `LoginSettings.AllowAutoLogin` | configurabile per test |
| `LoginSettings.ShowEnvironmentBadge` | `true` — badge visibile nell'UI |
| Logging livello `BlazorDematReports.*` | `Information` / `Debug` |
| Hot Reload | Disponibile con profilo `BlazorDematReports` |

---

## 4. Avvio in modalità Production

In produzione i segreti non vengono mai letti dai User Secrets.
Ogni chiave sensibile deve essere fornita come **variabile d'ambiente** del processo host.

### Regola di naming delle variabili d'ambiente

Il separatore gerarchico dei `appsettings.json` (`:`) diventa doppio underscore (`__`):

| appsettings | Variabile d'ambiente |
|---|---|
| `ConnectionStrings:DematReportsContext` | `ConnectionStrings__DematReportsContext` |
| `MailServices:HERA16:Password` | `MailServices__HERA16__Password` |
| `Email:Password` | `Email__Password` |

### Avvio da terminale PowerShell

```powershell
cd C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports

# 1. Build Release
dotnet publish -c Release -o ./publish

# 2. Impostare variabili d'ambiente nella sessione PowerShell
$env:ASPNETCORE_ENVIRONMENT                              = "Production"
$env:ConnectionStrings__DematReportsContext              = "Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;"
$env:ConnectionStrings__HangfireConnection               = "Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;"
$env:ConnectionStrings__CnxnDematReports                 = "Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;"
$env:ConnectionStrings__CnxnCaptiva206                   = "Server=...;..."
$env:ConnectionStrings__CnxnUnicredit                    = "Server=...;..."
$env:ConnectionStrings__CnxnPdp                          = "Server=...;..."
$env:ConnectionStrings__CnxnPraticheSuccessione          = "Server=...;..."
$env:ConnectionStrings__CnxnAder4SorterVips              = "Server=...;..."
$env:ConnectionStrings__CnxnAder4Sorter1                 = "Server=...;..."
$env:ConnectionStrings__CnxnAder4Sorter2                 = "Server=...;..."
$env:MailServices__HERA16__Username                      = "verona.edp"
$env:MailServices__HERA16__Password                      = "PASSWORD_HERA16"
$env:MailServices__HERA16__Domain                        = "postel.it"
$env:MailServices__ADER4__Username                       = "verona.edp"
$env:MailServices__ADER4__Password                       = "PASSWORD_ADER4"
$env:MailServices__ADER4__Domain                         = "postel.it"
$env:Email__Password                                     = "PASSWORD_SMTP"

# 3. Avvio
dotnet ./publish/BlazorDematReports.dll
```

### Avvio tramite IIS (web.config)

Aggiungere le variabili d'ambiente dentro `<aspNetCore>` nel file `web.config` della cartella di publish:

```xml
<aspNetCore processPath="dotnet"
            arguments=".\BlazorDematReports.dll"
            stdoutLogEnabled="false"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT"                     value="Production" />
    <environmentVariable name="ConnectionStrings__DematReportsContext"      value="Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;" />
    <environmentVariable name="ConnectionStrings__HangfireConnection"       value="Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;" />
    <environmentVariable name="ConnectionStrings__CnxnDematReports"         value="Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;" />
    <environmentVariable name="MailServices__HERA16__Username"             value="verona.edp" />
    <environmentVariable name="MailServices__HERA16__Password"              value="PASSWORD_HERA16" />
    <environmentVariable name="MailServices__HERA16__Domain"                value="postel.it" />
    <environmentVariable name="MailServices__ADER4__Username"               value="verona.edp" />
    <environmentVariable name="MailServices__ADER4__Password"               value="PASSWORD_ADER4" />
    <environmentVariable name="MailServices__ADER4__Domain"                 value="postel.it" />
    <environmentVariable name="Email__Password"                             value="PASSWORD_SMTP" />
  </environmentVariables>
</aspNetCore>
```

> **Sicurezza**: il file `web.config` contiene credenziali in chiaro.
> Proteggerlo con permessi NTFS ristretti (`IIS_IUSRS` read-only, amministratori only write).
> In alternativa, usare le **Variabili d'ambiente di sistema Windows** oppure **Azure Key Vault**.

### Impostare variabili d'ambiente di sistema Windows (persistente)

```powershell
# Eseguire come Amministratore
[System.Environment]::SetEnvironmentVariable(
    "ConnectionStrings__DematReportsContext",
    "Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;",
    [System.EnvironmentVariableTarget]::Machine
)

# Verificare
[System.Environment]::GetEnvironmentVariable(
    "ConnectionStrings__DematReportsContext",
    [System.EnvironmentVariableTarget]::Machine
)
```

### Cosa si attiva in Production

| Comportamento | Production |
|---|---|
| User Secrets | **Non caricati** |
| Variabili d'ambiente | **Caricate** (`AddEnvironmentVariables`) |
| Pagina dettaglio errore | **Disattiva** — mostra `/Error` generico |
| Active Directory | **Reale** (`ActiveDirectoryService`) |
| `LoginSettings.RequireActiveDirectory` | `true` (da `appsettings.Production.json`) |
| `LoginSettings.AllowAutoLogin` | `false` (obbligatorio) |
| `LoginSettings.ShowEnvironmentBadge` | `false` — badge nascosto |
| Logging livello predefinito | `Warning` — meno verboso |
| `LoginSettings.Environment` | `"Production"` — visibile nei log di avvio |

---

## 5. Differenze comportamentali tra ambienti

| Aspetto | Development | ProductionSim | Production |
|---|---|---|---|
| Sorgente segreti | User Secrets | User Secrets | Variabili d'ambiente |
| Variabili d'ambiente | No | Sì (sovrascrivono secrets) | Sì |
| Active Directory | Mock | **Reale** (`postel.it`) | Reale (`postel.it`) |
| Servizi mail | Mock (se `UseMockService: true`) | **Reali** | Reali |
| Pagina errore dettagliata | Sì | No | No |
| Badge ambiente nell'UI | Sì (`Development`) | Sì (`ProductionSim`) | No |
| Log verbosità | `Information` / `Debug` | `Warning` | `Warning` |
| Auto-login | Configurabile | `false` | `false` |
| Hot Reload | Disponibile | No | No |
| Debugger VS (F5) | Sì | **Sì** | No |
| `appsettings` caricato | `appsettings.json` | `appsettings.json` + `appsettings.ProductionSim.json` | `appsettings.json` + `appsettings.Production.json` |
| `RequireActiveDirectory` | `false` | `true` | `true` |

---

## 6. Verifica avvio

### Log di avvio attesi (Development e Production)

Controllare nei log NLog (`logs/blazor-demat-YYYY-MM-DD.log`) la presenza di:

```
[INFO] Application started.
[INFO] Hangfire Online: Servers=1 Enqueued=0 Failed=0
[INFO] Login in environment: Development|Production
```

### Test connessione database (PowerShell)

```powershell
# Verifica rapida della connection string principale
$connectionString = $env:ConnectionStrings__DematReportsContext
$conn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$conn.Open()
Write-Host "Connessione OK: $($conn.Database) su $($conn.DataSource)"
$conn.Close()
```

### Checklist minima prima dell'avvio Production

- [ ] `ASPNETCORE_ENVIRONMENT=Production` impostato nel processo host
- [ ] `ConnectionStrings__DematReportsContext` valorizzato
- [ ] `ConnectionStrings__HangfireConnection` valorizzato
- [ ] `ConnectionStrings__CnxnDematReports` valorizzato
- [ ] Active Directory raggiungibile dal server (`postel.it`)
- [ ] Cartella `logs/` con permessi di scrittura per l'utente del processo
- [ ] Dashboard Hangfire verificato: `https://<host>/hangfire` (richiede ruolo ADMIN)
- [ ] Login test con utente AD effettuato con successo

---

## 7. Simulare l'ambiente Production in Visual Studio (debug)

Per testare comportamenti production (logging con active directory, badge, Hangfire, AD mock) rimanendo nel debugger VS,
si usa un ambiente personalizzato chiamato **`ProductionSim`**.

### Come funziona

```
ASPNETCORE_ENVIRONMENT = ProductionSim
       │
       ├── appsettings.json                  (base)
       ├── appsettings.ProductionSim.json    (override production-like)
       ├── User Secrets                      (credenziali locali — come Development)
       ├── Variabili d'ambiente              (sovrascrivono User Secrets se presenti)
       └── Active Directory                  REALE (dominio postel.it)
```

Differenza rispetto all'ambiente `Production` vero:

| | `Production` | `ProductionSim` |
|---|---|---|
| Segreti | Variabili d'ambiente | **User Secrets** |
| Active Directory | Reale (`postel.it`) | **Reale (`postel.it`)** |
| Servizi mail (ADER4/HERA16) | Reali | **Reali** |
| Debugger VS | No | **Sì** |
| Badge nell'UI | Nascosto | **"ProductionSim"** |
| Logging | `Warning` | `Warning` (come prod) |
| Hangfire jobs | Sì | **Sì — attenzione al DB** |
| `RequireActiveDirectory` | `true` | `true` |

> **Attenzione**: `ProductionSim` usa Active Directory reale e caselle mail di produzione.
> Assicurarsi che il PC di sviluppo sia connesso al dominio `postel.it`.
> I job Hangfire scriveranno dati se i User Secrets puntano al DB di produzione —
> usare sempre un DB di **collaudo** per i test locali.

---

### Step 1 — Prerequisiti: User Secrets già configurati

Verificare che i secrets siano presenti (vedere §2):

```powershell
cd C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports
dotnet user-secrets list
```

Devono comparire almeno `ConnectionStrings:DematReportsContext` e `ConnectionStrings:HangfireConnection`.

---

### Step 2 — Selezionare il profilo `Production-Sim` in Visual Studio

Nel selettore dei profili di debug (toolbar in alto), scegliere **`Production-Sim`**:

```
[ ▶ Production-Sim ▼ ]   ← selezionare questo profilo
```

Poi premere **F5** per avviare con il debugger.

Il profilo imposta automaticamente `ASPNETCORE_ENVIRONMENT=ProductionSim`
e rimane sull'URL `https://localhost:7065`.

> Il profilo `Production-Sim` è già stato aggiunto in `launchSettings.json`
> (vedere file `BlazorDematReports\Properties\launchSettings.json`).

---

### Step 3 — Verificare che l'ambiente sia attivo

All'avvio, controllare nella finestra **Output → Debug** di Visual Studio:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7065
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
      Environment: ProductionSim
```

Nell'interfaccia Blazor comparirà il badge **`ProductionSim`** in alto.

---

### Comportamento dei job Hangfire

Con `ProductionSim` Hangfire è **attivo** e i job ricorrenti vengono schedulati.
Per evitare esecuzioni indesiderate sul DB di collaudo, sospendere i job dalla dashboard:

```
https://localhost:7065/hangfire
```

Selezionare i job → **Trigger** / **Delete** / mettere i server in pausa
dalla voce **Servers** nel menu laterale.

---

### Ripristinare il profilo Development

Per tornare alla modalità normale, selezionare il profilo **`https`** o **`http`** nel selettore
e premere F5. Nessuna modifica ai file è necessaria.

---

*Documento generato per il progetto BlazorDematReports — .NET 10 · Blazor Server*
