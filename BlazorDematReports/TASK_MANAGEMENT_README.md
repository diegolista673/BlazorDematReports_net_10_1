# ?? Task Granular Management - Documentazione

## ?? Overview

Sistema completo per la gestione granulare dei task in BlazorDematReports, che permette di configurare, modificare e gestire singolarmente ogni task schedulato con Hangfire.

## ? Funzionalità Implementate

### 1. Gestione Task Granulare
- **Tipologia Task Personalizzata**: Ogni task può essere di tipo SQL, EmailCSV, HandlerIntegrato o Pipeline
- **CRON Expression Modificabile**: Ogni task ha la sua espressione CRON configurabile
- **Query/Handler Specifici**: Ogni task può avere la sua query SQL o handler dedicato
- **Enable/Disable Individuale**: Ogni task può essere attivato/disattivato indipendentemente

### 2. UI Components
- **TaskManagerGrid**: Visualizzazione completa di tutti i task con statistiche
- **TaskEditorDialog**: Dialog per modificare la configurazione di un task esistente

### 3. Validazioni
- **Duplicati**: Previene la creazione di task con stessa Fase + CRON
- **SQL Injection**: Validazione delle query SQL tramite SqlValidationService
- **CRON Format**: Validazione formato espressione CRON

## ?? Struttura File

```
BlazorDematReports/
??? Components/Pages/Admin/Components/
?   ??? TaskManagerGrid.razor          # Griglia gestione task
?   ??? TaskEditorDialog.razor         # Dialog modifica task
??? Dto/
?   ??? ConfigurazioneTaskEditDto.cs   # DTO per edit task
?   ??? ConfigurazioneTaskDetailDto.cs # DTO per visualizzazione
??? Services/DataService/
?   ??? ServiceTaskManagement.cs       # Service layer gestione task
??? Interfaces/IDataService/
    ??? IServiceTaskManagement.cs      # Interfaccia service

Entities/Models/DbApplication/
??? ConfigurazioneFaseCentro.cs        # Entity con nuovi campi

Database/Migrations/
??? Add_TaskGranularConfiguration.sql  # Migration SQL
```

## ??? Database Schema

### Nuovi Campi in `ConfigurazioneFaseCentro`

| Campo | Tipo | Descrizione |
|-------|------|-------------|
| `TipoTask` | NVARCHAR(50) | Tipologia: SQL, EmailCSV, HandlerIntegrato, Pipeline |
| `CronExpression` | NVARCHAR(100) | Espressione CRON per scheduling |
| `TestoQueryTask` | NVARCHAR(MAX) | Query SQL specifica per il task |
| `MailServiceCode` | NVARCHAR(100) | Codice servizio mail (per EmailCSV) |
| `HandlerClassName` | NVARCHAR(255) | Nome classe handler (per Handler) |
| `EnabledTask` | BIT | Abilita/disabilita task (default: true) |
| `UltimaModificaTask` | DATETIME | Timestamp ultima modifica |

### Campi Legacy (Mantenuti per Backward Compatibility)

| Campo | Sostituito Da | Stato |
|-------|---------------|-------|
| `TestoQueryOverride` | `TestoQueryTask` | ?? Deprecato ma ancora usato come fallback |
| `ParametriExtra` | `CronExpression` | ?? Deprecato ma ancora usato (JSON) |

### Unique Index

```sql
CREATE UNIQUE NONCLUSTERED INDEX UQ_FaseCentro_Fase_Cron
ON ConfigurazioneFaseCentro (IdConfigurazione, IdFaseLavorazione, CronExpression)
WHERE CronExpression IS NOT NULL;
```

Previene la creazione di duplicati con stessa configurazione, fase e CRON.

## ?? Uso del Sistema

### 1. Eseguire la Migration

```sql
-- Eseguire lo script SQL
Database/Migrations/Add_TaskGranularConfiguration.sql
```

### 2. Visualizzare Task

Navigare su una configurazione esistente in **Admin ? Configura Fonte Dati**. 
Nella sezione "Gestione Task Schedulati" vengono mostrati:

- **KPI**: Task totali, attivi, in errore
- **Mappings Espandibili**: Ogni mapping mostra i suoi task
- **Task Details**: Stato, CRON, ultima esecuzione, errori

