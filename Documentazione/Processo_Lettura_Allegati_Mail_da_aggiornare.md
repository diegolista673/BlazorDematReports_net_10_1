# Processo di Lettura Allegati Email - BlazorDematReports

**Data**: 2026-03-04
**Versione**: 2.0 - Architettura per-operatore
**Branch**: `feat/migrate-to-mapperly`

---

## Indice

1. [Panoramica](#1-panoramica)
 
2. [Architettura a due fasi](#2-architettura-a-due-fasi)

3. [Componenti coinvolti](#3-componenti-coinvolti)
 
4. [Fase 1 - Ingestion email (07:00)](#4-fase-1---ingestion-email-0700)

5. [Fase 2 - Produzione (07:15+)](#5-fase-2---produzione-0715)

6. [Servizio ADER4](#6-servizio-ader4)
 
7. [Servizio HERA16](#7-servizio-hera16)

8. [Tabella staging DatiMailCsv](#8-tabella-staging-datimailcsv)

9. [Configurazione appsettings](#9-configurazione-appsettings)
 
10. [Modalità mock (sviluppo/test)](#10-modalità-mock-sviluppotest)
 
11. [Aggiungere un nuovo servizio email](#11-aggiungere-un-nuovo-servizio-email)

12. [Troubleshooting](#12-troubleshooting)

---

## 1. Panoramica

Il sistema legge report giornalieri inviati via email con allegati CSV, aggrega i dati **per operatore** e li rende 
disponibili per l'inserimento in `ProduzioneSistema`.

Il processo è **disaccoppiato in due fasi** per evitare race condition tra task paralleli e per 
garantire resilienza in caso di fallimento.

```
EMAIL (Exchange EWS)
        │
        ▼
  [FASE 1 - 07:00]
  GenericMailIngestionHandler
  → Ader4IngestionProcessor   ──► DatiMailCsv (staging per-operatore)
  → Hera16IngestionProcessor  ──►
        │
        ▼
  [FASE 2 - 07:15+]
  Handler specifici per TipoRisultato
  → Ader4CaptivaHandler        ──► ProduzioneSistema
  → Ader4SorterHandler         ──►
  → Hera16ScansioneHandler     ──►
  → ...
```

---

## 2. Architettura a due fasi

### Fase 1 — Mail Ingestion (`MAIL_INGESTION`, cron `0 7 * * *`)

Un **unico Hangfire recurring job** alle 07:00 che:

1. Istanzia `GenericMailIngestionHandler`
2. Chiama in sequenza tutti gli `IMailIngestionProcessor` registrati
3. Ogni processor legge le proprie email via **Exchange EWS**, scarica gli allegati CSV, aggruppa i dati **per operatore** e salva in `DatiMailCsv` (flag `Elaborata = 0`)
4. Non produce `DatiLavorazione` — restituisce sempre lista vuota

### Fase 2 — Production handlers (cron configurabile per task)

Task Hangfire separati per ogni procedura/fase che:

1. Leggono `DatiMailCsv` filtrato per `(CodiceServizio, TipoRisultato, DataLavorazione, Elaborata = 0)`
2. Mappano ogni riga staging → `DatiLavorazione` usando l'**operatore reale dal CSV**
3. Chiamano `ElaboratoreDatiLavorazione` → INSERT in `ProduzioneSistema`
4. Marcano le righe staging con `Elaborata = 1`

---

## 3. Componenti coinvolti

### Interfacce

| Interfaccia | Responsabilità |
|---|---|
| `IEmailBatchProcessor` | Contratto base Exchange EWS (scarica email + allegati) |
| `IMailIngestionProcessor` | Contratto per un processore specifico (ADER4, HERA16) |
| `IMailCsvService` | CRUD staging `DatiMailCsv` |
| `IProductionDataHandler` | Contratto handler produzione (legge staging, produce DatiLavorazione) |

### Classi per ADER4

| Classe | File | Ruolo |
|---|---|---|
| `Ader4EmailService` | `Handlers/MailHandlers/Ader4/` | EWS + parsing CSV ADER4 + aggregazione per-operatore |
| `LocalCsvAder4EmailService` | `Handlers/MailHandlers/Ader4/` | Mock: legge CSV da cartella locale |
| `Ader4IngestionProcessor` | `Handlers/MailHandlers/Ader4/` | Orchestra `Ader4EmailService` → `IMailCsvService` |
| `Ader4StagingHandlerBase` | `Handlers/MailHandlers/Ader4/` | Base per handler produzione ADER4 |
| `Ader4CaptivaHandler` | `Handlers/MailHandlers/Ader4/` | Legge staging `ScansioneCaptiva` |
| `Ader4SorterHandler` | `Handlers/MailHandlers/Ader4/` | Legge staging `ScansioneSorter` |
| `Ader4SorterBusteHandler` | `Handlers/MailHandlers/Ader4/` | Legge staging `ScansioneSorterBuste` |

### Classi per HERA16

| Classe | File | Ruolo |
|---|---|---|
| `Hera16EmailService` | `Handlers/MailHandlers/Hera16/` | EWS + parsing CSV HERA16 + aggregazione per-operatore |
| `LocalCsvHera16EmailService` | `Handlers/MailHandlers/Hera16/` | Mock: legge CSV da cartella locale |
| `Hera16IngestionProcessor` | `Handlers/MailHandlers/Hera16/` | Orchestra `Hera16EmailService` → `IMailCsvService` |
| `Hera16StagingHandlerBase` | `Handlers/MailHandlers/Hera16/` | Base per handler produzione HERA16 |
| `Hera16ScansioneHandler` | `Handlers/MailHandlers/Hera16/` | Legge staging `Scansione` |
| `Hera16IndexHandler` | `Handlers/MailHandlers/Hera16/` | Legge staging `Index` |
| `Hera16ClassificazioneHandler` | `Handlers/MailHandlers/Hera16/` | Legge staging `Classificazione` |

### Servizi infrastruttura

| Classe | Descrizione |
|---|---|
| `BaseEwsEmailService` | Connessione Exchange EWS, download allegati, parsing CSV base |
| `MailCsvService` | Implementazione `IMailCsvService` (UpsertBulk, GetUnprocessed, MarkAsProcessed) |
| `GenericMailIngestionHandler` | Orchestratore: chiama tutti i processori in sequenza |

---

## 4. Fase 1 - Ingestion email (07:00)

### Flusso dettagliato

```
Hangfire Job "MAIL_INGESTION"
    │
    └─► GenericMailIngestionHandler.ExecuteAsync()
            │
            ├─► Ader4IngestionProcessor.ProcessAndSaveAsync(mailCsvService)
            │       │
            │       ├─► Ader4EmailService.ProcessEmailsAsync()
            │       │       ├─► Exchange EWS: cerca email per SubjectFilters
            │       │       ├─► Download allegati CSV
            │       │       ├─► ProcessAttachmentAsync() per ogni allegato
            │       │       │       └─► AggiungiRighe*(csvData, dataRif, ...)
            │       │       │               GROUP BY 'postazione'
            │       │       │               → _righeElaborate.Add(DatiMailCsvDto)
            │       │       └─► Sposta email in cartella archivio
            │       │
            │       └─► mailCsvService.UpsertBulkAsync(righe)
            │               → INSERT/UPDATE DatiMailCsv (Elaborata=0)
            │
            └─► Hera16IngestionProcessor.ProcessAndSaveAsync(mailCsvService)
                    │  (stesso flusso, allegato CSV diverso)
                    └─► ...
```

### Gestione metadata dal body email

Il body dell'email viene analizzato per estrarre:

| Servizio | Campo ricercato | Chiave metadata |
|---|---|---|
| ADER4 | `Periodo di riferimento:` | `DataLavorazione` |
| ADER4 | `Identificativo evento:` | `IdEvento` |
| HERA16 | `Data lavorazione:` | `DataLavorazione` |
| HERA16 | `Identificativo evento:` | `IdEvento` |

---

## 5. Fase 2 - Produzione (07:15+)

### Flusso dettagliato

```
Hangfire Job (es. "hdl:42-proc:15-fase:3-nome:ader4_captiva")
    │
    └─► ProductionJobRunner.RunAsync(idTaskDaEseguire)
            │
            ├─► Carica TaskDaEseguire dal DB
            ├─► Risolve handler dal registry: "ADER4_CAPTIVA" → Ader4CaptivaHandler
            │
            └─► Ader4CaptivaHandler.ExecuteAsync(context)
                    │
                    ├─► mailCsvService.GetUnprocessedAsync(
                    │       "ADER4", "ScansioneCaptiva", dataMin, dataMax)
                    │
                    ├─► Map staging → DatiLavorazione[]
                    │       Operatore = s.Operatore  (reale dal CSV, non hardcoded)
                    │       Documenti = s.Documenti
                    │       Fogli     = CalcolaFogli(s.Documenti)
                    │
                    ├─► ElaboratoreDatiLavorazione.ElaboraEInserisci()
                    │       → INSERT ProduzioneSistema
                    │
                    └─► mailCsvService.MarkAsProcessedAsync(ids, taskId)
                            → UPDATE DatiMailCsv SET Elaborata=1
```

---

## 6. Servizio ADER4

### Email attese

- **Mittente/Subject Verona**: `DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)` (configurabile)
- **Mittente/Subject Genova**: `DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)` (configurabile)

### Allegati riconosciuti

| Pattern allegato | Metodo parsing | TipoRisultato generati |
|---|---|---|
| `EQTMN4_Scatole_Scansionate*` | `AggiungiRigheScatoleScansionate()` | `ScansioneCaptiva`, `ScansioneSorter`, `ScansioneSorterBuste` |
| `EQTMN4_Dispacci_Preaccettati*` | `AggiungiRigheDispacci()` | `PreAccettazione` |
| `EQTMN4_Dispacci_Ripartiti*` | `AggiungiRigheDispacci()` | `Ripartizione` |
| `EQTMN4_Scatole_Restituite*` | `AggiungiRigheDispacci()` | `Restituzione` |

### Formato CSV Scatole_Scansionate

```
postazione;Numero documenti;Codice Scatola;...
WORKST01;50;MN4001234567890
WORKST01;30;MN4999X91ABCDEF
WORKST02;20;MN4999X93GHIJKL
```

**Regole classificazione `Codice Scatola`**:

| Tipo | Condizione sul codice scatola |
|---|---|
| `ScansioneCaptiva` | `cod[4..9] != "999X9"` |
| `ScansioneSorter` | `cod[4..10] == "999X91"` o `"999X92"` **e** `cod[0..3] == "MN4"` |
| `ScansioneSorterBuste` | `cod[4..10] == "999X93"` **e** `cod[0..3] == "MN4"` |

**Aggregazione**: `GROUP BY postazione` — `SUM("Numero documenti")` per tipo.

### Formato CSV Dispacci (Preaccettati / Ripartiti / Restituite)

```
postazione;Numero Documenti;...
WORKST01;120
WORKST02;85
```

**Aggregazione**: `GROUP BY postazione` — `SUM("Numero Documenti")`.

### Calcolo Fogli per handler produzione ADER4

| HandlerCode | TipoRisultato | Calcolo Fogli |
|---|---|---|
| `ADER4_CAPTIVA` | `ScansioneCaptiva` | `Documenti / 2` |
| `ADER4_SORTER` | `ScansioneSorter` | `Documenti / 2` |
| `ADER4_SORTER_BUSTE` | `ScansioneSorterBuste` | `Documenti × 1` (1 busta = 1 foglio) |

---

## 7. Servizio HERA16

### Email attese

- **Subject**: `HERA16 - Report di produzione` (configurabile via `MailServices:HERA16:SubjectFilter`)

### Allegati riconosciuti

| Pattern allegato | TipoRisultato generati |
|---|---|
| `HERA16_Report*` | `Scansione`, `Index`, `Classificazione` |
| `HERA16_Produzione*` | `Scansione`, `Index`, `Classificazione` |

### Formato CSV HERA16

Una riga per documento elaborato. Le colonne operatore sono **indipendenti** (possono essere tutte valorizzate o solo alcune):

```
DataLavorazione;OperatoreScansione;OperatoreIndex;OperatoreClassificazione;CodiceMercato;...
2026-03-04;MARIO;LUIGI;ANNA;MERC001
2026-03-04;MARIO;;ANNA;MERC002
2026-03-04;;LUIGI;;MERC003
```

**Aggregazione per colonna**:

| Colonna CSV | TipoRisultato | Calcolo |
|---|---|---|
| `OperatoreScansione` | `Scansione` | `COUNT(*)` delle righe non nulle, `GROUP BY OperatoreScansione` |
| `OperatoreIndex` | `Index` | `COUNT(*)` delle righe non nulle, `GROUP BY OperatoreIndex` |
| `OperatoreClassificazione` | `Classificazione` | `COUNT(*)` delle righe non nulle, `GROUP BY OperatoreClassificazione` |

Ogni colonna viene processata indipendentemente: la stessa riga CSV può contribuire a uno, due o tutti e tre i TipoRisultato.

### Calcolo Fogli per handler produzione HERA16

| HandlerCode | TipoRisultato | Calcolo Fogli |
|---|---|---|
| `HERA16_SCANSIONE` | `Scansione` | `Documenti / 2` |
| `HERA16_INDEX` | `Index` | `Documenti / 2` |
| `HERA16_CLASSIFICAZIONE` | `Classificazione` | `Documenti / 2` |

---

## 8. Tabella staging DatiMailCsv

Una riga per combinazione univoca `(CodiceServizio, DataLavorazione, Operatore, TipoRisultato, IdEvento, Centro)`.

| Colonna | Tipo | Descrizione |
|---|---|---|
| `Id` | INT PK | Chiave primaria |
| `CodiceServizio` | NVARCHAR(50) | `ADER4` o `HERA16` |
| `DataLavorazione` | DATE | Data di riferimento del report |
| `Operatore` | NVARCHAR(100) | Valore `postazione` (ADER4) o nome operatore (HERA16) |
| `TipoRisultato` | NVARCHAR(100) | Tipo attività (es. `ScansioneCaptiva`, `Scansione`) |
| `Documenti` | INT | Totale documenti aggregati |
| `IdEvento` | NVARCHAR(100) NULL | Identificativo evento dall'email |
| `Centro` | NVARCHAR(50) NULL | Centro lavorazione (es. `VERONA`) |
| `DataIngestione` | DATETIME | Timestamp INSERT (default `GETDATE()`) |
| `Elaborata` | BIT | `0` = pronto, `1` = già elaborato |
| `ElaborataIl` | DATETIME NULL | Timestamp elaborazione |
| `ElaborataDaTaskId` | INT NULL | Id del task che ha elaborato il record |

**Script creazione**: `Database/Migrations/20250304_DatiMailCsv.sql`

### Query diagnostiche

```sql
-- Record non elaborati (pronti per i task produzione)
SELECT CodiceServizio, DataLavorazione, Operatore, TipoRisultato, Documenti
FROM DatiMailCsv WHERE Elaborata = 0
ORDER BY DataLavorazione DESC, CodiceServizio, TipoRisultato, Operatore;

-- Riepilogo per servizio/data
SELECT CodiceServizio, DataLavorazione, TipoRisultato,
       COUNT(DISTINCT Operatore) AS NumOperatori, SUM(Documenti) AS TotDocumenti
FROM DatiMailCsv
WHERE DataLavorazione >= DATEADD(DAY, -7, GETDATE())
GROUP BY CodiceServizio, DataLavorazione, TipoRisultato
ORDER BY DataLavorazione DESC;

-- Reset per re-processing (poi ri-eseguire il task da Hangfire dashboard)
UPDATE DatiMailCsv
SET Elaborata = 0, ElaborataIl = NULL, ElaborataDaTaskId = NULL
WHERE CodiceServizio = 'ADER4' AND DataLavorazione = '2026-03-04';

-- Cleanup record vecchi (oltre 30 giorni)
DELETE FROM DatiMailCsv
WHERE Elaborata = 1 AND ElaborataIl < DATEADD(DAY, -30, GETUTCDATE());
```

---

## 9. Configurazione appsettings

```jsonc
{
  "MailServices": {
    "ADER4": {
      "Username": "user@postel.it",
      "Password": "...",
      "Domain": "postel.it",
      "ExchangeUrl": "https://postaweb.postel.it/ews/exchange.asmx",
      "SubjectVerona": "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)",
      "SubjectGenova": "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)",
      "ArchiveFolder": "EQUITALIA_4",

      // Modalita mock (solo Development)
      "UseMockService": false,
      "MockDataPath": "TestData/ADER4"
    },
    "HERA16": {
      "Username": "user@postel.it",
      "Password": "...",
      "Domain": "postel.it",
      "ExchangeUrl": "https://postaweb.postel.it/ews/exchange.asmx",
      "SubjectFilter": "HERA16 - Report di produzione",
      "ArchiveFolder": "HERA16",

      // Modalita mock (solo Development)
      "UseMockService": false,
      "MockDataPath": "TestData/HERA16"
    }
  }
}
```

> **Sicurezza**: `Username` e `Password` non devono mai essere committati in chiaro. Usare **User Secrets** in Development (`dotnet user-secrets`) e **variabili d'ambiente** in Production.

---

## 10. Modalità mock (sviluppo/test)

Impostare `"UseMockService": true` in `appsettings.Development.json` per bypassare Exchange EWS e leggere file CSV da cartella locale.

### ADER4 — Cartella `TestData/ADER4/`

File richiesti (naming pattern):

| File | Contenuto |
|---|---|
| `EQTMN4_Scatole_Scansionate_*.csv` | Colonne: `postazione`, `Numero documenti`, `Codice Scatola` |
| `EQTMN4_Dispacci_Preaccettati_*.csv` | Colonne: `postazione`, `Numero Documenti` |
| `EQTMN4_Dispacci_Ripartiti_*.csv` | Colonne: `postazione`, `Numero Documenti` |
| `EQTMN4_Scatole_Restituite_*.csv` | Colonne: `postazione`, `Numero Documenti` |

Esempio `EQTMN4_Scatole_Scansionate_test.csv`:

```
postazione;Numero documenti;Codice Scatola
WORKST01;50;MN4001AABBCCDDEE
WORKST01;30;MN4999X91AABBCCD
WORKST02;20;MN4999X93EEFFGGH
```

### HERA16 — Cartella `TestData/HERA16/`

| File | Contenuto |
|---|---|
| `HERA16_Report_*.csv` | Colonne: `OperatoreScansione`, `OperatoreIndex`, `OperatoreClassificazione` + altre |

Esempio `HERA16_Report_test.csv`:

```
OperatoreScansione;OperatoreIndex;OperatoreClassificazione;CodiceMercato
MARIO;LUIGI;ANNA;MERC001
MARIO;;ANNA;MERC002
;LUIGI;;MERC003
GIUSEPPE;GIUSEPPE;GIUSEPPE;MERC004
```

> Il mock imposta `DataLavorazione = ieri` automaticamente per simulare il flusso reale.

---

## 11. Aggiungere un nuovo servizio email

Per aggiungere un nuovo servizio (es. `MERC99`):

### 1. Creare il servizio email

```
BlazorDematReports.Core/Handlers/MailHandlers/Merc99/
├── Merc99EmailService.cs          (estende BaseEwsEmailService)
└── LocalCsvMerc99EmailService.cs  (estende Merc99EmailService, per mock)
```

`Merc99EmailService` deve:
- Sovrascrivere `ProcessAttachmentAsync()` per parsare il proprio formato CSV
- Aggregare per operatore e popolare `_righeElaborate` (tipo `List<DatiMailCsvDto>`)
- Esporre `IReadOnlyList<DatiMailCsvDto> RigheElaborate`

### 2. Creare il processore ingestion

```csharp
public sealed class Merc99IngestionProcessor : IMailIngestionProcessor
{
    public string ServiceCode => "MERC99";

    public async Task<IngestionResult> ProcessAndSaveAsync(IMailCsvService svc, CancellationToken ct)
    {
        var result = await _emailService.ProcessEmailsAsync(ct);
        await svc.UpsertBulkAsync(_emailService.RigheElaborate, ct);
        return new IngestionResult { RecordsSaved = _emailService.RigheElaborate.Count };
    }
}
```

### 3. Creare gli handler produzione

```csharp
public abstract class Merc99StagingHandlerBase : IProductionDataHandler { /* come Ader4StagingHandlerBase */ }
public sealed class Merc99FaseXHandler : Merc99StagingHandlerBase { /* TipoRisultatoStaging = "FaseX" */ }
```

### 4. Aggiungere le costanti

```csharp
// LavorazioniCodes.cs
public const string MERC99 = "MERC99";
public const string MERC99_FASEX = "MERC99_FASEX";
```

### 5. Registrare in Program.cs

```csharp
// Processore
builder.Services.AddSingleton<IMailIngestionProcessor, Merc99IngestionProcessor>();

// Email service (mock/prod)
if (builder.Configuration.GetValue<bool>("MailServices:MERC99:UseMockService"))
    builder.Services.AddSingleton<Merc99EmailService, LocalCsvMerc99EmailService>();
else
    builder.Services.AddSingleton<Merc99EmailService>();

// Handler produzione
builder.Services.AddSingleton<IProductionDataHandler, Merc99FaseXHandler>();
```

> `GenericMailIngestionHandler` rileva automaticamente il nuovo processore tramite `IEnumerable<IMailIngestionProcessor>` — nessuna modifica necessaria all'orchestratore.

---

## 12. Troubleshooting

### Job MAIL_INGESTION non compare in Hangfire

Verificare in `Program.cs` la chiamata a `ScheduleMailIngestion()` e la registrazione di `GenericMailIngestionHandler`.

### Nessun record in DatiMailCsv dopo il job

- Controllare i log: `"nessuna email da processare"` indica che il filtro subject non ha trovato email
- Verificare che le email non siano già state archiviate in precedenza
- In modalità mock: verificare la presenza di file CSV in `TestData/{SERVIZIO}/`

### Task produzione non trova dati staging

```sql
SELECT * FROM DatiMailCsv
WHERE Elaborata = 0
  AND CodiceServizio = 'ADER4'
  AND TipoRisultato  = 'ScansioneCaptiva'
  AND DataLavorazione >= CAST(GETDATE() AS DATE);
```

Se la query restituisce righe ma il task non le trova, verificare il range di date nel `ProductionExecutionContext`.

### Duplicati in ProduzioneSistema dopo re-processing

Eliminare prima i record da `ProduzioneSistema` relativi alla data da rielaborare, poi eseguire il reset dello staging:

```sql
-- 1. Elimina produzione
DELETE FROM ProduzioneSistema
WHERE IdProcedura = 15 AND DataLavorazione = '2026-03-04';

-- 2. Reset staging
UPDATE DatiMailCsv
SET Elaborata = 0, ElaborataIl = NULL, ElaborataDaTaskId = NULL
WHERE CodiceServizio = 'ADER4' AND DataLavorazione = '2026-03-04';
```

---

**Autore**: Sistema DematReports
**Ultima modifica**: 2026-03-04
