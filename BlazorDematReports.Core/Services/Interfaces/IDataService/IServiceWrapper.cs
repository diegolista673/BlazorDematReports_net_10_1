namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Wrapper per l'accesso centralizzato a tutti i servizi dati dell'applicazione.
    /// </summary>
    public interface IServiceWrapper
    {
        /// <summary>Servizio per la gestione degli operatori.</summary>
        IServiceOperatori ServiceOperatori { get; }
        /// <summary>Servizio per la gestione dei centri.</summary>
        IServiceCentri ServiceCentri { get; }
        /// <summary>Servizio per la gestione dei clienti.</summary>
        IServiceClienti ServiceClienti { get; }
        /// <summary>Servizio per la gestione delle procedure clienti.</summary>
        IServiceProcedureClienti ServiceProcedureClienti { get; }
        /// <summary>Servizio per la gestione delle procedure lavorazioni.</summary>
        IServiceProcedureLavorazioni ServiceProcedureLavorazioni { get; }
        /// <summary>Servizio per la gestione dei formati dati.</summary>
        IServiceFormatoDati ServiceFormatoDati { get; }
        /// <summary>Servizio per la gestione dei reparti di produzione.</summary>
        IServiceRepartiProduzione ServiceRepartiProduzione { get; }
        /// <summary>Servizio per la gestione delle fasi di lavorazione.</summary>
        IServiceFasiLavorazioni ServiceFasiLavorazioni { get; }
        /// <summary>Servizio per la gestione della produzione operatori.</summary>
        IServiceProduzioneOperatori ServiceProduzioneOperatori { get; }
        /// <summary>Servizio per la gestione delle tipologie totali.</summary>
        IServiceTipologieTotali ServiceTipologieTotali { get; }
        /// <summary>Servizio per la gestione delle lavorazioni fasi tipo totale.</summary>
        IServiceLavorazioniFasiTipoTotale ServiceLavorazioniFasiTipoTotale { get; }
        /// <summary>Servizio per la gestione dei turni.</summary>
        IServiceTurni ServiceTurni { get; }
        /// <summary>Servizio per la gestione della produzione sistema.</summary>
        IServiceProduzioneSistema ServiceProduzioneSistema { get; }
        /// <summary>Servizio per la gestione della configurazione report documenti.</summary>
        IServiceConfigReportDocumenti ServiceConfigReportDocumenti { get; }
        /// <summary>Servizio per la gestione degli operatori normalizzati.</summary>
        IServiceOperatoriNormalizzati ServiceOperatoriNormalizzati { get; }
        /// <summary>Servizio per la gestione dei task di aggiornamento lettura dati.</summary>
        IServiceTaskDataReadingAggiornamento ServiceTaskDataReadingAggiornamento { get; }
        /// <summary>Servizio per la gestione delle query procedure lavorazioni.</summary>
        IServiceQueryProcedureLavorazioni ServiceQueryProcedureLavorazioni { get; }
        /// <summary>Servizio per la gestione dei centri visibili.</summary>
        IServiceCentriVisibili ServiceCentriVisibili { get; }
        /// <summary>Servizio per la gestione dei tipi turno.</summary>
        IServiceTipoTurni ServiceTipoTurni { get; }
        /// <summary>Servizio per la gestione dei task da eseguire.</summary>
        IServiceTaskDaEseguire ServiceTaskDaEseguire { get; }
        /// <summary>Servizio per la gestione dei ruoli.</summary>
        IServiceRuoli ServiceRuoli { get; }
        /// <summary>Servizio per la gestione della configurazione fonti dati.</summary>
        IServiceConfigurazioneFontiDati ServiceConfigurazioneFontiDati { get; }
        /// <summary>Servizio per la gestione dell'invio email e configurazioni servizi mail.</summary>
        IServiceMail ServiceMail { get; }
        /// <summary>
        /// Gets the service that provides task management operations for the current context.
        /// </summary>
        /// <remarks>Use this property to access functionality related to creating, updating, or querying
        /// service tasks. The returned service is typically used to manage background or scheduled operations within
        /// the application.</remarks>
        IServiceTaskManagement ServiceTaskManagement { get; }
    }
}
