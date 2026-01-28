namespace Entities.Models
{
    public class ReportAnnualeTotaliDedicati
    {
        public int? Anno { get; set; }
        public int? Mese { get; set; }
        public string? MeseString { get; set; }
        public string? TipoTotale { get; set; }
        public int? Totale { get; set; }
        // Added to allow filtering per fase in a single aggregated query
        public int? IdFaseLavorazione { get; set; }
    }
}
