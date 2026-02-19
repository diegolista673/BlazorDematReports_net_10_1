using System.ComponentModel.DataAnnotations;


namespace BlazorDematReports.Core.Application.Dto
{
    public partial class TurniDto
    {
        public int IdTurno { get; set; }

        [Required]
        public string? Turno { get; set; }


    }
}
