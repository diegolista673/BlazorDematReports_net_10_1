namespace BlazorDematReports.Core.Utility.Models
{
    /// <summary>
    /// Record che rappresenta i dati originali acquisiti da una fonte di lavorazione.
    /// <para>
    /// Contiene le informazioni di base relative all'operatore, alla data e ai conteggi dei documenti elaborati.
    /// </para>
    /// </summary>
    public record DatiLavorazione
    {
        /// <summary>
        /// Nome dell'operatore che ha effettuato la lavorazione.
        /// </summary>
        public string? Operatore { get; set; }

        /// <summary>
        /// Data in cui è stata effettuata la lavorazione.
        /// </summary>
        public DateTime DataLavorazione { get; set; }

        /// <summary>
        /// Nome dell'operatore di scansione, se disponibile.
        /// </summary>
        public string? OperatoreScan { get; set; }

        /// <summary>
        /// Numero di documenti elaborati.
        /// </summary>
        public int? Documenti { get; set; }

        /// <summary>
        /// Numero di fogli elaborati.
        /// </summary>
        public int? Fogli { get; set; }

        /// <summary>
        /// Numero di pagine elaborate.
        /// </summary>
        public int? Pagine { get; set; }

        /// <summary>
        /// Indica se il record proviene da una query che garantisce l'appartenenza al centro selezionato
        /// (es. presenza di un filtro esplicito per il centro).
        /// </summary>
        public bool AppartieneAlCentroSelezionato { get; set; }
    }
}
