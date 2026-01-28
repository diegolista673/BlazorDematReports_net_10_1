using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO che rappresenta una fase di lavorazione.
    /// </summary>
    public partial class FasiLavorazioneDto
    {
        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        public int IdFaseLavorazione { get; set; }

        /// <summary>
        /// Nome o descrizione della fase di lavorazione.
        /// </summary>
        [Required]
        public string? FaseLavorazione { get; set; }
    }
}
