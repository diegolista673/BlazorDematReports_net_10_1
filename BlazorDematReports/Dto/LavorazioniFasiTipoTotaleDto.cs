using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO che rappresenta una lavorazione fase tipo totale, con riferimenti a procedura, fase e tipologia.
    /// </summary>
    public partial class LavorazioniFasiTipoTotaleDto
    {
        /// <summary>
        /// Identificativo della lavorazione fase tipo totale.
        /// </summary>
        public int? IdLavorazioneFaseTipoTotale { get; set; }

        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int? IdProceduraLavorazione { get; set; }

        /// <summary>
        /// Identificativo della fase.
        /// </summary>
        public int? IdFase { get; set; }

        /// <summary>
        /// Identificativo della tipologia totale.
        /// </summary>
        public int? IdTipologiaTotale { get; set; }

        /// <summary>
        /// Nome della procedura di lavorazione.
        /// </summary>
        [Required]
        public string? NomeProcedura { get; set; }

        /// <summary>
        /// Nome della fase.
        /// </summary>
        [Required]
        public string? Fase { get; set; }

        /// <summary>
        /// Nome della tipologia totale.
        /// </summary>
        [Required]
        public string? TipologiaTotale { get; set; }
    }
}
