# Documento dei Requisiti - BlazorDematReports

**Versione:** 1.0  
**Data:** 2026-01  
**Framework:** Blazor Server (.NET 10)  
**Database:** SQL Server + Oracle (via EF Core)  
**UI Library:** MudBlazor v7+  

---

## 1. PANORAMICA DEL SISTEMA

### 1.1 Scopo del Sistema
BlazorDematReports è un **sistema di gestione configurazioni e monitoraggio automatico della produzione** per flussi di lavoro di digitalizzazione documenti. Il sistema consente di:

- Configurare fonti dati multiple (SQL queries, Email CSV, Handler C# personalizzati)
- Schedulare task automatici con espressioni cron
- Estrarre dati di produzione da database eterogenei
- Monitorare performance e avanzamento lavorazioni in tempo reale
- Generare report e dashboard per analisi produttività operatori

### 1.2 Utenti Target
- **Amministratori di Sistema**: Configurazione fonti dati, gestione task, accesso completo
- **Supervisor**: Monitoraggio lavorazioni, gestione operatori, accesso ai report
- **Operatori**: Inserimento dati produzione, visualizzazione schede personali
- **Manager/Responsabili**: Consultazione dashboard e report aggregati

### 1.3 Architettura Tecnica

```
┌─────────────────────────────────────────────────────────┐
│         Presentation Layer (Blazor Server)              │
│  - Components/Pages/      (Razor pages)                 │
│  - Components/Shared/     (Reusable components)         │
│  - Services/Validation/   (Client-side validation)      │
└─────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│              Business Logic Layer                        │
│  - DataReading/Services/    (Query execution)           │
│  - Services/DataService/    (CRUD operations)           │
│  - ClassLibraryLavorazioni/ (Legacy handlers migration) │
│  - Infrastructure/          (Job scheduling)            │
└─────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│              Data Access Layer                           │
│  - Entities/Models/         (EF Core entities)          │
│  - Entities/Context/        (DbContext configurations)  │
│  - ConnectionStrings:       SQL Server + Oracle         │
└─────────────────────────────────────────────────────────┘
                            ▼
┌─────────────────────────────────────────────────────────┐
│          Task Scheduling (Hangfire)                      │
│  - Recurring jobs per configurazione                    │
│  - Guard Clause per task disabilitati                   │
│  - Cleanup job orfani automatico                        │
└─────────────────────────────────────────────────────────┘
```

---

## 2. REQUISITI FUNZIONALI

### 2.1 Gestione Configurazioni Fonti Dati

#### RF-01: Creazione Configurazione Fonte Dati
**Priorità:** Alta  
**Attori:** Amministratore, Supervisor

**Descrizione:**  
L'utente può creare una nuova configurazione specificando:
- **Tipo Fonte**: SQL, EmailCSV, HandlerIntegrato
- **Procedura Lavorazione** e **Fase Lavorazione**
- **Codice Univoco** (formato: `P{IdProc:D2}F{IdFase:D2}`, es. `P01F45`)
- **Descrizione** della configurazione

**Dettagli per Tipo Fonte:**

**SQL:**
- Nome connection string (da appsettings.json)
- Query SQL template con parametri obbligatori: `@startDate`, `@endDate`
- Validazione automatica:
  - SQL injection check (no DROP, TRUNCATE, ALTER, xp_cmdshell, etc.)
  - Sintassi T-SQL corretta (Microsoft parser)
  - Colonne obbligatorie: `Operatore`, `DataLavorazione`, `Documenti`, `Fogli`, `Pagine`
  - No `SELECT *` (elenco esplicito colonne richiesto)

**EmailCSV:**
- Servizio email (HERA16, ADER4 configurati in appsettings.MailServices)
- Pattern subject filtro
- Pattern nome allegato CSV
- Mapping automatico CSV → database

**HandlerIntegrato:**
- Nome classe handler C# (es. `PRATICHE_SUCCESSIONEHandler`)
- Implementazione interfaccia `ILavorazioneHandler`
- Discovery automatico via Dependency Injection

**Regole di Validazione:**
- Codice Univoco calcolato automaticamente (sola lettura)
- Query SQL max 1024 caratteri
- Connection string deve esistere in appsettings.json
- Handler class deve essere registrato nel DI container

**File Coinvolti:**
- `BlazorDematReports/Components/Pages/Impostazioni/ConfigurazioneFonti/PageConfiguraFonteDati.razor`
- `BlazorDematReports/Services/DataService/ServiceConfigurazioneFontiDati.cs`
- `BlazorDematReports/Services/Validation/SqlValidationService.cs`

---

#### RF-02: Mapping Fasi/Centri
**Priorità:** Alta  
**Attori:** Amministratore, Supervisor

**Descrizione:**  
Ogni configurazione può avere **N mapping** fase/centro per schedulazione granulare:

**Campi Mapping:**
- `IdProceduraLavorazione`: Riferimento procedura
- `IdFaseLavorazione`: Fase specifica (es. Scansione, Classificazione)
- `IdCentro`: Centro lavorazione (VERONA, GENOVA, etc.)
- `CronExpression`: Espressione scheduling (es. `"0 5 * * *"` = ore 5:00 ogni giorno)
- `GiorniPrecedenti`: Intervallo giorni per estrazione dati (default: 10)
- `TestoQueryTask`: Query override specifica per fase (opzionale)

**Regole di Business:**
- Nessun duplicato (Fase + Cron) nella stessa configurazione
- Almeno 1 mapping richiesto per creare task Hangfire
- Cron expression validata (sintassi cron standard)
- Se `TestoQueryTask` specificato, override query principale configurazione

**Validazioni:**
```csharp
// Verifica duplicati
if (_mappings.Any(m => m.IdFase == newMapping.IdFase && m.Cron == newMapping.Cron))
{
    Snackbar.Add("Combinazione Fase/Cron già esistente", Severity.Warning);
    return;
}
```

**File Coinvolti:**
- `Entities/Models/DbApplication/ConfigurazioneFaseCentro.cs`

---

#### RF-03: Test Query SQL in Fase di Configurazione
**Priorità:** Media  
**Attori:** Amministratore

**Descrizione:**  
Prima di salvare una configurazione SQL, l'utente può testare:
1. **Test Connessione**: Verifica che connection string sia valida
2. **Test Esecuzione Query**: Esegue query con date sample (`@startDate = oggi-7`, `@endDate = oggi`)
3. **Validazione Schema**: Verifica presenza colonne obbligatorie nel risultato

**Feedback Utente:**
- ✅ **Success**: "Query eseguita con successo. Colonne valide."
- ⚠️ **Warning**: "Query eseguita ma mancano colonne: Fogli, Pagine"
- ❌ **Error**: "Errore SQL: Timeout expired / Syntax error near 'FROM'"

**Metodi Servizio:**
```csharp
public async Task<ValidationResult> TestConnectionAsync(string connectionStringName)
public async Task<ValidationResult> TestQueryExecutionAsync(string connectionStringName, string query)
public ValidationResult ValidateColumnNames(string query)
```

**File Coinvolti:**
- `BlazorDematReports/Services/Validation/SqlValidationService.cs`

---

### 2.2 Gestione Task Schedulati

#### RF-04: Creazione Task da Configurazione
**Priorità:** Alta  
**Attori:** Amministratore

**Descrizione:**  
Button "Crea Task" genera automaticamente task Hangfire per tutti i mapping configurati.

**Processo:**
1. Verifica prerequisiti:
   - `NumeroFasi > 0` (almeno 1 mapping presente)
   - `TaskAttivi == 0` (nessun task già esistente)
2. Per ogni mapping `ConfigurazioneFaseCentro`:
   - Crea record `TaskDaEseguire` con stato `"pending"`
   - Genera chiave Hangfire univoca: `prod:{IdTaskDaEseguire}-{IdProc}:{proc-normalized}-{detail}`
   - Registra recurring job in Hangfire con cron expression
3. Aggiorna contatore `TaskAttivi` nella vista configurazioni

**Chiave Hangfire Formato:**
- **SQL/Handler**: `prod:123-10:hera16-scansione`
- **EmailCSV**: `mail:456-15:ader4-verona-sorter`

**Guard Clause in Esecuzione:**
```csharp
// In ProductionJobRunner.RunAsync
if (!task.Enabled)
{
    _logger.LogInformation("Task {TaskId} disabilitato via flag Enabled, skip esecuzione", taskId);
    return;
}
```

**File Coinvolti:**
- `BlazorDematReports/Services/DataService/TaskGenerationService.cs`
- `DataReading/Infrastructure/ProductionJobInfrastructure.cs`

---

#### RF-05: Gestione Avanzata Task (Dialog)
**Priorità:** Alta  
**Attori:** Amministratore, Supervisor

**Descrizione:**  
Dialog modale "Gestisci Task" per controllo granulare su task esistenti.

**Funzionalità Dialog:**
- **Elenco Task**: Mostra ID, Schedulazione (Cron), Stato (Enabled/Disabled), Ultimo Esito
- **Toggle Individuale**: Abilita/Disabilita singolo task (Guard Clause attiva immediatamente)
- **Disabilita Tutti**: Batch disable su tutti i task configurazione
- **Abilita Tutti**: Batch enable su tutti i task configurazione
- **Elimina Tutti**: Rimuove task da DB + Hangfire (irreversibile)
- **Contatore Dinamico**: "X attivi / Y disabilitati / Z totali"
- **Salva e Aggiorna Dashboard**: Conferma modifiche + refresh griglia configurazioni

**Comportamento Button "Salva e Aggiorna":**
1. Salva tutte le modifiche pendenti sul database
2. Chiude dialog
3. Ricarica griglia configurazioni (colonna `TaskAttivi` aggiornata)
4. Mostra Snackbar feedback operazione

**Limitazioni:**
- Dialog abilitato solo se `TaskAttivi > 0`
- Eliminazione task richiede conferma (MudDialog)

**File Coinvolti:**
- `BlazorDematReports/Components/Dialog/DialogGestioneTask.razor`
- `BlazorDematReports/Components/Pages/Impostazioni/PageListaConfigurazioniFonti.razor`

---

#### RF-06: Disabilitazione Configurazione (Soft Delete)
**Priorità:** Media  
**Attori:** Amministratore

**Descrizione:**  
Button "Disabilita" (🛑 Giallo) esegue soft delete:
- Imposta `FlagAttiva = false` su configurazione
- Mantiene mapping e storico task nel database
- Operazione reversibile (può essere riattivata)

**Prerequisiti:**
- `TaskAttivi == 0` (tutti i task devono essere disabilitati/eliminati prima)
- Se task attivi presenti: tooltip suggerisce "Usa 'Gestisci Task' per disabilitarli"

**Use Case:**
- Sospensioni temporanee per manutenzione
- Test/debugging configurazioni
- Disattivazione stagionale lavorazioni

---

#### RF-07: Eliminazione Configurazione (Hard Delete)
**Priorità:** Alta  
**Attori:** Amministratore

**Descrizione:**  
Button "Elimina" (🗑️ Rosso) esegue hard delete:
1. Elimina configurazione dal database
2. Elimina tutti i mapping `ConfigurazioneFaseCentro`
3. Elimina task da `TaskDaEseguire`
4. Rimuove job schedulati da Hangfire
5. Esegue cleanup orfani (`CleanupOrphansAsync`)

**Processo Eliminazione Task Orfani:**
```csharp
// STEP 1: Trova LavorazioniFasiDataReading per mapping
var lavorazioneFase = await context.LavorazioniFasiDataReadings
    .FirstOrDefaultAsync(lf => 
        lf.IdProceduraLavorazione == mapping.IdProc &&
        lf.IdFaseLavorazione == mapping.IdFase);

// STEP 2: Trova task associati
var tasksToRemove = await context.TaskDaEseguires
    .Where(t => t.IdConfigurazioneDatabase == config.Id &&
            t.IdLavorazioneFaseDateReading == lavorazioneFase.Id)
    .ToListAsync();

// STEP 3: Rimuovi job Hangfire (PRIMA del DB)
await productionScheduler.RemoveByKeyAsync(task.IdTaskHangFire);

// STEP 4: Elimina task dal database
context.TaskDaEseguires.RemoveRange(tasksToRemove);

// STEP 5: Cleanup finale orfani post-commit
await productionScheduler.CleanupOrphansAsync();
```

**Prerequisiti:**
- `TaskAttivi == 0`
- Conferma utente (MudDialog)

**File Coinvolti:**
- `BlazorDematReports/Services/DataService/ServiceConfigurazioneFontiDati.cs`

---

### 2.3 Esecuzione Automatica Estrazione Dati

#### RF-08: Esecuzione Task Schedulati
**Priorità:** Critica  
**Attori:** Sistema (Hangfire)

**Descrizione:**  
I task Hangfire eseguono automaticamente estrazione dati secondo cron expression configurata.

**Flusso Esecuzione:**

**1. Trigger Hangfire (Recurring Job)**
```csharp
// Hangfire invoca ProductionJobRunner.RunAsync(int taskId)
```

**2. Guard Clause Check**
```csharp
var task = await _db.TaskDaEseguires
    .Include(t => t.IdConfigurazioneDatabaseNavigation)
    .FirstOrDefaultAsync(t => t.IdTaskDaEseguire == taskId);

if (task == null || !task.Enabled)
{
    _logger.LogInformation("Task {TaskId} disabilitato, skip esecuzione", taskId);
    return; // Esecuzione interrotta
}
```

**3. Risoluzione Fonte Dati**
```csharp
var config = task.IdConfigurazioneDatabaseNavigation;

switch (config.TipoFonte)
{
    case TipoFonteData.SQL:
        await ExecuteSqlQueryAsync(config, task);
        break;
    
    case TipoFonteData.HandlerIntegrato:
        await ExecuteHandlerAsync(config, task);
        break;
    
    case TipoFonteData.EmailCSV:
        // Legacy: supporto handler mail (HERA16, ADER4)
        await ExecuteMailHandlerAsync(config, task);
        break;
}
```

**4. Esecuzione SQL Query**
```csharp
// DataReading/Services/QueryService.cs
public async Task<DataTable> ExecuteQueryAsync(
    string connectionString, 
    string queryString, 
    DateTime startDate, 
    DateTime endDate)
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var command = new SqlCommand(queryString, connection);
    command.CommandTimeout = 30; // seconds
    
    // Parametri obbligatori
    command.Parameters.Add("@startDate", SqlDbType.DateTime2).Value = startDate;
    command.Parameters.Add("@endDate", SqlDbType.DateTime2).Value = endDate;
    
    var dataTable = new DataTable();
    using var adapter = new SqlDataAdapter(command);
    adapter.Fill(dataTable);
    
    return dataTable;
}
```

**5. Calcolo Date Estrazione**
```csharp
// GiorniPrecedenti configurato nel mapping (default: 1)
DateTime endDate = DateTime.Today;
DateTime startDate = endDate.AddDays(-task.GiorniPrecedenti ?? -1);
```

**6. Salvataggio Risultati**
```csharp
// Inserimento batch in ProduzioneOperatori o ProduzioneSistema
foreach (DataRow row in resultTable.Rows)
{
    var produzione = new ProduzioneOperatori
    {
        IdOperatore = GetOperatoreId(row["Operatore"]),
        DataLavorazione = Convert.ToDateTime(row["DataLavorazione"]),
        IdProceduraLavorazione = task.IdProceduraLavorazione,
        IdFaseLavorazione = task.IdFaseLavorazione,
        // TipologieTotali nested collection
        TipologieTotaliProduziones = new List<TipologieTotaliProduzione>
        {
            new() { IdTipoTotale = 1, Totale = Convert.ToInt32(row["Documenti"]) },
            new() { IdTipoTotale = 2, Totale = Convert.ToInt32(row["Fogli"]) },
            new() { IdTipoTotale = 3, Totale = Convert.ToInt32(row["Pagine"]) }
        }
    };
    
    context.ProduzioneOperatoris.Add(produzione);
}

await context.SaveChangesAsync();
```

**7. Aggiornamento Stato Task**
```csharp
// Success
task.LastRunUtc = DateTime.UtcNow;
task.LastError = null;
task.ConsecutiveFailures = 0;

// Failure
task.LastRunUtc = DateTime.UtcNow;
task.LastError = ex.Message;
task.ConsecutiveFailures++;
```

**Gestione Errori:**
- Timeout query (30s): Log error, increment `ConsecutiveFailures`
- SQL Exception: Log stack trace, update `LastError`
- Connection failure: Retry automatico Hangfire (3 tentativi default)
- Handler exception: Log + notifica admin

**File Coinvolti:**
- `DataReading/Infrastructure/ProductionJobRunner.cs`
- `DataReading/Services/QueryService.cs`

---

#### RF-09: Esecuzione Handler Personalizzati
**Priorità:** Alta  
**Attori:** Sistema

**Descrizione:**  
Handler C# personalizzati per logiche complesse non esprimibili in SQL.

**Interfaccia Standard:**
```csharp
public interface ILavorazioneHandler
{
    string LavorazioneCode { get; }
    
    Task<List<DatiLavorazione>> ExecuteAsync(
        LavorazioneExecutionContext context, 
        CancellationToken ct = default);
    
    string? GetServiceCode() => null;
    HandlerMetadata GetMetadata() => new();
}
```

**Esempio Handler:**
```csharp
// ClassLibraryLavorazioni/Lavorazioni/Handlers/PRATICHE_SUCCESSIONEHandler.cs
public class PRATICHE_SUCCESSIONEHandler : ILavorazioneHandler
{
    public string LavorazioneCode => LavorazioniCodes.PRATICHE_SUCCESSIONE;
    
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        LavorazioneExecutionContext context, 
        CancellationToken ct)
    {
        // Logica custom: query multiple, trasformazioni, API esterne
        var connString = context.Configuration.GetConnectionString("CnxnPraticheSuccessione");
        
        // Query multi-step
        var documenti = await QueryDocumentiAsync(connString, context.StartDate, context.EndDate);
        var allegati = await QueryAllegatiAsync(connString, documenti.Select(d => d.Id));
        
        // Aggregazione dati
        return documenti.GroupBy(d => new { d.Operatore, d.DataLavorazione })
            .Select(g => new DatiLavorazione
            {
                Operatore = g.Key.Operatore,
                DataLavorazione = g.Key.DataLavorazione,
                Documenti = g.Count(),
                Fogli = g.Sum(d => d.NumFogli),
                Pagine = g.Sum(d => d.NumPagine)
            })
            .ToList();
    }
}
```

**Discovery Automatico:**
```csharp
// BlazorDematReports/Program.cs
services.Scan(scan => scan
    .FromAssemblies(typeof(ILavorazioneHandler).Assembly)
    .AddClasses(classes => classes.AssignableTo<ILavorazioneHandler>())
    .AsImplementedInterfaces()
    .WithTransientLifetime());
```

**File Coinvolti:**
- `ClassLibraryLavorazioni/Lavorazioni/Interfaces/ILavorazioneHandler.cs`
- `ClassLibraryLavorazioni/Lavorazioni/Handlers/*Handler.cs`

---

#### RF-10: Import Email CSV Automatico
**Priorità:** Media  
**Attori:** Sistema

**Descrizione:**  
Handler specializzati per import automatico CSV da allegati email Exchange.

**Servizi Email Configurati:**

**HERA16:**
- Mailbox: `verona.edp@postel.it`
- Subject filter: `"DEMAT_HERA16"`
- Attachment pattern: `"file di produzione giornaliera*.csv"`
- Fasi mappate: Scansione, Classificazione, Indicizzazione (VERONA)

**ADER4:**
- Mailbox: `verona.edp@postel.it`
- Subject filter Verona: `"DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)"`
- Subject filter Genova: `"DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)"`
- Attachment pattern: `"EQTMN4_Scatole_Scansionate*.csv"`
- Fasi mappate: Sorter, SorterBuste, Captiva (VERONA + GENOVA)

**Flusso Esecuzione:**
1. Connessione Exchange Web Services (EWS)
2. Ricerca email non lette con subject filter
3. Download allegati CSV matching pattern
4. Parsing CSV con mappatura colonne configurata
5. Inserimento dati `ProduzioneOperatori` + `TipologieTotaliProduzione`
6. Spostamento email in cartella archivio
7. Logging operazione con conteggio righe processate

**Configurazione appsettings.json:**
```json
"MailServices": {
  "HERA16": {
    "Username": "verona.edp",
    "Password": "use_user_secrets",
    "Domain": "postel.it",
    "ExchangeUrl": "https://postaweb.postel.it/ews/exchange.asmx",
    "SubjectFilter": "DEMAT_HERA16",
    "AttachmentPattern": "file di produzione giornaliera",
    "ArchiveFolder": "HERA16",
    "Fasi": {
      "Scansione": {
        "VERONA": { "IdProcedura": 10, "IdFase": 4 }
      }
    }
  }
}
```

**File Coinvolti:**
- `ClassLibraryLavorazioni/LavorazioniViaMail/HERA16/ProduzioneGiornaliera.cs`
- `ClassLibraryLavorazioni/LavorazioniViaMail/ADER4/ProduzioneGiornaliera.cs`

---

### 2.4 Monitoring e Reporting

#### RF-11: Dashboard Configurazioni
**Priorità:** Alta  
**Attori:** Amministratore, Supervisor

**Descrizione:**  
Pagina `PageListaConfigurazioniFonti.razor` mostra griglia con tutte le configurazioni.

**Colonne Griglia:**
- **Codice Configurazione**: `P01F45`
- **Descrizione**: "Estrazione dati HERA scansione Verona"
- **Tipo Fonte**: Chip colorato (SQL=Primary, Handler=Secondary, EmailCSV=Info)
- **Fasi Configurate**: Elenco fase + centro + cron (expandable)
- **Task Attivi**: Badge con contatore (es. "3 task")
- **Ultimo Aggiornamento**: Data modifica configurazione
- **Azioni**: Button Edit, Disabilita, Elimina, Gestisci Task, Crea Task

**Filtri:**
- Search bar globale (codice + descrizione)
- Filtro tipo fonte (dropdown)
- Filtro stato task (Attivi / Disabilitati / Tutti)

**Azioni Bulk:**
- "Disabilita Tutti i Task" (ADMIN only)
- "Abilita Tutti i Task" (ADMIN only)

**File Coinvolti:**
- `BlazorDematReports/Components/Pages/Impostazioni/PageListaConfigurazioniFonti.razor`

---

#### RF-12: Report Produzione Operatori
**Priorità:** Alta  
**Attori:** Supervisor, Operatori, Manager

**Descrizione:**  
Dashboard con grafici e tabelle produzione giornaliera per operatore/fase/centro.

**Visualizzazioni:**
- **Grafico Documenti per Operatore**: Column chart con drill-down per fase
- **Grafico Trend Settimanale**: Line chart con documenti/fogli/pagine nel tempo
- **Tabella Dettaglio Operatori**: Elenco produzione per data/operatore/fase con totali
- **Card KPI**: Totali giornalieri documenti, fogli, pagine, tempo lavorato

**Filtri Interattivi:**
- Date range picker (da/a)
- Dropdown procedura lavorazione
- Dropdown fase lavorazione
- Dropdown centro lavorazione
- Dropdown operatore (multi-select)

**Export Dati:**
- Excel (XLSX)
- CSV
- PDF report formattato

**File Coinvolti:**
- `BlazorDematReports/Components/Pages/ReportsLavorazioni/PageReportCompleto.razor`
- `BlazorDematReports/Services/DataService/ServiceProduzioneOperatori.cs`

---

#### RF-13: Scheda Lavorazione Dettaglio
**Priorità:** Media  
**Attori:** Supervisor, Manager

**Descrizione:**  
Vista dettagliata singola lavorazione con storico e statistiche.

**Informazioni Visualizzate:**
- **Header**: Nome procedura, cliente, centro, stato attivo
- **Fasi Lavorazione**: Card per ogni fase con:
  - Totali produzione (doc/fogli/pagine)
  - Numero operatori attivi
  - Task schedulati associati (stato + cron)
  - Grafici trend ultimi 30 giorni
- **Configurazioni Associate**: Link a configurazioni fonti dati
- **Operatori Attivi**: Elenco operatori con ore lavorate e produttività

**File Coinvolti:**
- `BlazorDematReports/Components/Pages/SchedaLavorazione/PageSchedaLavorazione.razor`

---

### 2.5 Autenticazione e Autorizzazione

#### RF-14: Login con Active Directory
**Priorità:** Alta  
**Attori:** Tutti

**Descrizione:**  
Sistema di autenticazione integrato con Active Directory aziendale.

**Modalità Login:**
1. **Active Directory** (produzione): Validazione LDAP su dominio `postel.it`
2. **Database locale** (sviluppo): Credenziali in tabella `Operatori`
3. **Test user** (configurabile): Auto-login per debug

**Configurazione appsettings.json:**
```json
"LoginSettings": {
  "Environment": "Default",
  "RequireActiveDirectory": false,
  "DefaultTestUser": "",
  "DefaultTestPassword": "",
  "ShowEnvironmentBadge": true,
  "AllowAutoLogin": false
},
"ActiveDirectory": {
  "Domain": "postel.it",
  "TimeoutSeconds": 30
}
```

**Processo Login:**
1. Utente inserisce username + password
2. Se `RequireActiveDirectory = true`: validazione LDAP
3. Query database `Operatori` per recuperare ruolo e centro origine
4. Creazione claim identity con ruoli
5. Set cookie autenticazione
6. Redirect a homepage

**File Coinvolti:**
- `BlazorDematReports/Components/Pages/Account/Login.cshtml.cs`
- `BlazorDematReports/Services/Authentication/ActiveDirectoryService.cs`

---

#### RF-15: Gestione Ruoli e Permessi
**Priorità:** Alta  
**Attori:** Amministratore

**Descrizione:**  
Sistema di autorizzazione basato su ruoli con gerarchia permessi.

**Ruoli Sistema:**
- **ADMIN**: Accesso completo, gestione configurazioni, task, utenti
- **SUPERVISOR**: Gestione lavorazioni, report avanzati, assegnazione operatori
- **RESPONSABILE**: Report e dashboard, sola lettura configurazioni
- **USER**: Inserimento dati produzione, visualizzazione scheda personale

**Matrice Autorizzazioni:**

| Funzionalità | ADMIN | SUPERVISOR | RESPONSABILE | USER |
|--------------|-------|------------|--------------|------|
| Configurazione Fonti Dati | ✅ CRUD | ✅ Read | ❌ | ❌ |
| Gestione Task | ✅ | ✅ | ❌ | ❌ |
| Report Produzione | ✅ | ✅ | ✅ Read | ❌ |
| Scheda Operatore | ✅ | ✅ | ✅ | ✅ Propria |
| Gestione Utenti | ✅ | ❌ | ❌ | ❌ |
| Impostazioni Sistema | ✅ | ❌ | ❌ | ❌ |

**Implementazione Blazor:**
```razor
@attribute [Authorize(Roles = "ADMIN,SUPERVISOR")]

<AuthorizeView Roles="ADMIN">
    <Authorized>
        <MudButton OnClick="DeleteConfiguration">Elimina</MudButton>
    </Authorized>
    <NotAuthorized>
        <MudText>Accesso negato</MudText>
    </NotAuthorized>
</AuthorizeView>
```

**File Coinvolti:**
- `BlazorDematReports/Application/ConfigUser.cs`
- `BlazorDematReports/Services/DataService/ServiceRuoli.cs`

---

## 3. REQUISITI NON FUNZIONALI

### 3.1 Performance

#### RNF-01: Tempi di Risposta
- **Caricamento pagina**: < 2 secondi
- **Esecuzione query SQL**: < 30 secondi (timeout configurato)
- **Rendering griglia configurazioni**: < 1 secondo per 100 record
- **Export Excel/PDF**: < 5 secondi per 1000 righe

#### RNF-02: Scalabilità
- Supporto fino a **500 configurazioni fonti dati** simultanee
- Gestione **10.000 task schedulati** in Hangfire
- Database ottimizzato per **milioni di record produzione**
- Concurrent users: **50+ utenti simultanei** (Blazor Server SignalR)

---

### 3.2 Sicurezza

#### RNF-03: Protezione SQL Injection
- Validazione query con regex pattern pericolosi
- Blocco keyword: `DROP`, `TRUNCATE`, `ALTER`, `xp_cmdshell`, `sp_executesql`
- Parametrizzazione obbligatoria: `@startDate`, `@endDate`
- Parser T-SQL Microsoft per sintassi validation

#### RNF-04: Autenticazione e Sessioni
- Cookie autenticazione con scadenza 8 ore
- Supporto Active Directory LDAP
- Password hashing (BCrypt/SHA256)
- Session timeout dopo 30 minuti inattività

#### RNF-05: Audit Log
- Logging tutte le modifiche configurazioni (CreatoDa, ModificatoDa, CreatoIl, ModificatoIl)
- Tracking esecuzioni task (LastRunUtc, LastError, ConsecutiveFailures)
- Log file strutturati JSON (Serilog)

---

### 3.3 Affidabilità

#### RNF-06: Gestione Errori Task
- Retry automatico Hangfire: 3 tentativi con backoff esponenziale
- Guard Clause per task disabilitati
- Contatore `ConsecutiveFailures` per alert amministratore
- Isolamento errori: un task failing non blocca gli altri

#### RNF-07: Data Integrity
- Transaction scope per salvataggio produzione (rollback su errore)
- Foreign key constraints su tutte le relazioni
- Validazione dati in input (lunghezza, tipo, range)

---

### 3.4 Usabilità

#### RNF-08: Interfaccia Utente
- **Framework**: MudBlazor Material Design
- **Responsiveness**: Supporto desktop + tablet (no mobile ottimizzato)
- **Accessibilità**: Contrasto colori WCAG AA, keyboard navigation
- **Feedback utente**: Snackbar per successo/errore, loading indicators

#### RNF-09: Localizzazione
- Lingua principale: **Italiano**
- Date format: `dd/MM/yyyy`
- Decimal separator: `,` (virgola)
- Currency: € (euro)

---

### 3.5 Manutenibilità

#### RNF-10: Code Quality
- **SonarQube compliance**: No critical issues, code smells < 5%
- **Test coverage**: > 70% unit tests (xUnit)
- **Documentazione**: XML comments su classi/metodi pubblici
- **Naming conventions**: Microsoft C# guidelines

#### RNF-11: Logging
- Structured logging con Serilog
- Log levels: `Debug` (development), `Information` (production), `Error` (always)
- Log storage: File + Application Insights (opzionale)
- No emoji/icons nei log

---

## 4. VINCOLI E ASSUNZIONI

### 4.1 Vincoli Tecnologici
- **.NET Version**: 10.0 (requisito minimo)
- **C# Version**: 14.0
- **Database**: SQL Server 2019+ (primary), Oracle 12c+ (secondary)
- **Browser supportati**: Chrome 120+, Edge 120+, Firefox 120+
- **Blazor Render Mode**: Server (SignalR WebSocket)

### 4.2 Vincoli Operativi
- **Deployment**: IIS 10+ su Windows Server 2019+
- **Ambiente**: On-premise (no cloud pubblico per dati sensibili)
- **Backup database**: Giornaliero con retention 30 giorni
- **Manutenzione**: Finestra ogni domenica 02:00-04:00

### 4.3 Assunzioni
- Active Directory disponibile 99.9% uptime
- Connection string configurate correttamente in appsettings.json
- Database sorgenti accessibili via rete interna
- Hangfire background service sempre attivo
- Mailbox Exchange con spazio sufficiente (> 10 GB)

---

## 5. ENTITÀ DATABASE PRINCIPALI

### 5.1 Tabella: ConfigurazioneFontiDati
**Descrizione:** Configurazione centralizzata fonti dati

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| IdConfigurazione | int (PK) | Identificativo univoco |
| CodiceConfigurazione | string(6) | Formato P{IdProc:D2}F{IdFase:D2} |
| DescrizioneConfigurazione | string(500) | Descrizione estesa |
| TipoFonte | enum | SQL, EmailCSV, HandlerIntegrato |
| ConnectionStringName | string(100) | Nome connection string (appsettings) |
| HandlerClassName | string(200) | Nome classe handler C# |
| CreatoDa | string(100) | Username creatore |
| CreatoIl | DateTime | Data creazione |
| ModificatoDa | string(100) | Username ultimo modificatore |
| ModificatoIl | DateTime | Data ultima modifica |

**Relazioni:**
- 1:N con `ConfigurazioneFaseCentro`
- 1:N con `TaskDaEseguire`

---

### 5.2 Tabella: ConfigurazioneFaseCentro
**Descrizione:** Mapping fase/centro per schedulazione granulare

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| IdFaseCentro | int (PK) | Identificativo univoco |
| IdConfigurazione | int (FK) | Riferimento configurazione |
| IdProceduraLavorazione | int (FK) | Riferimento procedura |
| IdFaseLavorazione | int (FK) | Riferimento fase |
| IdCentro | int (FK) | Riferimento centro lavorazione |
| CronExpression | string(50) | Espressione cron (es. "0 5 * * *") |
| GiorniPrecedenti | int | Intervallo giorni estrazione (default 1) |
| TestoQueryTask | string(1024) | Query override fase specifica |
| FlagAttiva | bool | Stato attivo mapping |

**Relazioni:**
- N:1 con `ConfigurazioneFontiDati`
- N:1 con `ProcedureLavorazioni`
- N:1 con `FasiLavorazione`
- N:1 con `CentriLavorazione`

---

### 5.3 Tabella: TaskDaEseguire
**Descrizione:** Task schedulati Hangfire

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| IdTaskDaEseguire | int (PK) | Identificativo univoco |
| IdTaskHangFire | string(200) | Chiave Hangfire (es. prod:123-10:hera16) |
| IdLavorazioneFaseDateReading | int (FK) | Riferimento lavorazione/fase |
| IdConfigurazioneDatabase | int (FK) | Riferimento configurazione |
| Stato | string(50) | pending, running, completed, failed |
| Enabled | bool | Flag abilitazione (Guard Clause) |
| CronExpression | string(50) | Schedulazione |
| GiorniPrecedenti | int | Intervallo estrazione dati |
| LastRunUtc | DateTime? | Ultima esecuzione UTC |
| LastError | string(2000) | Messaggio ultimo errore |
| ConsecutiveFailures | int | Contatore errori consecutivi |

**Relazioni:**
- N:1 con `ConfigurazioneFontiDati`
- N:1 con `LavorazioniFasiDataReading`

---

### 5.4 Tabella: ProcedureLavorazioni
**Descrizione:** Procedure di lavorazione (workflow)

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| IdproceduraLavorazione | int (PK) | Identificativo univoco |
| NomeProcedura | string(200) | Nome procedura (es. HERA16) |
| IdproceduraCliente | int (FK) | Riferimento cliente |
| Idcentro | int (FK) | Centro lavorazione principale |
| Attiva | bool | Stato attivo |
| NomeServizio | string(100) | Codice servizio (es. HERA16, ADER4) |

**Relazioni:**
- 1:N con `LavorazioniFasiDataReading`
- 1:N con `ProduzioneOperatori`

---

### 5.5 Tabella: ProduzioneOperatori
**Descrizione:** Dati produzione giornaliera operatori

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| IdProduzione | int (PK) | Identificativo univoco |
| IdOperatore | int (FK) | Riferimento operatore |
| IdProceduraLavorazione | int (FK) | Riferimento procedura |
| IdFaseLavorazione | int (FK) | Riferimento fase |
| DataLavorazione | Date | Data produzione |
| TempoLavOreCent | decimal(5,2) | Ore lavorate (formato centesimale) |
| IdCentro | int (FK) | Centro lavorazione |

**Relazioni:**
- N:1 con `Operatori`
- N:1 con `ProcedureLavorazioni`
- N:1 con `FasiLavorazione`
- 1:N con `TipologieTotaliProduzione` (Documenti, Fogli, Pagine)

---

## 6. STANDARD SQL QUERY

### 6.1 Parametri Obbligatori
Tutte le query SQL **DEVONO** utilizzare esattamente questi parametri:
- `@startDate` (DateTime2) - Data inizio estrazione
- `@endDate` (DateTime2) - Data fine estrazione

**❌ NON USARE**: `@startDataDe`, `@endDataDe` (legacy/deprecati)

### 6.2 Colonne Obbligatorie
Tutte le query produzione **DEVONO** restituire (case-insensitive):
1. `Operatore` - Identificativo operatore
2. `DataLavorazione` - Data produzione
3. `Documenti` - Conteggio documenti
4. `Fogli` - Conteggio fogli
5. `Pagine` - Conteggio pagine

### 6.3 Template Query Standard
```sql
SELECT
    OP_INDEX AS Operatore,
    CAST(DATA_INDEX AS DATE) AS DataLavorazione,
    COUNT(*) AS Documenti,
    SUM(CAST(NUM_PAG AS INT)) / 2 AS Fogli,
    SUM(CAST(NUM_PAG AS INT)) AS Pagine
FROM TableName
WHERE DATA_INDEX >= @startDate
  AND DATA_INDEX < DATEADD(DAY, 1, @endDate)
GROUP BY OP_INDEX, CAST(DATA_INDEX AS DATE)
ORDER BY DataLavorazione DESC, Operatore
```

### 6.4 Regole Validazione
- ✅ Solo query `SELECT` o `WITH` (CTE)
- ❌ No `SELECT *` - sempre elenco esplicito colonne
- ❌ No DML: `UPDATE`, `INSERT`, `DELETE`, `DROP`, `TRUNCATE`, `ALTER`, `CREATE`
- ❌ No stored procedure sistema: `xp_cmdshell`, `sp_executesql`, `sp_OA*`
- ❌ No commenti SQL: `--` o `/* */`
- ✅ Lunghezza massima: 1024 caratteri

---

## 7. WORKFLOW STANDARD UTENTE

### 7.1 Creazione Nuova Configurazione SQL

**Attori:** Amministratore

**Steps:**
1. Navigare a `/fonti-dati`
2. Clic button "Nuova Configurazione"
3. **Step 1 - Tipo Fonte**: Selezionare "SQL"
4. **Step 2 - Dettagli**:
   - Selezionare Procedura Lavorazione (dropdown)
   - Selezionare Fase Lavorazione (dropdown)
   - Codice Univoco auto-generato (es. `P10F04`)
   - Inserire Descrizione (es. "Estrazione HERA scansione Verona")
5. **Step 3 - Configurazione SQL**:
   - Selezionare Connection String (dropdown da appsettings)
   - Incollare Query SQL con parametri `@startDate/@endDate`
   - Clic "Valida Query" → verifica sintassi + colonne
   - Clic "Test Esecuzione" → esegue query con date sample
6. **Step 4 - Mapping Fasi/Centri**:
   - Clic "Aggiungi Mapping"
   - Selezionare Centro (VERONA / GENOVA)
   - Inserire Cron Expression (es. `0 5 * * *`)
   - Giorni Precedenti: `1` (default)
   - Clic "Aggiungi" → mapping salvato
   - Ripetere per altri centri/schedulazioni
7. Clic "Salva Configurazione" → conferma creazione
8. Redirect a lista configurazioni
9. Clic button "Crea Task" → genera task Hangfire
10. Verifica colonna "Task Attivi" = numero mappings

**Risultato:** Configurazione attiva con task schedulati automatici.

---

### 7.2 Gestione Task Esistenti

**Attori:** Amministratore, Supervisor

**Scenario:** Disabilitare temporaneamente task per manutenzione database

**Steps:**
1. Navigare a `/fonti-dati`
2. Individuare configurazione target
3. Clic button "Gestisci Task" → apre dialog modale
4. Dialog mostra elenco task con stato:
   - Task #123: `0 5 * * *` - ✅ Abilitato - Last Run: 2025-01-15 05:00
   - Task #124: `0 17 * * *` - ✅ Abilitato - Last Run: 2025-01-14 17:00
5. Toggle OFF task #123 → stato diventa ❌ Disabilitato
6. Contatore aggiorna: "1 attivo / 1 disabilitato / 2 totali"
7. Clic "Salva e Aggiorna Dashboard"
8. Dialog chiude, griglia ricarica
9. Colonna "Task Attivi" ora mostra "1 task"

**Risultato:** Task #123 non viene più eseguito da Hangfire (Guard Clause attiva).

---

### 7.3 Eliminazione Configurazione

**Attori:** Amministratore

**Prerequisiti:** Tutti i task devono essere disabilitati/eliminati

**Steps:**
1. Navigare a `/fonti-dati`
2. Individuare configurazione da eliminare
3. Se "Task Attivi" > 0:
   - Clic "Gestisci Task"
   - Clic "Elimina Tutti i Task"
   - Conferma dialog eliminazione
   - Clic "Salva e Aggiorna Dashboard"
4. Ora "Task Attivi" = 0, button "Elimina" abilitato
5. Clic button "Elimina" (🗑️ rosso)
6. Dialog conferma: "Eliminare configurazione P10F04? Operazione irreversibile."
7. Clic "Conferma"
8. Sistema:
   - Elimina task da DB
   - Rimuove job Hangfire
   - Elimina mapping fase/centro
   - Elimina configurazione
   - Esegue cleanup orfani
9. Snackbar: "Configurazione eliminata con successo"
10. Griglia ricarica senza configurazione

**Risultato:** Configurazione e task completamente rimossi.

---

## 8. INTEGRAZIONE HANGFIRE

### 8.1 Configurazione Dashboard
- **URL**: `/hangfire`
- **Autenticazione**: Richiede ruolo `ADMIN`
- **Features**:
  - Visualizzazione job schedulati
  - Storico esecuzioni (succeeded, failed)
  - Retry manuale job failed
  - Delete job da dashboard

### 8.2 Storage Database
- **Connection String**: `HangfireConnection` (appsettings.json)
- **Tabelle**: `Hangfire.Job`, `Hangfire.State`, `Hangfire.Set`, etc.
- **Retention**: 7 giorni dati succeeded, 30 giorni failed

### 8.3 Job Monitoring
```csharp
// Registrazione job recurring
RecurringJob.AddOrUpdate(
    recurringJobId: "prod:123-10:hera16-scansione",
    methodCall: () => _runner.RunAsync(123),
    cronExpression: "0 5 * * *",
    options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Local }
);
```

### 8.4 Cleanup Automatico
```csharp
// Servizio cleanup job orfani (daily @ 03:00)
RecurringJob.AddOrUpdate(
    "cleanup-orphans",
    () => _scheduler.CleanupOrphansAsync(),
    "0 3 * * *"
);
```

---

## 9. FILE E CARTELLE PRINCIPALI

### 9.1 Struttura Progetto
```
BlazorDematReports_10/
├── BlazorDematReports/          # Blazor Server app principale
│   ├── Components/
│   │   ├── Pages/               # Razor pages
│   │   │   ├── Impostazioni/    # Configurazioni
│   │   │   │   ├── PageListaConfigurazioniFonti.razor
│   │   │   │   └── ConfigurazioneFonti/
│   │   │   │       └── PageConfiguraFonteDati.razor
│   │   │   ├── ReportsLavorazioni/
│   │   │   │   └── PageReportCompleto.razor
│   │   │   └── SchedaLavorazione/
│   │   │       └── PageSchedaLavorazione.razor
│   │   ├── Dialog/
│   │   │   └── DialogGestioneTask.razor
│   │   └── Shared/              # Layout, navigation
│   ├── Services/
│   │   ├── DataService/
│   │   │   ├── ServiceConfigurazioneFontiDati.cs
│   │   │   └── TaskGenerationService.cs
│   │   └── Validation/
│   │       └── SqlValidationService.cs
│   ├── Dto/                     # Data Transfer Objects
│   ├── Interfaces/              # Service contracts
│   ├── Application/
│   │   └── ConfigUser.cs        # User context management
│   ├── Program.cs               # DI configuration
│   └── appsettings.json         # Configuration
│
├── DataReading/                 # Data extraction project
│   ├── Infrastructure/
│   │   ├── ProductionJobInfrastructure.cs
│   │   └── ProductionJobRunner.cs
│   ├── Services/
│   │   └── QueryService.cs      # SQL query execution
│   └── Interfaces/
│
├── Entities/                    # EF Core entities
│   ├── Models/
│   │   ├── DbApplication/       # Application tables
│   │   │   ├── ConfigurazioneFontiDati.cs
│   │   │   ├── TaskDaEseguire.cs
│   │   │   └── ProduzioneOperatori.cs
│   │   └── DbLavorazioni/       # Work procedures
│   └── Context/
│       └── DematReportsContext.cs
│
├── ClassLibraryLavorazioni/     # Handlers library
│   ├── Lavorazioni/
│   │   ├── Interfaces/
│   │   │   └── ILavorazioneHandler.cs
│   │   └── Handlers/
│   │       └── *Handler.cs      # Custom handlers
│   └── LavorazioniViaMail/
│       ├── HERA16/
│       │   └── ProduzioneGiornaliera.cs
│       └── ADER4/
│           └── ProduzioneGiornaliera.cs
│
└── Database/
    └── Migrations/              # SQL migration scripts
```

---

## 10. GLOSSARIO TECNICO

| Termine | Definizione |
|---------|-------------|
| **Configurazione Fonte Dati** | Entità che definisce sorgente dati (SQL/Email/Handler) per estrazione produzione |
| **Mapping Fase/Centro** | Associazione fase lavorazione + centro + schedulazione (cron) |
| **Task Schedulato** | Job Hangfire recurring generato da configurazione |
| **Guard Clause** | Check `Enabled` flag prima esecuzione task |
| **Cron Expression** | Espressione schedulazione (es. `0 5 * * *` = 05:00 ogni giorno) |
| **Handler Integrato** | Classe C# custom per logiche complesse non esprimibili in SQL |
| **Soft Delete** | Disattivazione logica (FlagAttiva = false) senza eliminazione fisica |
| **Hard Delete** | Eliminazione fisica da database |
| **Procedura Lavorazione** | Workflow produzione (es. HERA16, ADER4) |
| **Fase Lavorazione** | Step workflow (es. Scansione, Classificazione, Indicizzazione) |
| **Centro Lavorazione** | Sede operativa (VERONA, GENOVA) |
| **TipoFonteData** | Enum: SQL, EmailCSV, HandlerIntegrato |
| **Giorni Precedenti** | Intervallo giorni per estrazione dati (calcolo @startDate) |
| **Cleanup Orfani** | Rimozione job Hangfire senza task DB associato |

---

## 11. ROADMAP E TODO

### 11.1 Funzionalità Completate (Q4 2024 - Q1 2025)
- ✅ Sistema configurazione fonti dati unificato
- ✅ Dialog gestione task avanzata
- ✅ Validazione SQL injection con SqlValidationService
- ✅ Handler mail unificati (HERA16, ADER4) con ILavorazioneHandler
- ✅ Guard Clause task disabilitati
- ✅ Cleanup automatico job orfani
- ✅ Soft delete + hard delete configurazioni
- ✅ Test esecuzione query in wizard configurazione

### 11.2 In Sviluppo (Q1 2025)
- 🔧 Handler mail nuovi servizi (ADER4 Genova)
- 🔧 Test import CSV allegati email produzione
- 🔧 Miglioramento query personalizzate per utente
- 🔧 Conteggio servizi mail associati a lavorazione

### 11.3 Backlog Prioritario
- 📋 Tabelle appoggio per controllo dati caricati
- 📋 Dashboard visualizzazione tabelle appoggio
- 📋 Gestione multi-operatore per singolo scanner
- 📋 Alert email per task con errori consecutivi > 3
- 📋 Export configurazioni JSON (backup/restore)
- 📋 Import bulk configurazioni da Excel template

### 11.4 Preparazione Produzione
- 🚀 Migration database produzione (saved + import procedure nuove)
- 🚀 Import produzione sistema con IDENTITY_INSERT ON
- 🚀 Modifica documenti Isole Digitali (GE 2024/2025)
- 🚀 Colonna `UtilizzataDaSistema` in FasiLavorazione
- 🚀 Documentazione programmatore: aggiunta servizi mail
- 🚀 Grafico attività/sequenze

### 11.5 Issue Noti
- ⚠️ Query con campi data diversi da standard (gestione non uniformata)
- ⚠️ Handler mail: nessun test su macchina ufficio (solo CSV esempio)
- ⚠️ Configurazione JSON parametri colonne complessa (da semplificare)
- ⚠️ Lavorazioni con DataReading=true ma senza job associato (check necessario)

---

## 12. CONTATTI E RIFERIMENTI

**Team di Sviluppo:**  
- Amministratore Sistema: [email]
- Lead Developer: [email]
- Database Administrator: [email]

**Repository:**  
- GitHub: `https://github.com/diegolista673/BlazorDematReports_net_10_1`
- Branch principale: `master`

**Documentazione Tecnica:**
- `/Documentazione/Diagram1.md` - Diagrammi architettura
- `/docs/MIGRATION_FINAL_REPORT.md` - Report migrazione .NET 10
- `/.github/copilot-instructions.md` - Code conventions

**Ambienti:**
- **Sviluppo**: `https://localhost:7001`
- **Test**: `https://test-dematreports.postel.it`
- **Produzione**: `https://dematreports.postel.it`

---

**Fine Documento**  
**Ultima revisione:** 2025-01-15  
**Versione documento:** 1.0  
**Status:** ✅ Approvato per implementazione
