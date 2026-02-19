namespace BlazorDematReports.Core.DataReading.Dto
{
    /// <summary>
    /// DTO che rappresenta un aggiornamento di lavorazione e i relativi dettagli.
    /// </summary>
    public class AggiornamentoDto
    {
        /// <summary>
        /// Identificativo dell'aggiornamento.
        /// </summary>
        public int? IdAggiornamento { get; set; }

        /// <summary>
        /// Nome della lavorazione.
        /// </summary>
        public string? Lavorazione { get; set; }

        /// <summary>
        /// Identificativo della lavorazione.
        /// </summary>
        public int? IdLavorazione { get; set; }

        /// <summary>
        /// Descrizione della fase della lavorazione.
        /// </summary>
        public string? FaseLavorazione { get; set; }

        /// <summary>
        /// Identificativo della fase.
        /// </summary>
        public int? IdFase { get; set; }

        /// <summary>
        /// Data della lavorazione.
        /// </summary>
        public DateTime? DataLavorazione { get; set; }

        /// <summary>
        /// Data dell'aggiornamento.
        /// </summary>
        public DateTime? DataAggiornamento { get; set; }

        /// <summary>
        /// Numero di risultati ottenuti.
        /// </summary>
        public int? Risultati { get; set; } = 0;

        /// <summary>
        /// Esito della lettura del dato.
        /// </summary>
        public bool? EsitoLetturaDato { get; set; } = false;

        /// <summary>
        /// Descrizione dell'esito della lavorazione.
        /// </summary>
        public string? DescrizioneEsito { get; set; }

    }
}
