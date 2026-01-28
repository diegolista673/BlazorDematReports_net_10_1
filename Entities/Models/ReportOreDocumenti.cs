namespace Entities.Models
{
    public class ReportOreDocumenti
    {
        public int? Anno { get; set; }
        public int? Mese { get; set; }
        public string? MeseString { get; set; }
        public int IdFaseLavorazione { get; set; }
        public double? TotaleOreUomo { get; set; }
        public double? FteMese { get; set; }
        public int? Documenti { get; set; }
        public int? Fogli { get; set; }
        public int? Pagine { get; set; }
        public int? Scarti { get; set; }
        public int? PagineSenzaBianco { get; set; }

    }
}
