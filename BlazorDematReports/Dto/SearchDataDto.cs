using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO per i criteri di ricerca dei dati di produzione (date range, procedura, centro, fase).
    /// </summary>
    public partial class SearchDataDto
    {
        [Required]
        public DateTime? StartDate { get; set; }

        [Required]
        public DateTime? EndDate { get; set; }

        [Required]
        public string? NomeProcedura { get; set; }

        [Required]
        public int? Idcentro { get; set; }

        public int? IdProceduraLavorazione { get; set; }

        public string? Centro { get; set; }


        [Required]
        public string? Fase { get; set; }



    }
}
