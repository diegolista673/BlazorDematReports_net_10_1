using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    public class ReportAnnualeDto
    {

        public int Anno { get; set; }

        [Required]
        public DateTime DataLavorazione { get; set; }

        public DateTime? StartDataLavorazione { get; set; }
        public DateTime? EndDataLavorazione { get; set; }
        public int? IdCentro { get; set; }
        public int? IdFaseLavorazione { get; set; }
        public int? IdProceduraLavorazione { get; set; }

        //[Required(ErrorMessage = "Fase is required")]
        public string? FaseLavorazione { get; set; }

        [Required(ErrorMessage = "Lavorazione is required")]
        public string? Lavorazione { get; set; }


        private DateTime? _year;

        [Required]
        public DateTime? Year
        {
            get { return _year; }
            set
            {

                _year = value;

                Anno = value!.Value.Year;
            }
        }

        public string? Image { get; set; }

        public string? Centro { get; set; }
    }
}
