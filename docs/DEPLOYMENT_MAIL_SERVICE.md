# ?? Guida Deployment: Sistema Mail Service Unificato

## ? Implementazione Completata

### File Modificati/Creati

| Tipo | File | Stato |
|------|------|-------|
| **Entity** | `Entities/Models/DbApplication/ProduzioneSistema.cs` | ? Modificato |
| **Service** | `ClassLibraryLavorazioni/LavorazioniViaMail/Services/UnifiedMailProduzioneService.cs` | ? Creato |
| **Handler** | `ClassLibraryLavorazioni/LavorazioniViaMail/Handlers/Ader4Handler.cs` | ? Creato |
| **Handler** | `ClassLibraryLavorazioni/LavorazioniViaMail/Handlers/Hera16EwsHandler.cs` | ? Aggiornato |
| **Constants** | `ClassLibraryLavorazioni/LavorazioniViaMail/Constants/JobConstants.cs` | ? Aggiornato |
| **Config** | `BlazorDematReports/appsettings.json` | ? Aggiornato |
| **DI** | `BlazorDematReports/Program.cs` | ? Aggiornato |
| **Migration** | `docs/migrations/ProduzioneSistema_AddMailMetadata.sql` | ? Creato |
| **Diagnostics** | `BlazorDematReports/Components/Pages/Diagnostics/PageHandlersDiagnostics.razor` | ? Creato |
| **Menu** | `BlazorDematReports/Components/Layout/MyNavMenu.razor` | ? Aggiornato |

---

## ?? Step di Deployment

### 1?? Stop Applicazione

**IMPORTANTE**: L'applicazione DEVE essere fermata per applicare le modifiche.

```powershell
# In Visual Studio
Debug ? Stop Debugging (Shift+F5)

# O chiudi Visual Studio
```

---

### 2?? Esegui Migration Database

Apri **SQL Server Management Studio** ed esegui:

```sql
-- File: docs/migrations/ProduzioneSistema_AddMailMetadata.sql

USE [DematReports]
GO

-- Aggiunge 3 colonne
ALTER TABLE ProduzioneSistema
ADD 
    EventoId VARCHAR(100) NULL,
    NomeAllegato VARCHAR(500) NULL,
    CentroElaborazione VARCHAR(50) NULL;

-- Crea indici
CREATE NONCLUSTERED INDEX IX_ProdSistema_EventoId
ON ProduzioneSistema(EventoId)
WHERE EventoId IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_ProdSistema_NomeAllegato
ON ProduzioneSistema(NomeAllegato)
WHERE NomeAllegato IS NOT NULL;

-- Verifica
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ProduzioneSistema'
  AND COLUMN_NAME IN ('EventoId', 'NomeAllegato', 'CentroElaborazione')
ORDER BY COLUMN_NAME;
```

**Output atteso**:
```
CentroElaborazione    varchar    50      YES
EventoId              varchar    100     YES
NomeAllegato          varchar    500     YES
```

---

### 3?? Configura User Secrets (Development)

**NON salvare password in appsettings.json!**

```powershell
# Apri PowerShell nella cartella BlazorDematReports

# Configura password HERA16
dotnet user-secrets set "MailServices:HERA16:Password" "200902hope*"

# Configura password ADER4
dotnet user-secrets set "MailServices:ADER4:Password" "200902hope*"

# Verifica secrets configurati
dotnet user-secrets list
```

**Output atteso**:
```
MailServices:HERA16:Password = 200902hope*
MailServices:ADER4:Password = 200902hope*
```

---

### 4?? Compila Soluzione

```powershell
# Dalla root della solution
dotnet build BlazorDematReports.sln --configuration Debug
```

**Verifica assenza errori di compilazione**.

---

### 5?? Avvia Applicazione

```powershell
# In Visual Studio
Debug ? Start Debugging (F5)

# O dalla CLI
cd BlazorDematReports
dotnet run
```

---

## ?? Verifica Installazione

### Test 1: Pagina Diagnostica Handler

1. **Login** come `ADMIN`
2. **Menu**: Settings ? ?? Diagnostica Handler
3. **URL**: `/diagnostics/handlers`

**Verifica**:
```
? Handler Lavorazioni SQL
   - DefaultLavorazioneHandler
   - Z0072370_28AUTHandler
   - Z0082041_SOFTLINEHandler
   - ANT_ADER4_SORTER_1_2Handler
   - PRATICHE_SUCCESSIONEHandler
   - RDMKT_RSPHandler

? Handler Mail Service
   - HERA16  ? DEVE ESSERE PRESENTE
   - ADER4   ? DEVE ESSERE PRESENTE

? Totale handler registrati: 8
```

