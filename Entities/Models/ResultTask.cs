namespace Entities.Models
{
    public class ResultTask
    {
        public string? NomeLavorazione { get; set; }
        public string? FaseLavorazione { get; set; }

        public int ThreadNum { get; set; }
        public int IdFase { get; set; }

        public int IdLavorazione { get; set; }
        public string? Status { get; set; }
        public int Nrow { get; set; }
        public DateTime DataLavorazione { get; set; }
        public DateTime DataAggiornamento { get; set; }

        public string? StatoLavorazione { get; set; }
        public bool LavorazioneImplementata { get; set; }

        public bool EsitoLetturaDato { get; set; }
        public string? DescrizioneEsito { get; set; }
    }
}
