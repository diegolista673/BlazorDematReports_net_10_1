namespace Entities.Models
{
    public class ReportEsportazioneOreDocumenti
    {
        public string? ProceduraCliente { get; set; }
        public string? NomeProcedura { get; set; }
        public string? FaseLavorazione { get; set; }
        public double TempoLavOreCent { get; set; }
        public int Documenti { get; set; }
        public int Fogli { get; set; }
        public int Pagine { get; set; }
        public int Scarti { get; set; }
        public int PagineSenzaBianco { get; set; }
        public int IdCentro { get; set; }


    }
}

