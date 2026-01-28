namespace Entities.Models
{
    public class ReportProduzioneCompleta
    {
        public int? IdProduzioneSistema { get; set; }
        public int? IdOperatore { get; set; }

        public int? IdCentro { get; set; }

        public string? Operatore { get; set; }
        public int? IdProceduraLavorazione { get; set; }
        public int? IdFaseLavorazione { get; set; }

        public DateTime? DataLavorazione { get; set; }


        public double? TempoLavOreCent { get; set; }
        public string? NomeProcedura { get; set; }
        public string? FaseLavorazione { get; set; }
        public string? OperatoreNonRiconosciuto { get; set; }
        public int? Documenti { get; set; }
        public int? Fogli { get; set; }
        public int? Pagine { get; set; }
        public int? Scarti { get; set; }
        public int? PagineSenzaBianco { get; set; }
        public string? AltraUtenza { get; set; }
        public bool FlagDataReading { get; set; }
        public int Esito { get; set; }
        public string? DescrizioneEsito { get; set; }


    }
}