**? Se "Nessun handler Mail registrato"**:

```
1. Verifica Program.cs contiene:
   builder.Services.AddScoped<IMailImportHandler, Hera16EwsHandler>();
   builder.Services.AddScoped<IMailImportHandler, Ader4Handler>();

2. Verifica che l'applicazione sia stata riavviata

3. Verifica errori di compilazione
```

---

### Test 2: Dropdown Servizi Mail

1. **Naviga**: `/procedure-lavorazioni/edit/{id}`
2. **Tab**: Servizi Mail
3. **Click**: "Aggiungi Servizio Mail"
4. **Dropdown "Servizio Mail"** deve contenere:
   - HERA16
   - ADER4

**Screenshot atteso**:
```
?? Aggiungi Servizio Mail ???????????
?                                   ?
? Servizio Mail: [HERA16 ?]        ?
?                 - HERA16          ?
?                 - ADER4           ?
?                                   ?
? Tipo Task:     [Giornaliero ?]   ?
?                                   ?
? [Aggiungi Servizio]               ?
?????????????????????????????????????
```

---

## ?? Test Funzionale Completo

### Scenario 1: Configurare HERA16

#### Step A: Crea Procedura (se non esiste)

```sql
-- Verifica/Crea procedura HERA16
IF NOT EXISTS (SELECT 1 FROM ProcedureLavorazioni WHERE NomeProcedura = 'HERA16')
BEGIN
    INSERT INTO ProcedureLavorazioni (
        NomeProcedura,
        IdCliente,
        IdCentro,
        Attiva
    ) VALUES (
        'HERA16',
        1, -- Sostituisci con ID cliente corretto
        1, -- Sostituisci con ID centro Verona
        1
    );
END

-- Recupera ID
SELECT IdproceduraLavorazione, NomeProcedura 
FROM ProcedureLavorazioni 
WHERE NomeProcedura = 'HERA16';
-- Supponiamo restituisca ID = 10
```

#### Step B: Crea Fasi

```sql
-- Fase Scansione (ID = 4)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione,
    IdProceduraLavorazione,
    FlagDataReading
) VALUES (4, 10, 1);

-- Fase Classificazione (ID = 5)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione,
    IdProceduraLavorazione,
    FlagDataReading
) VALUES (5, 10, 1);

-- Fase Indicizzazione (ID = 6)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione,
    IdProceduraLavorazione,
    FlagDataReading
) VALUES (6, 10, 1);
```

#### Step C: Crea Task HERA16 da UI

1. **Naviga**: `/procedure-lavorazioni/edit/10`
2. **Tab**: Servizi Mail
3. **Click**: Aggiungi Servizio Mail
4. **Compila**:
   - Servizio Mail: `HERA16`
   - Tipo Task: `Giornaliero` (esegue alle 02:00)
5. **Click**: Aggiungi Servizio
6. **Click**: ?? Salva (floating action bar)

#### Step D: Verifica Hangfire

1. **Naviga**: `/hangfire`
2. **Tab**: Recurring Jobs
3. **Cerca**: `mail_hera16`

**Verifica**:
```
Job ID: mail_hera16_daily
CRON: 0 2 * * *  (ogni giorno alle 02:00)
Next execution: [data prossima esecuzione]
Status: ? Scheduled
```

#### Step E: Test Manuale (Opzionale)

```sql
-- Simula esecuzione manuale
-- (Da fare solo in Development/Testing)

-- Verifica task creato
SELECT 
    t.IdTaskDaEseguire,
    t.MailServiceCode,
    t.IdTaskHangFire,
    t.Enabled,
    t.Stato
FROM TaskDaEseguire t
WHERE t.MailServiceCode = 'HERA16';
```

**Trigger manuale da Hangfire Dashboard**:
1. `/hangfire`
2. Recurring Jobs ? `mail_hera16_daily`
3. Click **"Trigger now"**

**Verifica logs**:
```
[HERA16] Inizio elaborazione
[HERA16] Connessione a Exchange: https://postaweb.postel.it/ews/exchange.asmx
[HERA16] Trovate 0 email con allegati (se nessuna email presente)
[HERA16] Completato: 0 righe totali
```

---

### Scenario 2: Configurare ADER4

#### Step A: Crea Procedure Verona + Genova

