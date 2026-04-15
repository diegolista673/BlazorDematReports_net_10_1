using Entities.Models;

namespace BlazorDematReports.Core.Application.Dto
{
    public class FaseReportData
    {
        public int IdFaseLavorazione { get; set; }
        public string FaseNome { get; set; } = string.Empty;
        public bool FlagDataReading { get; set; }
        public List<ReportOreDocumenti>? ReportData { get; set; }
        public List<ReportAnnualeTotaliDedicati>? ReportTotaliDedicati { get; set; }
        public List<ReportAnniSistema>? ReportDataLast5Years { get; set; }
        public int TotaleDocumenti { get; set; }
        public int TotaleFogli { get; set; }
        public int TotalePagine { get; set; }
        public int TotaleOre { get; set; }
        public double TotaleFteMese { get; set; }
    }
}