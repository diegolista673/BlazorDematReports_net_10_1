using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    public partial class RuoliDto
    {

        public int IdRuolo { get; set; }

        [Required]
        public string? Ruolo { get; set; }
    }
}