```sql
-- ADER4 Verona
INSERT INTO ProcedureLavorazioni (NomeProcedura, IdCliente, IdCentro, Attiva)
VALUES ('ADER4_VERONA', 1, 1, 1);
-- Supponiamo ID = 15

-- ADER4 Genova
INSERT INTO ProcedureLavorazioni (NomeProcedura, IdCliente, IdCentro, Attiva)
VALUES ('ADER4_GENOVA', 1, 2, 1);
-- Supponiamo ID = 16
```

#### Step B: Crea Fasi

```sql
-- VERONA - Fase Sorter (ID = 1)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (1, 15, 1);

-- VERONA - Fase Sorter Buste (ID = 2)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (2, 15, 1);

-- VERONA - Fase Captiva (ID = 3)
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (3, 15, 1);

-- GENOVA - Stesse fasi
INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (1, 16, 1);

INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (2, 16, 1);

INSERT INTO LavorazioniFasiDataReading (
    IdFaseLavorazione, IdProceduraLavorazione, FlagDataReading
) VALUES (3, 16, 1);
```

#### Step C: Crea Task ADER4

**IMPORTANTE**: Un solo task ADER4 gestisce **entrambi** i centri!

1. **Naviga**: `/procedure-lavorazioni/edit/15` (VERONA)
2. **Tab**: Servizi Mail
3. **Compila**:
   - Servizio Mail: `ADER4`
   - Tipo Task: `Temporizzato`
   - Orario: `08:00`
4. **Click**: Aggiungi Servizio
5. **Click**: ?? Salva

---

## ?? Verifica Dati in Database

### Query 1: Task Mail Service Configurati

```sql
SELECT 
    t.IdTaskDaEseguire,
    t.MailServiceCode,
    p.NomeProcedura,
    f.FaseLavorazione,
    CASE t.IdTask
        WHEN 1 THEN 'Giornaliero (02:00)'
        WHEN 2 THEN 'Temporizzato ' + CAST(t.TimeTask AS VARCHAR)
        WHEN 3 THEN 'Mensile'
    END AS TipoTask,
    t.Enabled,
    t.Stato,
    t.IdTaskHangFire
FROM TaskDaEseguire t
INNER JOIN LavorazioniFasiDataReading lf ON t.IdLavorazioneFaseDateReading = lf.IdLavorazioniFasiDateReading
INNER JOIN ProcedureLavorazioni p ON lf.IdProceduraLavorazione = p.IdproceduraLavorazione
INNER JOIN FasiLavorazione f ON lf.IdFaseLavorazione = f.IdFaseLavorazione
WHERE t.MailServiceCode IS NOT NULL
ORDER BY t.MailServiceCode, p.NomeProcedura;
```

**Output atteso**:
```
MailServiceCode  NomeProcedura     FaseLavorazione    TipoTask              Enabled  Stato
---------------  ----------------  -----------------  --------------------  -------  -----------
ADER4            ADER4_VERONA      SORTER             Temporizzato 08:00:00 1        CONFIGURED
HERA16           HERA16            SCANSIONE          Giornaliero (02:00)   1        CONFIGURED
```

---

### Query 2: Verifica Dati Inseriti (Dopo Esecuzione)

```sql
-- Dati HERA16 con metadata
SELECT TOP 10
    Operatore,
    DataLavorazione,
    Documenti,
    EventoId,
    NomeAllegato,
    CentroElaborazione,
    DataAggiornamento
FROM ProduzioneSistema
WHERE EventoId IS NOT NULL  -- Solo dati da mail service
  AND IdProceduraLavorazione = 10  -- HERA16
ORDER BY DataAggiornamento DESC;
```

**Output atteso** (dopo esecuzione job):
```
Operatore  DataLavorazione  Documenti  EventoId   NomeAllegato                        CentroElaborazione
---------  ---------------  ---------  ---------  ----------------------------------  ------------------
mario.rossi 2024-01-15      150        EVT12345   file_produzione_giornaliera_VR.csv  VERONA
luigi.verdi 2024-01-15      200        EVT12345   file_produzione_giornaliera_VR.csv  VERONA
```

---

## ?? Troubleshooting

### Problema 1: Handler Mail Non Appaiono in Dropdown

**Sintomo**: Dropdown "Servizio Mail" vuoto o mostra solo "Nessun servizio mail disponibile"

**Soluzione**:

1. **Verifica pagina diagnostica**: `/diagnostics/handlers`
   - Se "Nessun handler Mail registrato" ? vai a step 2
   - Se handler presenti ma dropdown vuoto ? problema cache, riavvia browser

2. **Verifica Program.cs**:
   ```csharp
   // DEVE contenere queste righe:
   builder.Services.AddScoped<IMailImportHandler, Hera16EwsHandler>();
   builder.Services.AddScoped<IMailImportHandler, Ader4Handler>();
   ```

