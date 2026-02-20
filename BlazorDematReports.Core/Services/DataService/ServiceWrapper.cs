using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.DataReading.Infrastructure;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio wrapper che implementa il pattern Facade, fornendo un punto di accesso centralizzato
    /// a tutti i servizi di gestione dati dell'applicazione. Questa classe nasconde la complessit� 
    /// delle operazioni sottostanti e riduce il numero di dipendenze nei componenti Blazor.
    /// Implementa anche il pattern Lazy Loading per inizializzare i servizi solo quando necessario.
    /// </summary>
    public class ServiceWrapper : IServiceWrapper
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILoggerFactory loggerFactory;
        private readonly IProductionJobScheduler productionScheduler;

        // Campi privati per implementazione del Lazy Loading
        private readonly Lazy<IServiceCentri> _serviceCentri;
        private readonly Lazy<IServiceOperatori> _serviceOperatori;
        private readonly Lazy<IServiceClienti> _serviceClienti;
        private readonly Lazy<IServiceProcedureClienti> _serviceProcedureClienti;
        private readonly Lazy<IServiceProcedureLavorazioni> _serviceProcedureLavorazioni;
        private readonly Lazy<IServiceFormatoDati> _serviceFormatoDati;
        private readonly Lazy<IServiceRepartiProduzione> _serviceRepartiProduzione;
        private readonly Lazy<IServiceFasiLavorazioni> _serviceFasiLavorazioni;
        private readonly Lazy<IServiceProduzioneOperatori> _serviceProduzioneOperatori;
        private readonly Lazy<IServiceTipologieTotali> _serviceTipologieTotali;
        private readonly Lazy<IServiceLavorazioniFasiTipoTotale> _serviceLavorazioniFasiTipoTotale;
        private readonly Lazy<IServiceTurni> _serviceTurni;
        private readonly Lazy<IServiceProduzioneSistema> _serviceProduzioneSistema;
        private readonly Lazy<IServiceConfigReportDocumenti> _serviceConfigReportDocumenti;
        private readonly Lazy<IServiceOperatoriNormalizzati> _serviceOperatoriNormalizzati;
        private readonly Lazy<IServiceTaskDataReadingAggiornamento> _serviceTaskDataReadingAggiornamento;
        private readonly Lazy<IServiceQueryProcedureLavorazioni> _serviceQueryProcedureLavorazioni;
        private readonly Lazy<IServiceCentriVisibili> _serviceCentriVisibili;
        private readonly Lazy<IServiceTipoTurni> _serviceTipoTurni;
        private readonly Lazy<IServiceTaskDaEseguire> _serviceTaskDaEseguire;
        private readonly Lazy<IServiceRuoli> _serviceRuoli;
        private readonly Lazy<IServiceConfigurazioneFontiDati> _serviceConfigurazioneFontiDati;
        private readonly Lazy<IServiceMail> _serviceMail;
        private readonly Lazy<IServiceTaskManagement> _serviceTaskManagement;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per tutti i servizi di gestione dati.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="lettoreDati">Servizio per la lettura dei dati.</param>
        /// <param name="loggerFactory">Factory per la creazione dei logger specifici.</param>
        /// <param name="fluentEmail">Servizio FluentEmail per invio email.</param>
        /// <param name="productionScheduler">Scheduler per la gestione dei job Hangfire di produzione.</param>
        public ServiceWrapper(
            IMapper mapper,
            ConfigUser configUser,
            IDbContextFactory<DematReportsContext> contextFactory,
            ILoggerFactory loggerFactory,
            FluentEmail.Core.IFluentEmail fluentEmail,
            IProductionJobScheduler productionScheduler)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.loggerFactory = loggerFactory;
            this.productionScheduler = productionScheduler;

            // Servizi con logging implementato (QueryLoggingHelper gi� configurato)
            _serviceOperatori = new Lazy<IServiceOperatori>(() => new ServiceOperatori(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceOperatori>()));
            _serviceProcedureClienti = new Lazy<IServiceProcedureClienti>(() => new ServiceProcedureClienti(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceProcedureClienti>()));
            _serviceProcedureLavorazioni = new Lazy<IServiceProcedureLavorazioni>(() => new ServiceProcedureLavorazioni(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceProcedureLavorazioni>()));
            _serviceQueryProcedureLavorazioni = new Lazy<IServiceQueryProcedureLavorazioni>(() => new ServiceQueryProcedureLavorazioni(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceQueryProcedureLavorazioni>()));
            _serviceCentri = new Lazy<IServiceCentri>(() => new ServiceCentri(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceCentri>()));
            _serviceFasiLavorazioni = new Lazy<IServiceFasiLavorazioni>(() => new ServiceFasiLavorazioni(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceFasiLavorazioni>()));
            _serviceFormatoDati = new Lazy<IServiceFormatoDati>(() => new ServiceFormatoDati(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceFormatoDati>()));
            _serviceProduzioneOperatori = new Lazy<IServiceProduzioneOperatori>(() => new ServiceProduzioneOperatori(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceProduzioneOperatori>()));
            _serviceProduzioneSistema = new Lazy<IServiceProduzioneSistema>(() => new ServiceProduzioneSistema(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceProduzioneSistema>()));
            _serviceClienti = new Lazy<IServiceClienti>(() => new ServiceClienti(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceClienti>()));
            _serviceRepartiProduzione = new Lazy<IServiceRepartiProduzione>(() => new ServiceRepartiProduzione(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceRepartiProduzione>()));
            _serviceTipologieTotali = new Lazy<IServiceTipologieTotali>(() => new ServiceTipologieTotali(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceTipologieTotali>()));
            _serviceLavorazioniFasiTipoTotale = new Lazy<IServiceLavorazioniFasiTipoTotale>(() => new ServiceLavorazioniFasiTipoTotale(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceLavorazioniFasiTipoTotale>()));
            _serviceTurni = new Lazy<IServiceTurni>(() => new ServiceTurni(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceTurni>()));
            _serviceTipoTurni = new Lazy<IServiceTipoTurni>(() => new ServiceTipoTurni(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceTipoTurni>()));
            _serviceRuoli = new Lazy<IServiceRuoli>(() => new ServiceRuoli(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceRuoli>()));
            _serviceConfigReportDocumenti = new Lazy<IServiceConfigReportDocumenti>(() => new ServiceConfigReportDocumenti(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceConfigReportDocumenti>()));
            _serviceOperatoriNormalizzati = new Lazy<IServiceOperatoriNormalizzati>(() => new ServiceOperatoriNormalizzati(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceOperatoriNormalizzati>()));
            _serviceTaskDataReadingAggiornamento = new Lazy<IServiceTaskDataReadingAggiornamento>(() => new ServiceTaskDataReadingAggiornamento(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceTaskDataReadingAggiornamento>()));
            _serviceCentriVisibili = new Lazy<IServiceCentriVisibili>(() => new ServiceCentriVisibili(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceCentriVisibili>()));
            _serviceTaskDaEseguire = new Lazy<IServiceTaskDaEseguire>(() => new ServiceTaskDaEseguire(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceTaskDaEseguire>()));
            _serviceConfigurazioneFontiDati = new Lazy<IServiceConfigurazioneFontiDati>(() => new ServiceConfigurazioneFontiDati(mapper, configUser, contextFactory, loggerFactory.CreateLogger<ServiceConfigurazioneFontiDati>(), productionScheduler));
            _serviceMail = new Lazy<IServiceMail>(() => new ServiceMail(mapper, configUser, contextFactory, fluentEmail, loggerFactory.CreateLogger<ServiceMail>()));
            _serviceTaskManagement = new Lazy<IServiceTaskManagement>(() => new ServiceTaskManagement(contextFactory, loggerFactory.CreateLogger<ServiceTaskManagement>()));
        }

        /// <inheritdoc/>
        public IServiceCentri ServiceCentri => _serviceCentri.Value;

        /// <inheritdoc/>
        public IServiceOperatori ServiceOperatori => _serviceOperatori.Value;

        /// <inheritdoc/>
        public IServiceClienti ServiceClienti => _serviceClienti.Value;

        /// <inheritdoc/>
        public IServiceProcedureClienti ServiceProcedureClienti => _serviceProcedureClienti.Value;

        /// <inheritdoc/>
        public IServiceProcedureLavorazioni ServiceProcedureLavorazioni => _serviceProcedureLavorazioni.Value;

        /// <inheritdoc/>
        public IServiceFormatoDati ServiceFormatoDati => _serviceFormatoDati.Value;

        /// <inheritdoc/>
        public IServiceRepartiProduzione ServiceRepartiProduzione => _serviceRepartiProduzione.Value;

        /// <inheritdoc/>
        public IServiceFasiLavorazioni ServiceFasiLavorazioni => _serviceFasiLavorazioni.Value;

        /// <inheritdoc/>
        public IServiceProduzioneOperatori ServiceProduzioneOperatori => _serviceProduzioneOperatori.Value;

        /// <inheritdoc/>
        public IServiceTipologieTotali ServiceTipologieTotali => _serviceTipologieTotali.Value;

        /// <inheritdoc/>
        public IServiceLavorazioniFasiTipoTotale ServiceLavorazioniFasiTipoTotale => _serviceLavorazioniFasiTipoTotale.Value;

        /// <inheritdoc/>
        public IServiceTurni ServiceTurni => _serviceTurni.Value;

        /// <inheritdoc/>
        public IServiceProduzioneSistema ServiceProduzioneSistema => _serviceProduzioneSistema.Value;

        /// <inheritdoc/>
        public IServiceConfigReportDocumenti ServiceConfigReportDocumenti => _serviceConfigReportDocumenti.Value;

        /// <inheritdoc/>
        public IServiceOperatoriNormalizzati ServiceOperatoriNormalizzati => _serviceOperatoriNormalizzati.Value;

        /// <inheritdoc/>
        public IServiceTaskDataReadingAggiornamento ServiceTaskDataReadingAggiornamento => _serviceTaskDataReadingAggiornamento.Value;

        /// <inheritdoc/>
        public IServiceQueryProcedureLavorazioni ServiceQueryProcedureLavorazioni => _serviceQueryProcedureLavorazioni.Value;

        /// <inheritdoc/>
        public IServiceCentriVisibili ServiceCentriVisibili => _serviceCentriVisibili.Value;

        /// <inheritdoc/>
        public IServiceTipoTurni ServiceTipoTurni => _serviceTipoTurni.Value;


        /// <inheritdoc/>
        public IServiceTaskDaEseguire ServiceTaskDaEseguire => _serviceTaskDaEseguire.Value;

        /// <inheritdoc/>
        public IServiceRuoli ServiceRuoli => _serviceRuoli.Value;
        /// <inheritdoc/>
        public IServiceConfigurazioneFontiDati ServiceConfigurazioneFontiDati => _serviceConfigurazioneFontiDati.Value;
        /// <inheritdoc/>
        public IServiceMail ServiceMail => _serviceMail?.Value ?? throw new InvalidOperationException("ServiceMail non inizializzato. Verificare la registrazione di IFluentEmail nel DI container.");

        /// <inheritdoc/>
        public IServiceTaskManagement ServiceTaskManagement => _serviceTaskManagement.Value;
    }
}