### 3. Modificare un Task

1. Click su icona **Edit** (matita) nella colonna Azioni
2. Modificare:
   - Tipologia Task
   - CRON Expression
   - Query SQL / Mail Service / Handler
   - Stato attivo/disattivo
3. Click **Salva**

Il sistema:
- ? Valida la configurazione
- ? Controlla duplicati (Fase + CRON)
- ? Aggiorna il recurring job in Hangfire
- ? Sincronizza il database

### 4. Eliminare un Task

1. Click su icona **Delete** (cestino)
2. Confermare l'eliminazione
3. Il task viene rimosso da:
   - Database (`TaskDaEseguire`)
   - Hangfire (recurring job)

### 5. Enable/Disable Task

- **Singolo Task**: Toggle switch nella griglia
- **Tutti i Task di un Mapping**: Pulsanti "Abilita Tutti" / "Disabilita Tutti"

## ?? Workflow Operativo

```
User ? TaskManagerGrid
  ?
Click "Edit Task"
  ?
TaskEditorDialog aperto
  ?
Modifica Tipo/CRON/Query
  ?
Validazione (Duplicati, SQL Injection, CRON Format)
  ?
ServiceTaskManagement.UpdateTaskConfigurationAsync()
  ?
?? Update ConfigurazioneFaseCentro (DB)
?? Update TaskDaEseguire (CRON + Enabled)
?? RecurringJob.AddOrUpdate (Hangfire)
  ?
Success ? Griglia aggiornata
```

## ??? Sicurezza

### SQL Injection Prevention
```csharp
var validation = SqlValidator.ValidateQuery(_taskEdit.TestoQueryTask);
if (!validation.IsValid)
{
    _queryError = validation.Message;
}
```

### Validazione Duplicati
```csharp
var isUnique = await ValidateUniqueTaskAsync(
    idConfigurazione, 
    idFase, 
    cronExpression, 
    excludeIdFaseCentro);

if (!isUnique)
{
    // Errore: Task duplicato rilevato
}
```

### Transaction Safety
```csharp
await using var tx = await context.Database.BeginTransactionAsync();
try
{
    // Update operations
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

## ?? Estensioni Future

### Possibili Miglioramenti
1. **Bulk Edit**: Modificare più task contemporaneamente
2. **Task Templates**: Salvare configurazioni come template riutilizzabili
3. **Task History**: Storico modifiche con rollback
4. **Advanced Scheduling**: UI wizard per CRON expression
5. **Task Dependencies**: Catene di task con dipendenze
6. **Notification System**: Alert su task falliti via email/Slack

### Migrazione Completa (TODO)
Per rimuovere completamente i campi legacy:

1. Migrare tutti i riferimenti a `ParametriExtra` ? `CronExpression`
2. Migrare tutti i riferimenti a `TestoQueryOverride` ? `TestoQueryTask`
3. Aggiornare `ServiceConfigurazioneFontiDati.ExtractCronFromJson()`
4. Rimuovere proprietà obsolete dall'entity

## ?? Troubleshooting

### Task non si aggiorna dopo modifica
- Verificare che il recurring job Hangfire sia attivo nel dashboard
- Controllare i log per errori di validazione

### Errore "Task duplicato"
- Due task non possono avere stessa Fase + CRON
- Modificare il CRON o la fase del task

### Query SQL rifiutata
- SqlValidationService ha rilevato pattern pericolosi
- Rivedere la query per evitare injection patterns

## ?? Support

Per problemi o domande:
1. Controllare i log in `NLog`
2. Verificare Hangfire Dashboard per stato jobs
3. Consultare la documentazione in `NOTE.txt`

## ?? Best Practices

1. **Nomenclatura CRON**: Usare commenti descrittivi (es: "0 5 * * * // Ogni giorno alle 05:00")
2. **Testing Query**: Testare sempre le query SQL prima di salvarle
3. **Backup Config**: Esportare configurazioni prima di modifiche massicce
4. **Monitoring**: Controllare regolarmente la sezione "Task in Errore"

---

**Versione**: 1.0  
**Data**: 2024  
**Autore**: BlazorDematReports Team
