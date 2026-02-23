namespace BlazorDematReports.Core.Constants
{
    /// <summary>
    /// Costanti per i codici delle lavorazioni supportate.
    /// Utilizzate per identificare univocamente ogni tipo di lavorazione disponibile nel sistema.
    /// </summary>
    public static class LavorazioniCodes
    {
        /// <summary>
        /// Lavorazione Z0072370 per 28 AUT.
        /// </summary>
        public const string Z0072370_28AUT = "Z0072370_28AUT";

        /// <summary>
        /// Lavorazione Z0082041 per Softline.
        /// </summary>
        public const string Z0082041_SOFTLINE = "Z0082041_SOFTLINE";

        /// <summary>
        /// Lavorazione ANT ADER4 Sorter 1 e 2.
        /// </summary>
        public const string ANT_ADER4_SORTER_1_2 = "ANT_ADER4_SORTER_1_2";

        /// <summary>
        /// Lavorazione Pratiche Successione.
        /// </summary>
        public const string PRATICHE_SUCCESSIONE = "PRATICHE_SUCCESSIONE";

        /// <summary>
        /// Lavorazione RDMKT RSP per tabelle dinamiche.
        /// </summary>
        public const string RDMKT_RSP = "RDMKT_RSP";

        /// <summary>
        /// Servizio HERA16 via Exchange Web Services per importazione dati da email.
        /// </summary>
        public const string HERA16 = "HERA16";

        /// <summary>
        /// Servizio ADER4/Equitalia via Exchange Web Services per importazione dati da email (Verona + Genova).
        /// </summary>
        public const string ADER4 = "ADER4";
    }
}
