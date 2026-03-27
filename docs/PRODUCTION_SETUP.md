# BlazorDematReports — Guida alla messa in produzione

> **Framework**: Blazor Server · **.NET 10** · **SQL Server** · **Hangfire** · **NLog**

---

## Indice

1. [Prerequisiti](#1-prerequisiti)
2. [Struttura della configurazione](#2-struttura-della-configurazione)
3. [User Secrets (Sviluppo)](#3-user-secrets-sviluppo)
4. [Variabili d'ambiente (Produzione)](#4-variabili-dambiente-produzione)
5. [Connection Strings](#5-connection-strings)
6. [Servizi Mail](#6-servizi-mail)
7. [Credenziali Webtop / Sistemi esterni](#7-credenziali-webtop--sistemi-esterni)
8. [Active Directory](#8-active-directory)
9. [Email SMTP (FluentEmail)](#9-email-smtp-fluentemail)
10. [Hangfire Dashboard](#10-hangfire-dashboard)
11. [NLog e cartella log](#11-nlog-e-cartella-log)
12. [Checklist prima dell'avvio](#12-checklist-prima-dellavvio)
13. [Database in produzione](#13-database-in-produzione)

---

## 1. Prerequisiti

| Componente | Versione minima | Note |
|---|---|---|
| .NET Runtime | **10.0** | Installato sul server applicativo |
| SQL Server | 2019+ | Database applicativo + Hangfire |
| IIS / Kestrel | — | Configurare `ASPNETCORE_ENVIRONMENT=Production` |
| Active Directory | — | Dominio `postel.it` raggiungibile dal server |
| Accesso SMTP | — | Solo se `ServiceMail` abilitato |
| Accesso Exchange EWS | — | Solo per ingestion mail ADER4 / HERA16 |

---

## 2. Struttura della configurazione

L'applicazione usa la seguente cascata di configurazione (priorità crescente):

```
appsettings.json
  └── appsettings.Production.json      ← overrides per produzione
        └── Variabili d'ambiente       ← segreti (connessioni, password)
```

In **Development** al posto delle variabili d'ambiente vengono usati i **User Secrets** (vedi §3).

> **Regola**: nessuna password o connection string deve essere committata nel repository.
> `appsettings.json` e `appsettings.Production.json` devono contenere valori **vuoti** per tutti i campi sensibili.

---

## 3. User Secrets (Sviluppo)

`UserSecretsId`: `2ad40ed9-cd4a-4f74-99d0-122f1c576463`

Il file viene salvato in:
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\2ad40ed9-cd4a-4f74-99d0-122f1c576463\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/2ad40ed9-cd4a-4f74-99d0-122f1c576463/secrets.json`

### Inizializzazione rapida (PowerShell)

```powershell
cd BlazorDematReports

# --- Connection Strings ---
dotnet user-secrets set "ConnectionStrings:DematReportsContext"    "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=UserProduzioneGed2022!;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:HangfireConnection"     "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=UserProduzioneGed2022!;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:CnxnDematReports"       "Server=SERVER;Database=DematReports;User Id=UserProduzioneGed;Password=UserProduzioneGed2022!;TrustServerCertificate=True;"

dotnet user-secrets set "ConnectionStrings:CnxnUnicredit"          "Server=10.114.8.12;Database=ProduzioneGesimUnicredit;User Id=ProduzioneGesimUnicredit; Password=Produzione2020;TrustServerCertificate=True;"
dotnet user-secrets set "ConnectionStrings:CnxnCaptiva206"         "Server=172.30.122.206;Database=RHM_POSTEL;User Id=read_user_db;Password=read_user_db;"


dotnet user-secrets set "ConnectionStrings:CnxnPdp"                "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnPraticheSuccessione" "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4SorterVips"    "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4Sorter1"       "Server=...;Database=...;User Id=...;Password=...;"
dotnet user-secrets set "ConnectionStrings:CnxnAder4Sorter2"       "Server=...;Database=...;User Id=...;Password=...;"

# --- Servizi Mail ---
dotnet user-secrets set "MailServices:HERA16:Password"   "PASSWORD_HERA16"
dotnet user-secrets set "MailServices:ADER4:Password"    "PASSWORD_ADER4"


# --- Email SMTP (FluentEmail) ---
dotnet user-secrets set "Email:Password" "PASSWORD_SMTP"
```

### Verifica

```powershell
dotnet user-secrets list
```

---

## 4. Variabili d'ambiente (Produzione)

In produzione l'applicazione legge i segreti da variabili d'ambiente.
Il separatore di sezione è il doppio underscore `__`.

### Esempio: file `.env` o configurazione IIS / Windows Service

```env
ASPNETCORE_ENVIRONMENT=Production

# Connection Strings
ConnectionStrings__DematReportsContext=Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;
ConnectionStrings__HangfireConnection=Server=PROD_SERVER;Database=DematReports_Hangfire;User Id=hangfire_user;Password=SECRET;TrustServerCertificate=True;
ConnectionStrings__CnxnDematReports=Server=PROD_SERVER;Database=DematReports;User Id=app_user;Password=SECRET;TrustServerCertificate=True;
ConnectionStrings__CnxnCaptiva206=Server=CAPTIVA_SERVER;Database=Captiva206;User Id=...;Password=...;TrustServerCertificate=True;
ConnectionStrings__CnxnUnicredit=...
ConnectionStrings__CnxnPdp=...
ConnectionStrings__CnxnPraticheSuccessione=...
ConnectionStrings__CnxnAder4SorterVips=...
ConnectionStrings__CnxnAder4Sorter1=...
ConnectionStrings__CnxnAder4Sorter2=...

# Servizi Mail
MailServices__HERA16__Password=PASSWORD_HERA16
MailServices__ADER4__Password=PASSWORD_ADER4

# Webtop
UrlConfig__PasswordWebtopInps=PASSWORD
UrlConfig__PasswordWebtopInpsGenova=PASSWORD
UrlConfig__PasswordWebtopInpsPomezia=PASSWORD
UrlConfig__PasswordWebtopInpsMelzo=PASSWORD
UrlConfig__PasswordWebtopInail=PASSWORD
UrlConfig__PasswordWebtopEquitalia23I=PASSWORD
UrlConfig__PasswordWebtopAciRaccomandate=PASSWORD
UrlConfig__PasswordPraticheSucc=PASSWORD

# Email SMTP
Email__Password=PASSWORD_SMTP
```

### Impostare variabili d'ambiente su IIS (web.config)

```xml
<aspNetCore processPath="dotnet" arguments=".\BlazorDematReports.dll" stdoutLogEnabled="false">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DematReportsContext" value="Server=...;Password=...;" />
  </environmentVariables>
</aspNetCore>
```

---

## 5. Connection Strings

| Chiave | Descrizione | Obbligatoria |
|---|---|---|
| `DematReportsContext` | DB principale EF Core (Blazor + Hangfire schema) | **Sì** |
| `HangfireConnection` | DB dedicato Hangfire (può coincidere con il principale) | **Sì** |
| `CnxnDematReports` | Connessione diretta ADO.NET al DB principale | **Sì** |
| `CnxnCaptiva206` | Sistema Captiva 206 (lavorazioni scansione) | Solo se abilitato |
| `CnxnUnicredit` | Sistema Unicredit | Solo se abilitato |
| `CnxnPdp` | Sistema PdP | Solo se abilitato |
| `CnxnPraticheSuccessione` | Sistema Pratiche Successione | Solo se abilitato |
| `CnxnAder4SorterVips` | Sorter VIPS Ader4 | Solo se abilitato |
| `CnxnAder4Sorter1` | Sorter 1 Ader4 | Solo se abilitato |
| `CnxnAder4Sorter2` | Sorter 2 Ader4 | Solo se abilitato |

> `DematReportsContext` e `HangfireConnection` sono le uniche connessioni che bloccano l'avvio se assenti.
> Le altre sono richieste solo dai job che le utilizzano.

---

## 6. Servizi Mail

Usati per l'ingestion automatica dei CSV via Exchange EWS.

| Chiave | Descrizione |
|---|---|
| `MailServices:HERA16:Username` | Utente Exchange per HERA16 (in `appsettings.Production.json`) |
| `MailServices:HERA16:Password` | **Segreto** — variabile d'ambiente / User Secret |
| `MailServices:HERA16:ExchangeUrl` | URL EWS Exchange |
| `MailServices:ADER4:Username` | Utente Exchange per ADER4 (in `appsettings.Production.json`) |
| `MailServices:ADER4:Password` | **Segreto** — variabile d'ambiente / User Secret |
| `MailServices:ADER4:ExchangeUrl` | URL EWS Exchange |

### Modalità Mock (solo sviluppo)

In `appsettings.Development.json` impostare:
```json
"MailServices": {
  "HERA16": { "UseMockService": true, "MockDataPath": "TestData/HERA16" },
  "ADER4":  { "UseMockService": true, "MockDataPath": "TestData/ADER4"  }
}
```

In produzione assicurarsi che `UseMockService` sia `false` (o assente).

---



## 8. Active Directory

Configurato in `appsettings.Production.json`:

```json
"ActiveDirectory": {
  "Domain": "postel.it",
  "TimeoutSeconds": 30
},
"LoginSettings": {
  "RequireActiveDirectory": true,
  "AllowAutoLogin": false,
  "ShowEnvironmentBadge": false
}
```

> In produzione `RequireActiveDirectory: true` è obbligatorio.
> `AllowAutoLogin` deve essere **sempre `false`** in produzione.

---

## 9. Email SMTP (FluentEmail)

Usato dal servizio `ServiceMail` per notifiche interne.

| Chiave | Dove |
|---|---|
| `Email:SmtpHost` | `appsettings.Production.json` |
| `Email:SmtpPort` | `appsettings.Production.json` |
| `Email:EnableSsl` | `appsettings.Production.json` |
| `Email:Username` | `appsettings.Production.json` |
| `Email:DefaultFrom` | `appsettings.Production.json` |
| `Email:Password` | **Variabile d'ambiente** / User Secret |

Riferimento template: `appsettings.Email.example.json`.

---

## 10. Hangfire Dashboard

Accessibile all'URL `/hangfire`.
L'accesso è protetto da `MyAuthorizationFilter` — richiede autenticazione con ruolo **ADMIN**.

```
https://<host>/hangfire
```

> Il DB Hangfire viene creato/migrato automaticamente all'avvio grazie a `PrepareSchemaIfNecessary = true`.

---

## 11. NLog e cartella log

I log sono scritti nella cartella `logs/` relativa alla directory dell'applicazione.

```
<app_root>/
  logs/
    blazor-demat-YYYY-MM-DD.log          ← log principale
    errors/
      sql/sql-errors-YYYY-MM-DD.log
      validation/validation-errors-*.log
      background-tasks/bg-task-errors-*.log
    internal/nlog-internal.log           ← diagnostica NLog stessa
    archives/                            ← file ruotati (max 30 giorni)
```

Assicurarsi che l'utente del processo IIS abbia **permesso di scrittura** sulla cartella `logs/`.

---

## 12. Checklist prima dell'avvio

### Database

- [ ] DB `DematReports` esistente e accessibile dal server applicativo
- [ ] DB `DematReports_Hangfire` creato (oppure usare lo stesso DB con schema separato)
- [ ] Utente SQL con permessi `db_owner` su entrambi i database (o equivalenti minimi)
- [ ] Tutte le tabelle migrate: verificare con `SELECT TOP 1 * FROM ProcedureLavorazioni`
- [ ] Tabella identità reimpostata se necessario: `DBCC CHECKIDENT ('NomeTabella', RESEED, N)`

### Applicazione

- [ ] `ASPNETCORE_ENVIRONMENT=Production` impostato
- [ ] Tutte le connection strings configurate come variabili d'ambiente
- [ ] Tutte le password Webtop configurate
- [ ] Password mail HERA16 e ADER4 configurate
- [ ] `MailServices:HERA16:UseMockService` e `ADER4:UseMockService` impostati a `false`
- [ ] `LoginSettings:AllowAutoLogin = false`
- [ ] `LoginSettings:RequireActiveDirectory = true`
- [ ] Cartella `logs/` scrivibile dall'utente del processo
- [ ] Cartella `PathFileConfig:PathFileBollettini` raggiungibile (`\\vrfile1.postel.it\...`)
- [ ] Build Release eseguita: `dotnet publish -c Release -o ./publish`

### Verifica avvio

```powershell
# Avvio manuale per test
dotnet BlazorDematReports.dll

# Controllare nei log:
# [INFO] Hangfire Online: Servers=1 Enqueued=0 Failed=0
# [INFO] Application started
```

- [ ] Nessun errore fatale nei log di avvio
- [ ] Dashboard Hangfire raggiungibile a `/hangfire`
- [ ] Login con utente AD funzionante
- [ ] Almeno una lavorazione test eseguita manualmente

---

## 13. Database in produzione

### Importazione dati iniziali

```sql
-- Reimpostare identity dopo import
DBCC CHECKIDENT ('ProcedureLavorazioni', RESEED, N)
DBCC CHECKIDENT ('ProcedureCliente', RESEED, 217)

-- Import CSV con IDENTITY mantenuta
BULK INSERT clienti
FROM 'C:\import\clienti.csv'
WITH (
    FIRSTROW = 1,
    KEEPIDENTITY,
    FIELDTERMINATOR = ',',
    ROWTERMINATOR = '\n',
    BATCHSIZE = 10000,
    MAXERRORS = 10
);
```

### Colonne aggiuntive pianificate

> Da aggiungere prima del go-live definitivo:

```sql
-- Distingue fasi create dal sistema (non modificabili) da quelle utente
ALTER TABLE FasiLavorazione
ADD UtilizzataDaSistema BIT NOT NULL DEFAULT 0;

-- Impostare a 1 le fasi di sistema (ProceduraCompleta, No_Fase_Lavorazione)
UPDATE FasiLavorazione SET UtilizzataDaSistema = 1
WHERE FaseLavorazione IN ('ProceduraCompleta', 'No_Fase_Lavorazione');
```

### Ordine di import consigliato

1. Struttura schema (script DDL)
2. Dati master: `CentriLavorazione`, `Ruoli`, `FormatoDati`, `Reparti`
3. `ProcedureLavorazioni` + `FasiLavorazione` (con `KEEPIDENTITY`)
4. `Operatori` + `CentriVisibili`
5. `ConfigurazioneFontiDati` + `ConfigurazioneFaseCentro`
6. Avvio applicazione → Hangfire sincronizza i job automaticamente

---

*Documento generato automaticamente da GitHub Copilot — aggiornare a ogni modifica della configurazione.*
