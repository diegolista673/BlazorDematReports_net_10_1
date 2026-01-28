using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    public partial class RuoliDto
    {

        public int IdRuolo { get; set; }

        [Required]
        public string? Ruolo { get; set; }
    }
}
