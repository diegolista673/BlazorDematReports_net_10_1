namespace BlazorDematReports.Core.Constants
{
    /// <summary>
    /// Costanti di configurazione centralizzate per il sistema di task scheduling.
    /// SINGLE SOURCE OF TRUTH per tutti i progetti.
    /// Modifica questi valori per cambiare i default globali dell'applicazione.
    /// </summary>
    public static class TaskConfigurationDefaults
    {
        /// <summary>
        /// Numero di giorni precedenti di default per l'estrazione dati.
        /// Usato quando GiorniPrecedenti non è specificato o è minore/uguale a 0.
        /// </summary>
        public const int DefaultGiorniPrecedenti = 10;

        /// <summary>
        /// Espressione cron di default per task schedulati.
        /// Formato: "minuto ora giorno mese giornoSettimana"
        /// Default: "0 5 * * *" = Esegue alle 05:00 ogni giorno.
        /// </summary>
        public const string DefaultCronExpression = "0 5 * * *";

        /// <summary>
        /// Timeout in secondi per l'esecuzione di query SQL.
        /// </summary>
        public const int DefaultQueryTimeoutSeconds = 60;

        /// <summary>
        /// Numero massimo di tentativi consecutivi falliti prima di disabilitare un task.
        /// </summary>
        public const int MaxConsecutiveFailures = 3;

        /// <summary>
        /// Delay in millisecondi tra tentativi di retry per race condition.
        /// Usato in EmailDailyFlagService per gestire lock concorrenti.
        /// </summary>
        public const int RetryDelayMilliseconds = 500;

        /// <summary>
        /// Numero massimo di tentativi di retry per acquisizione lock.
        /// Usato in EmailDailyFlagService per prevenire loop infiniti.
        /// </summary>
        public const int MaxRetryAttempts = 3;

        /// <summary>
        /// Numero massimo di righe da caricare in una singola query.
        /// Protezione contro query che restituiscono troppi dati.
        /// </summary>
        public const int MaxQueryResultRows = 10000;

        /// <summary>
        /// Valore minimo valido per GiorniPrecedenti.
        /// </summary>
        public const int MinGiorniPrecedenti = 1;

        /// <summary>
        /// Valore massimo valido per GiorniPrecedenti.
        /// </summary>
        public const int MaxGiorniPrecedenti = 365;
    }
}
