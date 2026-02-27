using BlazorDematReports.Core.Application.Dto;
using Entities.Models;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per entità di reporting.
/// </summary>
[Mapper]
public partial class ReportsMapper
{
    /// <summary>ReportDocumenti → ConfigReportDocumenti (nomi identici).</summary>
    public partial ConfigReportDocumenti ReportToConfig(ReportDocumenti report);

    /// <summary>
    /// ReportProduzioneCompleta → ReportProduzioneCompleta (self-map per merge).
    /// </summary>
    public partial ReportProduzioneCompleta MergeReport(ReportProduzioneCompleta source);
}
