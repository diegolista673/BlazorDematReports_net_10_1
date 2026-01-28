# ?? Quick Start: Creazione Task da Configurazioni

## Come Creare Task Automaticamente

### Passo 1: Crea Configurazione Fonte Dati

1. Vai a `/admin/fonti-dati`
2. Clicca **+ Nuova Configurazione**
3. Compila il wizard:
   - Tipo fonte (SQL/Email/Handler/Pipeline)
   - Dettagli (Codice, Nome, Descrizione)
   - Configurazione specifica (Query/MailService/etc.)
   - **Mapping Fasi/Centri** (almeno 1 mapping!)

### Passo 2: Crea Task Automatici

**Opzione A: Auto-Creazione (CONSIGLIATA)** ?

```
Dashboard Configurazioni ? Trova configurazione ? Clicca ?? (PlayCircle verde)
```

**Cosa fa:**
- Crea 1 task Hangfire per ogni mapping fase/centro
- Imposta cron `0 5 * * *` (esecuzione giornaliera alle 05:00)
- Registra in Hangfire con chiave univoca
- Abilita task automaticamente

**Esempio Output:**
```
? 3 task creati e schedulati con successo!
?? 1 task già esistente
```

**Opzione B: Creazione Manuale**

```
Dashboard Task ? Nuovo Task ? Sistema Unificato ? Seleziona Configurazione
```

---

## ?? Pulsante Crea Task - Dettagli

### Quando È Abilitato

| Condizione | Icona ?? |
|------------|----------|
| Configurazione con mapping | ?? Verde (cliccabile) |
| Nessun mapping | ? Grigio (disabilitato) |
| Task già presenti | ? Grigio (disabilitato) |

### Cosa Crea

Per ogni mapping attivo in `ConfigurazioneFaseCentro`:

```csharp
TaskDaEseguire {
  IdConfigurazioneDatabase = [id della configurazione],
  IdLavorazioneFaseDateReading = [da Procedura+Fase],
  CronExpression = [cron dal mapping o "0 5 * * *" default],
  Enabled = true,
  // Campi legacy = NULL (IdQuery, QueryIntegrata, etc.)
}
```

**Cron Expression:**
- Se specificato nel mapping ? usa quello
- Se non specificato ? default `"0 5 * * *"` (05:00 giornaliero)
- Salvato in `ParametriExtra` JSON come `{"cron": "0 5 * * *"}`

Hangfire Key: `prod:{IdTask}-{IdProc}:{nomeprocedura}-{fase}`

---

## ?? Esempio Completo

### 1. Configurazione SQL

```
Codice: INPS_SCAN_VR
Nome: INPS Scansione Verona
Tipo: SQL
Connection: CnxnCaptiva206
Query: SELECT OP_SCAN as operatore, ...

Mapping:
?? Procedura: INPS Verona (5), Fase: Scansione (4), Centro: Verona (1)
?  ?? Schedulazione: Giornaliero 05:00 (cron: 0 5 * * *)
?? Procedura: INPS Verona (5), Fase: Indicizzazione (5), Centro: Verona (1)
   ?? Schedulazione: Ogni 4 ore (cron: 0 */4 * * *)
```

### 2. Clic su ?? Crea Task

Sistema crea automaticamente:

```
Task 1:
  IdTaskDaEseguire: 123
  IdConfigurazioneDatabase: 1
  IdLavorazioneFaseDateReading: 10 (INPS Verona - Scansione)
  Cron: 0 5 * * *  ? Dal mapping
  Hangfire Key: prod:123-5:inps-verona-scansione

Task 2:
  IdTaskDaEseguire: 124
  IdConfigurazioneDatabase: 1
  IdLavorazioneFaseDateReading: 11 (INPS Verona - Indicizzazione)
  Cron: 0 */4 * * *  ? Dal mapping (ogni 4 ore!)
  Hangfire Key: prod:124-5:inps-verona-indicizzazione
```

### 3. Esecuzione

**Task 1 - Ogni giorno alle 05:00:**
**Task 2 - Ogni 4 ore (00:00, 04:00, 08:00, etc.):**

1. Hangfire trigger task 123
2. `UnifiedDataSourceHandler` riceve context con `IdConfigurazioneDatabase=1`
3. Carica configurazione SQL dal DB
4. Trova mapping per Procedura=5, Fase=4, Centro=1
5. Esegue query SQL con parametri `@startData`, `@endData`
6. Applica parametri extra da JSON (se presenti)
7. Inserisce dati in `ProduzioneSistema`

---

## ?? Verifica Task Creati

### Dashboard Task (`/dashboard-task`)

```
Codice          | Procedura       | Fase        | Cron        | Stato
INPS_SCAN_VR    | INPS Verona     | Scansione   | 0 5 * * *   | ? Enabled
INPS_SCAN_VR    | INPS Verona     | Indicizz.   | 0 5 * * *   | ? Enabled
```

### Dashboard Configurazioni

```
Codice          | Tipo | Fasi | Task Attivi
INPS_SCAN_VR    | SQL  | 2    | 2          ? Contatore aggiornato
```

---

## ?? Gestione Errori

### Errori Comuni

| Errore | Causa | Soluzione |
|--------|-------|-----------|
| "LavorazioneFase non trovata" | Procedura+Fase non esistono in `LavorazioniFasiDataReading` | Verifica che la procedura e fase siano configurate |
| "Task già esistente" | Task già presente per questa configurazione+lavorazione | Normale, viene saltato |
| "Errore scheduling" | Problema registrazione Hangfire | Verifica log NLog |

### Output Tipico

```
? 2 task creati e schedulati con successo!
?? 1 task già esistente
?? LavorazioneFase non trovata per Proc=99, Fase=99
```

---

## ?? UI - Legenda Pulsanti

| Icona | Azione | Tooltip |
|-------|--------|---------|
| ?? Edit | Modifica configurazione | "Modifica configurazione" |
| ?? PlayCircle | **Crea task automatici** | "Crea task automatici per questa configurazione" |
| ?? ContentCopy | Duplica configurazione | "Duplica configurazione" |
| ??? Delete | Elimina (soft delete) | "Elimina configurazione" |

---

## ?? Prossimi Passi

1. ? Crea configurazione + mapping
2. ? Clicca ?? per auto-generare task
3. ? Attendi esecuzione scheduled (o trigger manuale da Hangfire)
4. ?? Verifica dati in `ProduzioneSistema`
5. ?? Visualizza report in dashboard produzione

---

**Nota:** I task creati automaticamente hanno cron `0 5 * * *` (05:00). 
Per personalizzare, modifica il task manualmente dalla dashboard task.