3. **Riavvia applicazione completamente**:
   - Stop Visual Studio
   - Pulisci solution: `dotnet clean`
   - Rebuild: `dotnet build`
   - Start applicazione

---

### Problema 2: Errore "Handler non trovato per il codice 'HERA16'"

**Sintomo**: Exception durante esecuzione job Hangfire

**Soluzione**:

```sql
-- Verifica MailServiceCode nel database
SELECT MailServiceCode FROM TaskDaEseguire;

-- DEVE essere esattamente 'HERA16' o 'ADER4' (maiuscolo)
-- NON 'hera16' o 'Hera16'
```

**Correzione se necessario**:
```sql
UPDATE TaskDaEseguire
SET MailServiceCode = 'HERA16'
WHERE MailServiceCode LIKE '%hera%';

UPDATE TaskDaEseguire
SET MailServiceCode = 'ADER4'
WHERE MailServiceCode LIKE '%ader%';
```

---

### Problema 3: Email Non Processate

**Sintomo**: Job esegue ma nessun dato inserito in ProduzioneSistema

**Debug**:

1. **Verifica logs applicazione**:
   ```
   [HERA16] Trovate 0 email con allegati
   ```

2. **Verifica casella email**:
   - Email presenti in Inbox?
   - Subject contiene "DEMAT_HERA16"?
   - Email ha allegati CSV?

3. **Verifica credenziali Exchange**:
   ```csharp
   // User Secrets configurati?
   dotnet user-secrets list
   ```

4. **Test connessione manuale**:
   ```csharp
   // In PageHandlersDiagnostics.razor aggiungi test button
   private async Task TestHera16ConnectionAsync()
   {
       var service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
       service.Credentials = new WebCredentials(
           _configuration["MailServices:HERA16:Username"],
           _configuration["MailServices:HERA16:Password"],
           _configuration["MailServices:HERA16:Domain"]);
       service.Url = new Uri(_configuration["MailServices:HERA16:ExchangeUrl"]);
       
       try
       {
           var inbox = Folder.Bind(service, WellKnownFolderName.Inbox);
           _snackbar.Add($"Connessione OK! Email in inbox: {inbox.TotalCount}", Severity.Success);
       }
       catch (Exception ex)
       {
           _snackbar.Add($"Errore: {ex.Message}", Severity.Error);
       }
   }
   ```

---

### Problema 4: Dati Duplicati

**Sintomo**: Stessi dati inseriti più volte in ProduzioneSistema

**Soluzione**:

1. **Verifica EventoId univoco**:
   ```sql
   SELECT EventoId, COUNT(*) as Occorrenze
   FROM ProduzioneSistema
   WHERE EventoId IS NOT NULL
   GROUP BY EventoId
   HAVING COUNT(*) > 1;
   ```

2. **Aggiungi constraint unicità** (opzionale):
   ```sql
   CREATE UNIQUE NONCLUSTERED INDEX UQ_ProdSistema_Evento_Operatore_Data
   ON ProduzioneSistema(EventoId, Operatore, DataLavorazione, IdFaseLavorazione)
   WHERE EventoId IS NOT NULL;
   ```

---

## ?? Checklist Deployment Finale

- [ ] Migration SQL eseguita con successo
- [ ] User Secrets configurati
- [ ] Applicazione riavviata
- [ ] Compilazione OK (nessun errore)
- [ ] `/diagnostics/handlers` mostra HERA16 e ADER4
- [ ] Dropdown "Servizio Mail" contiene HERA16 e ADER4
- [ ] Task creato con MailServiceCode corretto
- [ ] Job visibile in `/hangfire` Recurring Jobs
- [ ] Test esecuzione manuale OK
- [ ] Dati inseriti in ProduzioneSistema con metadata
- [ ] Logs applicazione OK

---

## ?? Documentazione di Riferimento

- **Guida Implementazione**: `docs/MAIL_HANDLER_IMPLEMENTATION_GUIDE.md`
- **Migration SQL**: `docs/migrations/ProduzioneSistema_AddMailMetadata.sql`
- **Configurazione**: `appsettings.json` ? `MailServices`

---

## ?? Prossimi Step (Opzionali)

1. **Aggiungere altri servizi mail** (Equitalia, altri clienti)
2. **Implementare notifiche** email su errori
3. **Dashboard monitoring** esecuzioni mail service
4. **Export dati** con metadata per audit

---

**Versione**: 1.0  
**Data**: 2024  
**Autore**: Sistema Unificato Mail Service
