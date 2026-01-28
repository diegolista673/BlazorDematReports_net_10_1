namespace Entities.Models
{
    public class ReportGiornalieroTotaliDedicati
    {
        public string? Operatore { get; set; }
        public DateTime DataLavorazione { get; set; }
        public string? FaseLavorazione { get; set; }
        public string? Lavorazione { get; set; }
        public string? TipoTotale { get; set; }
        public int? Totale { get; set; }

    }
}
