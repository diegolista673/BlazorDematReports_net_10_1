using System.ComponentModel.DataAnnotations;


namespace BlazorDematReports.Dto
{
    public partial class TipoTurniDto
    {
        public int IdTipoTurno { get; set; }

        [Required]
        public string? TipoTurno { get; set; }


    }
}
