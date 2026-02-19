using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta la normalizzazione di un operatore.
    /// </summary>
    public partial class OperatoriNormalizzatiDto
    {
        /// <summary>
        /// Identificativo della normalizzazione.
        /// </summary>
        public int IdNorm { get; set; }

        /// <summary>
        /// Nome dell'operatore da normalizzare.
        /// </summary>
        [Required]
        public string? OperatoreDaNormalizzare { get; set; }

        /// <summary>
        /// Nome normalizzato dell'operatore.
        /// </summary>
        [Required]
        public string? OperatoreNormalizzato { get; set; }
    }
}
