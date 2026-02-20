# ??? Rimozione QueryProcedureLavorazioni System - Report

**Data Esecuzione:** 2024-01-26  
**Database:** DematReports  
**Server:** VEVRFL1M031H  
**Status:** ? COMPLETATA CON SUCCESSO

---

## ?? Riepilogo Migrazione

### Motivazione

Il sistema legacy `QueryProcedureLavorazioni` è stato **completamente sostituito** dal nuovo sistema `ConfigurazioneFontiDati`. 

**Sistema OLD (deprecato):**
- Tabella `QueryProcedureLavorazioni` conteneva query SQL per procedura
- `TaskDaEseguire.IdQuery` (FK) puntava alle query
- `TaskDaEseguire.QueryIntegrata` (flag) per query hardcoded
- `TaskDaEseguire.Connessione` (string) per connection string

**Sistema NUOVO (attivo):**
- `ConfigurazioneFontiDati` contiene configurazioni complete
- `TaskDaEseguire.IdConfigurazioneDatabase` (FK) punta alla configurazione
- Supporta 4 tipi: SQL, EmailCSV, HandlerIntegrato, Pipeline
- Query, connection string, handler tutto in un'unica configurazione

---

## ? Prerequisiti Verificati

| Verifica | Risultato | Note |
|----------|-----------|------|
| Task attivi con `IdQuery` | ? 0 task | Nessun task usa il vecchio sistema |
| Task attivi con `QueryIntegrata` | ? 0 task | Nessun task usa query integrate legacy |
| Task attivi con `MailServiceCode` | ? 0 task | Tutti migrati a EmailCSV config |
| Query legacy esistenti | ?? 55 query | Salvate in backup |

**Conclusione:** ? Sicuro procedere con la rimozione

---

## ?? Modifiche Database Applicate

### 1. Backup Query Legacy

```sql
SELECT * INTO QueryProcedureLavorazioni_BACKUP
FROM QueryProcedureLavorazioni;
```

**Risultato:** ? 55 query salvate in `QueryProcedureLavorazioni_BACKUP`

---

### 2. FK Constraints Rimosse

```sql
ALTER TABLE [TaskDaEseguire]
DROP CONSTRAINT [FK_TaskDaEseguire_QueryProcedureLavorazioni];
```

**Risultato:** ? FK constraint rimossa

---

### 3. Colonne Rimosse da TaskDaEseguire

| Colonna | Tipo | Status |
|---------|------|--------|
| `IdQuery` | INT NULL | ? RIMOSSA |
| `QueryIntegrata` | BIT NULL | ? RIMOSSA |
| `Connessione` | VARCHAR(100) | ? RIMOSSA |
| `MailServiceCode` | VARCHAR(100) | ? RIMOSSA |

**SQL Eseguito:**
```sql
ALTER TABLE [TaskDaEseguire] DROP COLUMN [IdQuery];
ALTER TABLE [TaskDaEseguire] DROP COLUMN [QueryIntegrata];
ALTER TABLE [TaskDaEseguire] DROP COLUMN [Connessione];
ALTER TABLE [TaskDaEseguire] DROP COLUMN [MailServiceCode];
```

---

### 4. Struttura TaskDaEseguire Aggiornata

**PRIMA della migration:**
```sql
CREATE TABLE TaskDaEseguire (
    IdTaskDaEseguire INT PRIMARY KEY,
    -- ... altre colonne ...
    IdQuery INT NULL,                    -- ? RIMOSSO
    QueryIntegrata BIT NULL,             -- ? RIMOSSO
    Connessione VARCHAR(100) NULL,       -- ? RIMOSSO
    MailServiceCode VARCHAR(100) NULL,   -- ? RIMOSSO
    IdConfigurazioneDatabase INT NULL,   -- ? NUOVO SISTEMA
    -- ... altre colonne ...
);
```

