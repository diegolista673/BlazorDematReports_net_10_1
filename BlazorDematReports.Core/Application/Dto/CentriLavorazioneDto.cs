using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta un centro di lavorazione.
    /// </summary>
    public partial class CentriLavorazioneDto
    {
        /// <summary>
        /// Identificativo del centro di lavorazione.
        /// </summary>
        public int Idcentro { get; set; }

        /// <summary>
        /// Nome del centro di lavorazione.
        /// </summary>
        [Required]
        public string? Centro { get; set; }

    }
}
