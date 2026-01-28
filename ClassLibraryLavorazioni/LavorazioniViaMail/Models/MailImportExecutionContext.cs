namespace LibraryLavorazioni.LavorazioniViaMail.Models
{
    /// <summary>
    /// Contesto di esecuzione per l'importazione dati via mail.
    /// Contiene le informazioni necessarie per identificare la procedura e il servizio da utilizzare.
    /// Aggiornato per supportare il sistema unificato.
    /// </summary>
    public sealed class MailImportExecutionContext
    {
        /// <summary>
        /// Identificativo della procedura di lavorazione associata al task mail.
        /// </summary>
        public int IdProceduraLavorazione { get; init; }

        /// <summary>
        /// Codice identificativo del servizio mail specifico (es: "hera16.ews", "ader4.ews").
        /// </summary>
        public string ServiceCode { get; init; } = string.Empty;

        /// <summary>
        /// Identificativo del task specifico che ha scatenato l'esecuzione.
        /// Utilizzato per audit e logging dettagliato.
        /// </summary>
        public int TaskId { get; init; }

        /// <summary>
        /// Parametri aggiuntivi specifici del contesto di esecuzione.
        /// Possono includere configurazioni specifiche per l'handler.
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();

        /// <summary>
        /// Sede specifica per handler multi-sede (es: "Verona", "Genova" per ADER4).
        /// </summary>
        public string? Sede { get; init; }

        /// <summary>
        /// Modalità di esecuzione (es: "Full", "Incremental", "Test").
        /// </summary>
        public string ExecutionMode { get; init; } = "Full";

        /// <summary>
        /// Data di riferimento per l'elaborazione (utile per elaborazioni storiche).
        /// </summary>
        public DateTime? ReferenceDate { get; init; }
    }
}