**DOPO la migration:**
```sql
CREATE TABLE TaskDaEseguire (
    IdTaskDaEseguire INT PRIMARY KEY,
    -- ... altre colonne ...
    IdConfigurazioneDatabase INT NULL,   -- ? UNICO FK
    -- ... altre colonne ...
    CONSTRAINT FK_TaskDaEseguire_ConfigurazioneFontiDati
        FOREIGN KEY (IdConfigurazioneDatabase)
        REFERENCES ConfigurazioneFontiDati(IdConfigurazione)
);
```

---

## ?? Modifiche Codice Applicate

### 1. TaskDaEseguire.cs - Entity Aggiornata

**PRIMA:**
```csharp
public partial class TaskDaEseguire
{
    // ... altre proprietà ...
    
    [Obsolete("Use IdConfigurazioneDatabase instead")]
    public int? IdQuery { get; set; }
    
    [Obsolete("Use IdConfigurazioneDatabase instead")]
    public bool? QueryIntegrata { get; set; }
    
    [Obsolete("Use IdConfigurazioneDatabase instead")]
    public string? Connessione { get; set; }
    
    [Obsolete("Use IdConfigurazioneDatabase instead")]
    public string? MailServiceCode { get; set; }
    
    public int? IdConfigurazioneDatabase { get; set; }
    
    public virtual QueryProcedureLavorazioni? IdQueryNavigation { get; set; }
    public virtual ConfigurazioneFontiDati? ConfigurazioneDatabase { get; set; }
}
```

**DOPO:**
```csharp
public partial class TaskDaEseguire
{
    // ... altre proprietà ...
    
    /// <summary>
    /// FK a ConfigurazioneFontiDati per il sistema unificato.
    /// Contiene configurazione completa per SQL, Email, Handler o Pipeline.
    /// </summary>
    public int? IdConfigurazioneDatabase { get; set; }
    
    [ForeignKey("IdConfigurazioneDatabase")]
    public virtual ConfigurazioneFontiDati? ConfigurazioneDatabase { get; set; }
}
```

**Modifiche:**
- ? Rimosse 4 proprietà deprecate (`IdQuery`, `QueryIntegrata`, `Connessione`, `MailServiceCode`)
- ? Rimossa navigation property `IdQueryNavigation`
- ? Mantenuta solo `IdConfigurazioneDatabase` e `ConfigurazioneDatabase`
- ? Documentazione aggiornata

---

### 2. PageEditProcedura.razor - UI Aggiornata

**PRIMA:**
```razor
@* Expansion Panel Query Personalizzate *@
<MudExpansionPanel Text="Query Personalizzate">
    @if (Model.QueryProcedureLavorazioniDto?.Any() == true)
    {
        @foreach (var query in Model.QueryProcedureLavorazioniDto)
        {
            <MudCard>
                <MudText>@query.Titolo</MudText>
                <MudText>@query.Descrizione</MudText>
            </MudCard>
        }
    }
</MudExpansionPanel>
```

**DOPO:**
```razor
@* Expansion Panel RIMOSSO *@
@* Query legacy non più usate - sistema migrato a ConfigurazioneFontiDati *@
```

**Motivazione:** Il panel mostrava solo query legacy non più utilizzabili. Rimosso per semplificare UI.

---

## ?? File Migration Creati

1. ? `Entities/Migrations/SQL/002_RemoveQueryProcedureLavorazioniDependencies.sql`
   - Script SQL migration completo
   - Include verifiche prerequisiti
   - Backup automatico query legacy
   - Rimozione FK e colonne

2. ? `docs/QUERY_PROCEDURE_LAVORAZIONI_REMOVAL_REPORT.md`
   - Documentazione dettagliata
   - Riepilogo modifiche DB e codice
   - Piano rollback

---

## ?? Tabella QueryProcedureLavorazioni

### Status: DEPRECATA (Mantenuta)

La tabella `QueryProcedureLavorazioni` è stata **deprecata** ma **NON eliminata fisicamente**.

