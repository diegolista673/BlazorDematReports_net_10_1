namespace LibraryLavorazioni.Utility.Models
{
    /// <summary>
    /// Record che rappresenta i dati di lavorazione elaborati e pronti per la persistenza o l'analisi.
    /// <para>
    /// Contiene informazioni dettagliate sull'operatore, la lavorazione, i conteggi e lo stato di inserimento.
    /// </para>
    /// </summary>
    public record DatiElaborati
    {
        public string? Operatore { get; set; }
        public int IdOperatore { get; set; }
        public DateTime DataLavorazione { get; set; }
        public DateTime DataAggiornamento { get; set; }
        public bool FlagInserimentoAuto { get; set; }
        public bool FlagInserimentoManuale { get; set; }
        public int IdProceduraLavorazione { get; set; }
        public int IdFaseLavorazione { get; set; }
        public int IdCentro { get; set; }
        public string? OperatoreNonRiconosciuto { get; set; }
        public int? Documenti { get; set; }
        public int? Fogli { get; set; }
        public int? Pagine { get; set; }
    }
}
