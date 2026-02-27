using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per TaskDaEseguire.
/// </summary>
[Mapper]
public partial class TaskDaEseguireMapper
{
    /// <summary>
    /// TaskDaEseguire → TaskDaEseguireDto (denormalizza FaseLavorazione e TipoFonte).
    /// IdProceduraLavorazione, IdFaseLavorazione, FaseLavorazione non sono mappabili via
    /// IdLavorazioneFaseDateReadingNavigation (non-nullable, mai inclusa in query EF → NRE).
    /// Vengono arricchiti in MapFasiCollection dal contesto LavorazioniFasiDataReading parent.
    /// TaskName   ← IdTaskHangFire (chiave Hangfire leggibile, es. "hdl:15-10-nome:ader4").
    /// Descrizione ← IdConfigurazioneDatabaseNavigation.DescrizioneConfigurazione.
    /// </summary>
    [MapperIgnoreTarget(nameof(TaskDaEseguireDto.IdProceduraLavorazione))]
    [MapperIgnoreTarget(nameof(TaskDaEseguireDto.IdFaseLavorazione))]
    [MapperIgnoreTarget(nameof(TaskDaEseguireDto.FaseLavorazione))]
    [MapProperty(nameof(TaskDaEseguire.IdTaskHangFire), nameof(TaskDaEseguireDto.TaskName))]
    [MapProperty(
        nameof(TaskDaEseguire.IdConfigurazioneDatabaseNavigation) + "." + nameof(ConfigurazioneFontiDati.DescrizioneConfigurazione),
        nameof(TaskDaEseguireDto.Descrizione))]
    public partial TaskDaEseguireDto EntityToDto(TaskDaEseguire entity);

    /// <summary>
    /// Mapping custom per TipoFonte da enum a stringa.
    /// </summary>
    private string? MapTipoFonte(ConfigurazioneFontiDati? config)
        => config?.TipoFonte.ToString();

    /// <summary>
    /// TaskDaEseguireDto → TaskDaEseguire (ignora navigation e ID).
    /// </summary>
    [MapperIgnoreTarget(nameof(TaskDaEseguire.IdTaskDaEseguire))]
    [MapperIgnoreTarget(nameof(TaskDaEseguire.IdLavorazioneFaseDateReading))]
    [MapperIgnoreTarget(nameof(TaskDaEseguire.IdLavorazioneFaseDateReadingNavigation))]
    [MapperIgnoreTarget(nameof(TaskDaEseguire.IdConfigurazioneDatabaseNavigation))]
    [MapperIgnoreTarget(nameof(TaskDaEseguire.Stato))]
    [MapperIgnoreTarget(nameof(TaskDaEseguire.DataStato))]
    public partial TaskDaEseguire DtoToEntity(TaskDaEseguireDto dto);
}
