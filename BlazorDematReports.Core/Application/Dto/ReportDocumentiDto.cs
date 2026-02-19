namespace BlazorDematReports.Core.Application.Dto
{
    public class ReportDocumentiDto
    {

        public string? NomeProcedura { get; set; }
        public string? FaseLavorazione { get; set; }
        public int? Documenti { get; set; }
        public int? Fogli { get; set; }
        public int? Pagine { get; set; }
        public int? Scarti { get; set; }
        public int? PagineSenzaBianco { get; set; }

        public bool Reading { get; set; }


    }
}

