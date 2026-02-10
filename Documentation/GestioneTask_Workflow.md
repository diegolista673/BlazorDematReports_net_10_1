# Gestione Task e Configurazioni - Workflow

## Panoramica

Il sistema gestisce il ciclo di vita delle configurazioni fonti dati e dei relativi task schedulati tramite Hangfire.

---

## Workflow Creazione Task

I task vengono creati **automaticamente** dal wizard di configurazione, non dalla pagina elenco.

```
1. Utente apre /configura-fonte-dati-wizard (nuovo o modifica)
2. Compila 4 step: Tipo Fonte > Configurazione > Procedura > Mapping
3. Clic "Crea/Aggiorna Configurazione"
   ?
   ??? CREAZIONE (HandleFinishAsync)
   ?   ??? ServiceConfigurazioneFontiDati.AddConfigurazioneFontiDatiAsync()
   ?   ?   ??? Salva ConfigurazioneFontiDati + ConfigurazioneFaseCentro nel DB
   ?   ??? TaskGenerationService.GenerateTasksForConfigurationAsync()
   ?       ??? Per ogni mapping attivo:
   ?       ?   ??? Cerca/Crea LavorazioniFasiDataReading
   ?       ?   ??? Crea TaskDaEseguire (Enabled=true, IdTaskHangFire=temp-*)
   ?       ?   ??? ProductionJobScheduler.AddOrUpdateAsync()
   ?       ?       ??? Genera chiave Hangfire definitiva (prod:* o mail:*)
   ?       ?       ??? Aggiorna IdTaskHangFire nel DB
   ?       ?       ??? Registra RecurringJob in Hangfire
   ?       ??? Restituisce conteggio task creati/esistenti
   ?
   ??? MODIFICA (HandleFinishAsync)
       ??? ServiceConfigurazioneFontiDati.UpdateConfigurazioneFontiDatiAsync()
       ?   ??? Aggiorna configurazione
       ?   ??? Per mapping rimossi: elimina task + job Hangfire associati
       ?   ??? Aggiorna mapping esistenti
       ??? TaskGenerationService.GenerateTasksForConfigurationAsync()
       ?   ??? Crea task per nuovi mapping aggiunti
       ??? ProductionJobScheduler.CleanupOrphansAsync()
           ??? Rimuove job Hangfire orfani (senza task DB corrispondente)
```

---

## Workflow Esecuzione Task (Hangfire)

```
Hangfire trigger (cron expression)
  ?
  ??? ProductionJobRunner.RunAsync(idTaskDaEseguire)
      ?
      ??? Carica TaskDaEseguire con relazioni dal DB
      ?
      ??? Task non trovato?
      ?   ??? LogWarning + return (nessuna azione)
      ?
      ??? Task.Enabled == false? (Guard Clause)
      ?   ??? LogInformation "Task {id} disabilitato - esecuzione saltata" + return
      ?
      ??? DetermineJobTypeAndCode() (da ConfigurazioneFontiDati.TipoFonte)
      ?   ??? SQL         ? ExecuteUnifiedDataSourceAsync (query DB)
      ?   ??? EmailCSV    ? ExecuteUnifiedHandlerAsync (import mail)
      ?   ??? Handler     ? ExecuteUnifiedHandlerAsync (handler C#)
      ?
      ??? Successo ? MarkSuccess (Stato=COMPLETED, LastError=null)
      ??? Errore   ? MarkFailure (Stato=ERROR, LastError=message) + throw
```

---

## Azioni Disponibili nella Pagina Elenco (/fonti-dati)

La pagina elenco configurazioni offre **3 azioni** per ogni configurazione:

### 1. Modifica (Edit - Blu)

Naviga al wizard in modalità modifica. Il wizard gestisce aggiornamento configurazione, creazione/rimozione task per mapping modificati e cleanup job orfani.

### 2. Gestisci Task (Settings - Blu Info)

Apre `DialogGestioneTask.razor` per gestione granulare dei task associati.
Disponibile solo se la configurazione ha task (TaskAttivi > 0).

**Funzionalità dialog:**
- Toggle Enabled/Disabled per task singolo (Guard Clause: il job rimane in Hangfire ma l'esecuzione viene saltata)
- Disabilita tutti / Abilita tutti (batch)
- Elimina task singolo o tutti (rimuove da Hangfire + DB)
- Contatore real-time: X attivi / Y disabilitati / Z totali
- Visualizzazione ultimo esito e cron expression
- Button "Salva e Aggiorna Dashboard" per confermare e aggiornare la griglia

### 3. Elimina Definitiva (DeleteForever - Rosso)

Sempre abilitato. Se la configurazione ha task attivi, il dialog di conferma mostra il numero di task che verranno rimossi.

**Flusso eliminazione (`DeleteConfigurazioneFontiDatiAsync`):**
1. Rimuove job da Hangfire (`RemoveByKeyAsync`)
2. Elimina `TaskDaEseguire` dal DB
3. Elimina `ConfigurazioneFaseCentro` (mapping) dal DB
4. Elimina `ConfigurazioneFontiDati` dal DB

---

## Struttura Chiave Hangfire

```
Produzione: prod:{IdTask}-{IdProcedura}-{nome-procedura}:{nome-fase}
Mail:       mail:{IdTask}-{IdProcedura}-{nome-procedura}:{codice-servizio-mail}
```

Generata da `ProductionJobScheduler.BuildHangfireKey()`. Il token `temp-*` viene sostituito alla prima sincronizzazione.

---

## Entità Coinvolte

```
ConfigurazioneFontiDati (1)
  ??? ConfigurazioneFaseCentro (N) - mapping fase/centro/cron
  ??? TaskDaEseguire (N) - task schedulati in Hangfire
       ??? LavorazioniFasiDataReading - collegamento procedura/fase
```

---

## File Principali

| File | Responsabilità |
|------|---------------|
| `ConfigurazioneFontiWizard.razor` | Creazione/modifica configurazione + generazione task |
| `PageListaConfigurazioniFonti.razor` | Elenco configurazioni con azioni (modifica, gestisci task, elimina) |
| `DialogGestioneTask.razor` | Dialog gestione granulare task (toggle, elimina, batch) |
| `TaskGenerationService.cs` | Generazione automatica task da mapping |
| `ServiceConfigurazioneFontiDati.cs` | CRUD configurazioni + eliminazione cascata task |
| `ProductionJobInfrastructure.cs` | Scheduler Hangfire + Runner con Guard Clause |
