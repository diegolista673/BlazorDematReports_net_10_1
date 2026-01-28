using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO per l'archivio di query SQL utilizzabili esternamente.
    /// Le query per i task automatici sono ora gestite in ConfigurazioneFontiDati.
    /// </summary>
    public partial class QueryProcedureLavorazioniDto
    {
        public int IdQuery { get; set; }

        public int IdproceduraLavorazione { get; set; }

        [Required]
        public string? Titolo { get; set; }

        [Required]
        public string? Descrizione { get; set; }

        [Required]
        public string? NomeProcedura { get; set; }

        public string? Centro { get; set; }

        public string? FaseLavorazione { get; set; }

        public string? Note { get; set; }

        public DateTime? DataCreazioneQuery { get; set; }
    }
}
