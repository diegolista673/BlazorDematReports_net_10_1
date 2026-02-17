namespace WorkerServiceAderEquitalia4
{
    /// <summary>
    /// Classe per rappresentare i totali calcolati da un file CSV di mail.
    /// Contiene proprietà per diversi tipi di scansioni e scarti.
    /// </summary>
    class TotaleMailCSV
    {
        /// <summary>
        /// Totale generico calcolato.
        /// </summary>
        public int TotaleGenerico { get; set; }

        /// <summary>
        /// Totale delle scansioni effettuate tramite Captiva.
        /// </summary>
        public int ScansioneCaptiva { get; set; }

        /// <summary>
        /// Totale delle scansioni effettuate tramite Sorter.
        /// </summary>
        public int ScansioneSorter { get; set; }

        /// <summary>
        /// Totale degli scarti delle scansioni effettuate tramite Sorter.
        /// </summary>
        public int ScartiScansioneSorter { get; set; }

        /// <summary>
        /// Totale delle scansioni di buste effettuate tramite Sorter.
        /// </summary>
        public int ScansioneSorterBuste { get; set; }

        /// <summary>
        /// Totale degli scarti delle scansioni di buste effettuate tramite Sorter.
        /// </summary>
        public int ScartiScansioneSorterBuste { get; set; }

        /// <summary>
        /// Costruttore predefinito della classe.
        /// </summary>
        public TotaleMailCSV()
        {
        }
    }
}
