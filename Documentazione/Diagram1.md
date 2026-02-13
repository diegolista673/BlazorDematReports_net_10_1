```mermaid
sequenceDiagram
    actor Utente
    participant UI as PageConfiguraFonteDati<br/>(Wizard)
    participant State as ConfigurationWizardStateService
    participant Service as ServiceConfigurazioneFontiDati
    participant DB as DematReportsContext
    participant Scheduler as ProductionJobScheduler
    participant Hangfire as Hangfire RecurringJobManager

    Note over Utente,Hangfire: FASE 1: WIZARD STEP-BY-STEP

    Utente->>UI: Avvia configurazione
    UI->>State: Reset()
    State-->>UI: Stato iniziale

    Note over Utente,State: Step 1: Tipo Fonte
    Utente->>UI: Seleziona TipoFonte (SQL/Handler)
    UI->>State: UpdateState(s => s.WithTipoFonte(...))
    State-->>UI: Stato aggiornato
    UI->>UI: NextStep()

    Note over Utente,State: Step 2: Configurazione Specifica
    alt TipoFonte = SQL
        Utente->>UI: Seleziona Connection String
        UI->>State: UpdateState(s => s.WithConnectionString(...))
        Utente->>UI: Click "Testa Connessione"
        UI->>Service: SqlValidator.TestConnectionAsync(connString)
        Service->>Service: SqlConnection.OpenAsync()
        Service-->>UI: ValidationResult
        UI->>State: UpdateState(s => s.WithValidationMessage(...))
    else TipoFonte = HandlerIntegrato
        Utente->>UI: Seleziona Handler C#
        UI->>State: UpdateState(s => s.WithHandler(...))
    end
    UI->>UI: NextStep()

    Note over Utente,State: Step 3: Procedura e Fasi
    Utente->>UI: Seleziona Procedura
    UI->>Service: GetFasiByProceduraAsync(idProcedura)
    Service->>DB: Query LavorazioniFasiDataReading
    DB-->>Service: Lista FasiLavorazione
    Service-->>UI: Fasi disponibili
    UI->>State: UpdateState(s => s.WithProcedura(...))
    UI->>UI: NextStep()

    Note over Utente,Hangfire: Step 4: Definizione Mappings

    loop Aggiunta mappings
        Utente->>UI: Click "Aggiungi Mapping"
        UI->>UI: DialogMapping.ShowAsync()
        Utente->>UI: Compila Fase, Centro, Cron, Query/Handler
        UI->>State: UpdateState(s => s.AddMapping(...))
        State-->>UI: Mappings in memoria
    end

    Note over Utente,Hangfire: FASE 2: SALVATAGGIO (Config + Mappings + Task)

    Utente->>UI: Click "Salva Configurazione"
    UI->>State: State.BuildConfigurazioneFontiDati()
    State-->>UI: ConfigurazioneFontiDati + Mappings
    UI->>Service: AddConfigurazioneFontiDatiAsync(config, mappings, user)

    activate Service
    Service->>DB: BeginTransaction()
    Service->>DB: ConfigurazioneFontiDatis.Add(config)
    Service->>DB: SaveChangesAsync()

    loop Per ogni mapping
        Service->>DB: ConfigurazioneFaseCentros.Add(mapping)
    end
    Service->>DB: SaveChangesAsync()

    Service->>Service: UpdateFlagDataReadingForMappingsAsync()
    Service->>DB: UPDATE FlagDataReading
    Service->>DB: Commit Transaction

    loop Per ogni mapping creato
        Service->>DB: Query LavorazioniFasiDataReading
        Service->>Service: Crea TaskDaEseguire
        Service->>DB: TaskDaEseguires.Add(task)
        Service->>DB: SaveChangesAsync()

        Service->>Scheduler: AddOrUpdateAsync(IdTaskDaEseguire)
        activate Scheduler
        Scheduler->>DB: Query TaskDaEseguire con relazioni
        Scheduler->>Scheduler: BuildHangfireKey(task)
        Scheduler->>DB: UPDATE IdTaskHangFire, CronExpression
        Scheduler->>Hangfire: AddOrUpdate(hangfireKey, idTask, cron)
        Hangfire-->>Scheduler: Job registrato
        deactivate Scheduler
    end
    deactivate Service

    Service-->>UI: Successo
    UI->>Utente: Snackbar "Configurazione salvata e N task creati"
    UI->>UI: NavigateTo("/lista-configurazioni")

    Note over Utente,Hangfire: MODALITÀ EDIT

    Utente->>UI: Click "Modifica" su configurazione
    UI->>Service: GetConfigurazioneFontiDatiAsync(idConfig)
    Service->>DB: Query ConfigurazioneFontiDati + Mappings
    Service-->>UI: Dati caricati
    UI->>State: LoadEditState(config, mappings, ...)

    Utente->>UI: Modifica mappings
    UI->>State: UpdateState(...)
    Utente->>UI: Click "Salva Modifiche"
    UI->>Service: UpdateConfigurazioneFontiDatiAsync(config, mappings, user)
    Service->>DB: UPDATE Config + Sync Mappings + Delete Task orfani
    Service-->>UI: Successo

    Note over Utente,Hangfire: FASE 3: ESECUZIONE AUTOMATICA

    Hangfire->>Hangfire: Trigger schedulato (cron)
    Hangfire->>Scheduler: ProductionJobRunner.RunAsync(idTask)

    activate Scheduler
    Scheduler->>DB: Query TaskDaEseguire

    alt Task.Enabled = false
        Scheduler-->>Hangfire: Skip (Guard Clause)
    else Task.Enabled = true
        Scheduler->>Scheduler: DetermineJobTypeAndCode(task)

        alt TipoFonte = SQL
            Scheduler->>Service: ExecuteSqlSourceAsync()
            Service->>DB: ExecuteQueryAsync(connectionString, query)
            Service->>DB: Salva in ProduzioneSistema
        else TipoFonte = HandlerIntegrato
            Scheduler->>Service: ExecuteHandlerSourceAsync()
            Service->>Service: ExecuteHandlerAsync(handlerCode)
            Service->>DB: Salva in ProduzioneSistema
        end

        Scheduler->>DB: UPDATE TaskDaEseguire (LastRunUtc, Stato)
    end
    deactivate Scheduler

    Note over Utente,Hangfire: FASE 4: GESTIONE TASK

    Utente->>UI: Click "Gestisci Task"
    UI->>Service: GetTasksByConfigurazioneAsync(idConfig)
    Service->>DB: Query TaskDaEseguire
    Service-->>UI: Lista task
    UI->>UI: DialogGestioneTask.ShowAsync()

    alt Disabilita Task
        Utente->>UI: Toggle Enabled OFF
        UI->>Service: UpdateTaskEnabledAsync(idTask, false)
        Service->>DB: UPDATE Enabled = false
    else Elimina Task
        Utente->>UI: Click "Elimina Task"
        UI->>Service: DeleteTaskAsync(idTask)
        Service->>Scheduler: RemoveByKeyAsync(hangfireKey)
        Scheduler->>Hangfire: RemoveIfExists(hangfireKey)
        Service->>DB: TaskDaEseguires.Remove(task)
    end

    Utente->>UI: Click "Salva e Aggiorna"
    UI->>UI: RefreshData()
    
