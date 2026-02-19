using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta la visibilità di un centro per un operatore.
    /// </summary>
    public partial class CentriVisibiliDto
    {
        /// <summary>
        /// Identificativo dell'operatore.
        /// </summary>
        public int? IdOperatore { get; set; }

        /// <summary>
        /// Identificativo del centro.
        /// </summary>
        public int? IdCentro { get; set; }

        /// <summary>
        /// Nome del centro.
        /// </summary>
        public string? Centro { get; set; }

        /// <summary>
        /// Indica se il centro è visibile per l'operatore.
        /// </summary>
        [Required]
        public bool FlagVisibile { get; set; } = false;
    }
}
