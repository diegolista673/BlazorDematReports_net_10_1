namespace LibraryLavorazioni.Shared.Constants
{
    /// <summary>
    /// Costanti di configurazione condivise per il progetto ClassLibraryLavorazioni.
    /// Sincronizzate con BlazorDematReports.Constants.TaskConfigurationDefaults.
    /// </summary>
    public static class TaskConfigurationDefaults
    {
        /// <summary>
        /// Numero di giorni precedenti di default per l'estrazione dati.
        /// </summary>
        public const int DefaultGiorniPrecedenti = 10;

        /// <summary>
        /// Espressione cron di default per task schedulati.
        /// Formato: "0 5 * * *" = Esegue alle 05:00 ogni giorno.
        /// </summary>
        public const string DefaultCronExpression = "0 5 * * *";

        /// <summary>
        /// Timeout in secondi per l'esecuzione di query SQL.
        /// </summary>
        public const int DefaultQueryTimeoutSeconds = 60;

        /// <summary>
        /// Numero massimo di tentativi consecutivi falliti.
        /// </summary>
        public const int MaxConsecutiveFailures = 3;

        /// <summary>
        /// Delay in millisecondi tra tentativi di retry.
        /// </summary>
        public const int RetryDelayMilliseconds = 500;

        /// <summary>
        /// Numero massimo di tentativi di retry.
        /// </summary>
        public const int MaxRetryAttempts = 3;
    }
}
