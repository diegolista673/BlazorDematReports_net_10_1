namespace DataReading.Dto
{
    /// <summary>
    /// DTO che rappresenta i dati di produzione di un sistema.
    /// </summary>
    public class ProduzioneSistemaDTO
    {
        /// <summary>
        /// Identificativo della produzione sistema.
        /// </summary>
        public int IdProduzioneSistema { get; set; }

        /// <summary>
        /// Identificativo dell'operatore.
        /// </summary>
        public int? IdOperatore { get; set; }

        /// <summary>
        /// Nome dell'operatore.
        /// </summary>
        public string? Operatore { get; set; }

        /// <summary>
        /// Nome dell'operatore non riconosciuto.
        /// </summary>
        public string? OperatoreNonRiconosciuto { get; set; }

        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int? IdProceduraLavorazione { get; set; }

        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        public int? IdFaseLavorazione { get; set; }

        /// <summary>
        /// Data della lavorazione.
        /// </summary>
        public DateTime? DataLavorazione { get; set; }

        /// <summary>
        /// Data di aggiornamento del record.
        /// </summary>
        public DateTime DataAggiornamento { get; set; }

        /// <summary>
        /// Numero di documenti prodotti.
        /// </summary>
        public int Documenti { get; set; }

        /// <summary>
        /// Numero di fogli prodotti.
        /// </summary>
        public int Fogli { get; set; }

        /// <summary>
        /// Numero di pagine prodotte.
        /// </summary>
        public int Pagine { get; set; }

        /// <summary>
        /// Numero di scarti prodotti.
        /// </summary>
        public int Scarti { get; set; }

        /// <summary>
        /// Numero di pagine senza bianco.
        /// </summary>
        public int PagineSenzaBianco { get; set; }

        /// <summary>
        /// Indica se l'inserimento è stato effettuato automaticamente.
        /// </summary>
        public bool? FlagInserimentoAuto { get; set; }

        /// <summary>
        /// Indica se l'inserimento è stato effettuato manualmente.
        /// </summary>
        public bool? FlagInserimentoManuale { get; set; }

        /// <summary>
        /// Identificativo del centro di lavorazione.
        /// </summary>
        public int? IdCentro { get; set; }

        /// <summary>
        /// Nome della lavorazione.
        /// </summary>
        public string? Lavorazione { get; set; }

        /// <summary>
        /// Nome della fase di lavorazione.
        /// </summary>
        public string? Fase { get; set; }
    }
}
