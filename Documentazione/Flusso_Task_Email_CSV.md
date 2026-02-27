Hangfire (cron scheduler)
    │
    ▼
ProductionJobScheduler.AddOrUpdateAsync()
    │  registra recurring job con chiave "hdl:{id}-{proc}:{handlercode}"
    │
    ▼
ProductionJobRunner.RunAsync(idTaskDaEseguire)
    │  1. carica TaskDaEseguire dal DB con tutte le navigation
    │  2. controlla Enabled
    │
    ▼
ExecuteProductionTaskAsync()
    │  3. carica ConfigurazioneFontiDati (TipoFonte = HandlerIntegrato)
    │  4. trova mapping Fase/Centro attivo
    │
    ▼
AcquireFromHandlerAsync()
    │  5. risolve IUnifiedHandlerService → Ader4Handler
    │
    ▼
Ader4Handler.ExecuteAsync()
    │  6. chiede a EmailDailyFlagService.TryMarkAsProcessingAsync()
    │     ├─ TRUE  → è il primo task oggi → processa
    │     └─ FALSE → già elaborato oggi   → ritorna lista vuota (skip)
    │
    ▼ (solo se primo oggi)
Ader4EmailService.ProcessEmailsAsync()  [eredita da BaseEwsEmailService]
    │
    │  7. connessione Exchange via EWS (WebCredentials da appsettings "MailServices:ADER4")
    │  8. ricerca email in Inbox con filtro subject (OR su SubjectVerona / SubjectGenova)
    │
    │  per ogni email trovata:
    │    ├─ carica proprietà complete (Attachments, Subject, Body)
    │    ├─ ExtractMetadataFromBody() → regex su "Periodo di riferimento:" → DataRiferimento
    │    ├─ per ogni FileAttachment che matcha pattern (EQTMN4_*):
    │    │     DownloadAttachmentAsync()  → salva in /report/ADER4/
    │    │     ProcessAttachmentAsync()   → [override in Ader4EmailService]
    │    │       ├─ EQTMN4_Scatole_Scansionate* → ProcessScatoleScansionateAsync()
    │    │       │     legge CSV (LumenWorks CsvReader, delimiter ';')
    │    │       │     calcola totali Captiva / Sorter / SorterBuste
    │    │       │     scrive in metadata dict
    │    │       └─ EQTMN4_Dispacci_* → ProcessDispacciAsync()
    │    │             conta totale documenti → scrive in metadata
    │    └─ sposta email in cartella archivio Exchange ("EQUITALIA_4")
    │
    │  se CreateZipArchive → crea zip giornaliero in /archive/ADER4/
    │  se CleanupAfterProcessing → elimina allegati temporanei
    │
    ▼
Ader4Handler.ConvertEmailResultsToDatiLavorazione()
    │  9. per ogni email di successo:
    │     legge metadata (DataRiferimento, ScansioneCaptiva, ScansioneSorter, ScansioneSorterBuste)
    │     crea record DatiLavorazione con Operatore="SISTEMA" / "SISTEMA_SORTER" / "SISTEMA_SORTER_BUSTE"
    │
    ▼
ProductionJobRunner: ElaboraDatiLavorazioneAsync() → PersistProduzioneSistemaAsync()
    │  10. elabora + salva in ProduzioneSistema
    │  11. aggiorna stato task + audit log in DB