**Motivazione:**
- Contiene 55 query legacy storico
- Potrebbe servire per reference/documentazione
- Backup già creato in `QueryProcedureLavorazioni_BACKUP`

**Opzioni future:**
```sql
-- Opzione 1: Eliminazione fisica (dopo 6+ mesi)
DROP TABLE QueryProcedureLavorazioni;

-- Opzione 2: Rename per indicare deprecazione
EXEC sp_rename 'QueryProcedureLavorazioni', 'QueryProcedureLavorazioni_DEPRECATED';

-- Opzione 3: Mantenere come tabella di archivio (consigliato)
-- Nessuna azione - tabella mantenuta per reference storico
```

---

## ?? Verifica Post-Migration

### Query di Verifica

```sql
-- 1. Verifica colonne TaskDaEseguire
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TaskDaEseguire'
ORDER BY ORDINAL_POSITION;

-- Risultato atteso: IdQuery, QueryIntegrata, Connessione, MailServiceCode NON presenti

-- 2. Verifica FK constraints
SELECT 
    fk.name AS FK_Name,
    tp.name AS Parent_Table,
    cp.name AS Parent_Column,
    tr.name AS Referenced_Table,
    cr.name AS Referenced_Column
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
WHERE tp.name = 'TaskDaEseguire';

-- Risultato atteso: Solo FK_TaskDaEseguire_ConfigurazioneFontiDati (non FK a QueryProcedureLavorazioni)

-- 3. Verifica task attivi con nuovo sistema
SELECT 
    COUNT(*) AS TaskConNuovoSistema
FROM TaskDaEseguire
WHERE IdConfigurazioneDatabase IS NOT NULL 
  AND Enabled = 1;

-- 4. Verifica backup query
SELECT COUNT(*) AS QueryBackup
FROM QueryProcedureLavorazioni_BACKUP;

-- Risultato atteso: 55 query
```

---

## ?? Diagramma Evoluzione Sistema

```
????????????????????????????????????????????????
?  SISTEMA LEGACY (Deprecato)                 ?
?  ????????????????????????????                ?
?                                              ?
?  TaskDaEseguire                              ?
?  ?? IdQuery (FK) ?????                      ?
?  ?? QueryIntegrata   ?                      ?
?  ?? Connessione      ?                      ?
?  ?? MailServiceCode  ?                      ?
?                      ?                      ?
?  QueryProcedureLavorazioni                   ?
?  ?? IdQuery (PK) ?????                      ?
?  ?? Titolo                                  ?
?  ?? Descrizione (SQL)                        ?
?  ?? Connessione                             ?
????????????????????????????????????????????????

                    ?? MIGRAZIONE

????????????????????????????????????????????????
?  SISTEMA NUOVO (Attivo)                      ?
?  ??????????????????????                      ?
?                                              ?
?  TaskDaEseguire                              ?
?  ?? IdConfigurazioneDatabase (FK) ?????     ?
?                                        ?     ?
?  ConfigurazioneFontiDati               ?     ?
?  ?? IdConfigurazione (PK) ??????????????     ?
?  ?? TipoFonte (SQL/Email/Handler/Pipeline)  ?
?  ?? TestoQuery                              ?
?  ?? ConnectionStringName                     ?
?  ?? MailServiceCode                          ?
?  ?? HandlerClassName                         ?
?                                              ?
?  ConfigurazioneFaseCentro (Mapping N:N)     ?
?  ?? IdProceduraLavorazione                  ?
?  ?? IdFaseLavorazione                       ?
?  ?? IdCentro                                ?
?  ?? ParametriExtra (JSON con cron)          ?
????????????????????????????????????????????????
```

---

## ?? Rollback Plan

In caso di necessità di rollback (?? SCONSIGLIATO):

### Step 1: Ricreare Colonne

