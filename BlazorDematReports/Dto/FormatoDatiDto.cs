#nullable disable

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO che rappresenta il formato dei dati di produzione.
    /// </summary>
    public partial class FormatoDatiDto
    {
        /// <summary>
        /// Identificativo del formato dati.
        /// </summary>
        public int IdformatoDati { get; set; }

        /// <summary>
        /// Descrizione del formato dati di produzione.
        /// </summary>
        public string FormatoDatiProduzione { get; set; }
    }
}
