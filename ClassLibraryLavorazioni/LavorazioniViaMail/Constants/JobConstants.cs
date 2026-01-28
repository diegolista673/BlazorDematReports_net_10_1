namespace LibraryLavorazioni.LavorazioniViaMail.Constants
{
    /// <summary>
    /// Costanti centralizzate per la gestione dei job di lavorazione.
    /// Utilizzate per standardizzare nomi, espressioni cron e messaggi di log.
    /// Aggiornamento: include supporto per ADER4 e sistema unificato.
    /// </summary>
    public static class JobConstants
    {
        /// <summary>
        /// Nomi standardizzati dei job per logging e identificazione.
        /// </summary>
        public static class JobNames
        {
            public const string Hera16Production = "HERA16 Produzione Giornaliera";
        }



        /// <summary>
        /// Messaggi di log standardizzati per identificazione rapida.
        /// </summary>
        public static class LogMessages
        {
            public const string JobStarting = "Avvio job {JobName}...";
            public const string JobCompleted = "Job {JobName} completato con successo";
            public const string JobFailed = "Errore durante l'esecuzione del job {JobName}";
            public const string ConfigValidated = "Configurazione validata correttamente";
        }


        /// <summary>
        /// Service codes per i vari handler mail.
        /// </summary>
        public static class MailServiceCodes
        {
            /// <summary>
            /// Servizio HERA16 via Exchange Web Services.
            /// </summary>
            public const string Hera16 = "HERA16";
            
            /// <summary>
            /// Servizio ADER4/Equitalia via Exchange Web Services (Verona + Genova).
            /// </summary>
            public const string Ader4 = "ADER4";
        }


    }
}
