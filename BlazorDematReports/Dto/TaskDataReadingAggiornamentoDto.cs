using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Dto
{
    public class TaskDataReadingAggiornamentoDto
    {

        public string Lavorazione { get; set; } = string.Empty;
        public int IdLavorazione { get; set; }
        public string FaseLavorazione { get; set; } = string.Empty;
        public int IdFase { get; set; }
        public DateTime DataInizioLavorazione { get; set; }
        public DateTime DataFineLavorazione { get; set; }
        public DateTime DataAggiornamento { get; set; }
        public int Risultati { get; set; }
        public bool EsitoLetturaDato { get; set; }
        public string DescrizioneEsito { get; set; } = string.Empty;



        [Required]
        public DateTime? StartDataLavorazione { get; set; }

        [Required]
        public DateTime? EndDataLavorazione { get; set; }








    }
}
