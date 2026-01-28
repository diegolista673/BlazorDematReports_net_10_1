using LibraryLavorazioni.LavorazioniViaMail.HERA16;
using LibraryLavorazioni.LavorazioniViaMail.Constants;
using LibraryLavorazioni.Utility.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryLavorazioni.Jobs
{
    /// <summary>
    /// Job Hangfire semplificato per l'elaborazione della produzione giornaliera HERA16.
    /// Versione ottimizzata con logging strutturato e validazione migliorata.
    /// 
    /// Esempio di utilizzo:
    /// RecurringJob.AddOrUpdate<Hera16ProduzioneJob>(
    ///     job => job.EseguiAsync(),
    ///     JobConstants.CronExpressions.Daily7AM
    /// );
    /// </summary>
    public class Hera16ProduzioneJob
    {
        #region Private Fields
        private readonly ILavorazioniConfigManager _configManager;
        private readonly ILogger<Hera16ProduzioneJob> _logger;
        #endregion

        #region Constructor
        /// <summary>
        /// Inizializza il job HERA16 con le dipendenze necessarie.
        /// </summary>
        /// <param name="configManager">Gestore configurazione lavorazioni</param>
        /// <param name="logger">Logger per il tracking delle operazioni</param>
        /// <exception cref="ArgumentNullException">Se uno dei parametri è null</exception>
        public Hera16ProduzioneJob(
            ILavorazioniConfigManager configManager,
            ILogger<Hera16ProduzioneJob> logger)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Esegue l'elaborazione della produzione giornaliera HERA16.
        /// Questo metodo viene chiamato da Hangfire secondo la pianificazione configurata.
        /// </summary>
        /// <returns>Task asincrono per l'esecuzione del job</returns>
        public async Task EseguiAsync()
        {
            const string jobName = JobConstants.JobNames.Hera16Production;
            
            _logger.LogInformation(JobConstants.LogMessages.JobStarting, jobName);
            
            try
            {
                // Valida la configurazione prima di procedere
                ValidateConfiguration();
                _logger.LogDebug(JobConstants.LogMessages.ConfigValidated);
                
                // Crea ed esegue il processore di produzione
                var processor = CreateProductionProcessor();
                await processor.CaricaAllegato();
                
                _logger.LogInformation(JobConstants.LogMessages.JobCompleted, jobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, JobConstants.LogMessages.JobFailed, jobName);
                
                // Re-throw per permettere a Hangfire di gestire il fallimento
                throw;
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Valida che la configurazione necessaria sia presente.
        /// </summary>
        /// <exception cref="InvalidOperationException">Se la configurazione non è valida</exception>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_configManager.CnxnHera))
            {
                throw new InvalidOperationException("Connessione HERA non configurata nel manager di configurazione");
            }
            
            // Validazioni aggiuntive per credenziali mail se necessarie
            if (string.IsNullOrWhiteSpace(_configManager.UserWebtopInps) || 
                string.IsNullOrWhiteSpace(_configManager.PasswordWebtopInps))
            {
                _logger.LogWarning("?? Credenziali Exchange non completamente configurate - alcune funzionalità potrebbero non essere disponibili");
            }
        }

        /// <summary>
        /// Crea una nuova istanza del processore di produzione HERA16.
        /// </summary>
        /// <returns>Istanza configurata di ProduzioneGiornaliera</returns>
        private ProduzioneGiornaliera CreateProductionProcessor()
        {
            _logger.LogDebug("?? Creazione processore produzione HERA16...");
            return new ProduzioneGiornaliera(_configManager);
        }
        #endregion
    }
}
