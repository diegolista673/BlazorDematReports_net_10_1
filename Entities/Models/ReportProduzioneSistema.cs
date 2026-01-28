namespace Entities.Models
{
    public class ReportProduzioneSistema
    {
        public int IdProduzioneSistema { get; set; }
        public int IdOperatore { get; set; }
        public string? Operatore { get; set; }
        public string? OperatoreNonRiconosciuto { get; set; }
        public int IdProceduraLavorazione { get; set; }
        public int IdFaseLavorazione { get; set; }
        public DateTime DataLavorazione { get; set; }
        public DateTime? DataAggiornamento { get; set; }
        public int? Documenti { get; set; }
        public int? Fogli { get; set; }
        public int? Pagine { get; set; }
        public int? Scarti { get; set; }
        public bool? FlagInserimentoAuto { get; set; }
        public bool? FlagInserimentoManuale { get; set; }
        public int? PagineSenzaBianco { get; set; }
        public int? IdCentro { get; set; }

        public int? Mese { get; set; }
        public string? MeseString { get; set; }
        public int? Anno { get; set; }
        public double? TotaleOreUomo { get; set; }
        public double? FteMEse { get; set; }

        public int? TipoTotale { get; set; }
        public int? Totale { get; set; }




    }
}