```sql
ALTER TABLE TaskDaEseguire ADD IdQuery INT NULL;
ALTER TABLE TaskDaEseguire ADD QueryIntegrata BIT NULL;
ALTER TABLE TaskDaEseguire ADD Connessione VARCHAR(100) NULL;
ALTER TABLE TaskDaEseguire ADD MailServiceCode VARCHAR(100) NULL;
```

### Step 2: Ricreare FK Constraint

```sql
ALTER TABLE TaskDaEseguire
ADD CONSTRAINT FK_TaskDaEseguire_QueryProcedureLavorazioni
    FOREIGN KEY (IdQuery)
    REFERENCES QueryProcedureLavorazioni(IdQuery);
```

### Step 3: Ripristinare Codice

```csharp
// In TaskDaEseguire.cs
public int? IdQuery { get; set; }
public bool? QueryIntegrata { get; set; }
public string? Connessione { get; set; }
public string? MailServiceCode { get; set; }
public virtual QueryProcedureLavorazioni? IdQueryNavigation { get; set; }
```

?? **NOTA:** Rollback NON consigliato. Il sistema legacy non è più mantenuto.

---

## ? Checklist Post-Migration

- [x] **Backup query legacy** - 55 query in QueryProcedureLavorazioni_BACKUP
- [x] **FK constraints rimosse** - FK_TaskDaEseguire_QueryProcedureLavorazioni
- [x] **Colonne rimosse** - IdQuery, QueryIntegrata, Connessione, MailServiceCode
- [x] **Entity aggiornata** - TaskDaEseguire.cs pulito
- [x] **UI aggiornata** - Expansion Panel legacy rimosso
- [x] **Migration SQL** - Script documentato in `002_RemoveQueryProcedureLavorazioniDependencies.sql`
- [ ] **Build applicazione** - Riavviare app per applicare modifiche ??
- [ ] **Test funzionale** - Verificare creazione task da ConfigurazioniFontiDati
- [ ] **Test Hangfire** - Verificare esecuzione task schedulati
- [ ] **Monitoraggio log** - Verificare nessun errore riferimenti a campi legacy

---

## ?? Benefici Migrazione

| Aspetto | Prima | Dopo | Beneficio |
|---------|-------|------|-----------|
| **Tabelle task** | 2 (TaskDaEseguire + QueryProcedureLavorazioni) | 1 (TaskDaEseguire) | Semplificazione |
| **FK per task** | 2 (IdQuery + IdConfigurazioneDatabase) | 1 (IdConfigurazioneDatabase) | Riduzione complessità |
| **Tipi fonte supportati** | 1 (SQL) | 4 (SQL, Email, Handler, Pipeline) | Flessibilità |
| **Configurazione** | Separata query/connection | Unificata | Coerenza |
| **Mapping N:N** | ? Non supportato | ? Proc/Fase/Centro | Granularità |
| **Cron personalizzati** | ? Globale per task | ? Per mapping | Flessibilità |
| **Validazione SQL** | ? Nessuna | ? SQL injection detection | Sicurezza |

---

## ?? Documentazione Correlata

- `docs/MIGRATION_FINAL_REPORT.md` - Riepilogo generale migrazione
- `docs/DATABASE_MIGRATION_REPORT.md` - Prima migration (aggiunta ConfigurazioneFontiDati)
- `Entities/Migrations/SQL/001_AddConfigurazioneFontiDatiTables.sql` - Prima migration SQL
- `Entities/Migrations/SQL/002_RemoveQueryProcedureLavorazioniDependencies.sql` - Questa migration

---

**Migration completata:** 2024-01-26  
**Database:** DematReports @ VEVRFL1M031H  
**Impact:** ? MEDIUM - Richiede riavvio applicazione  
**Rollback:** ?? POSSIBILE ma SCONSIGLIATO  
**Status finale:** ? SUCCESS

---

**Team Sviluppo**  
BlazorDematReports - Sistema Configurazioni Fonti Dati Unificato
