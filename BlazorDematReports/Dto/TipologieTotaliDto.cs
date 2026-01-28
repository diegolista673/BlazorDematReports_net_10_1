using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO per rappresentare le tipologie di totali.
    /// Contiene informazioni sull'identificativo, il tipo e il valore totale.
    /// </summary>
    public partial class TipologieTotaliDto
    {
        /// <summary>
        /// Identificativo univoco del tipo di totale.
        /// </summary>
        public int IdTipoTotale { get; set; }

        /// <summary>
        /// Nome o descrizione del tipo di totale.
        /// </summary>
        [Required]
        public string? TipoTotale { get; set; }

        /// <summary>
        /// Valore totale associato al tipo.
        /// </summary>
        public int Totale { get; set; }
    }
}
