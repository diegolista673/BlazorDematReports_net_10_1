using System.ComponentModel.DataAnnotations;

#nullable disable

namespace BlazorDematReports.Core.Application.Dto
{
    public partial class ProduzioneSistemaDto
    {

        public int IdProduzioneSistema { get; set; }
        public int? IdOperatore { get; set; }
        public string Operatore { get; set; }
        public string OperatoreNonRiconosciuto { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdProceduraLavorazione { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdFaseLavorazione { get; set; }
        public DateTime? DataLavorazione { get; set; }
        public DateTime DataAggiornamento { get; set; }

        [Required(ErrorMessage = "Documenti is required")]
        public int Documenti { get; set; }

        [Required(ErrorMessage = "Fogli is required")]
        public int Fogli { get; set; }

        [Required(ErrorMessage = "Pagine is required")]
        public int Pagine { get; set; }

        [Required(ErrorMessage = "Scarti is required")]
        public int Scarti { get; set; }

        [Required(ErrorMessage = "PagineSenzaBianco is required")]
        public int PagineSenzaBianco { get; set; }


        public bool? FlagInserimentoAuto { get; set; }
        public bool? FlagInserimentoManuale { get; set; }


        public int? IdCentro { get; set; }


#nullable enable
        [Required(ErrorMessage = "Lavorazione is required")]
        public string? Lavorazione { get; set; }

#nullable enable
        [Required(ErrorMessage = "Fase is required")]
        public string? Fase { get; set; }


    }
}
