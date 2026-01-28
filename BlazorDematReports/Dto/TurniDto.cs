using System.ComponentModel.DataAnnotations;


namespace BlazorDematReports.Dto
{
    public partial class TurniDto
    {
        public int IdTurno { get; set; }

        [Required]
        public string? Turno { get; set; }


    }
}
