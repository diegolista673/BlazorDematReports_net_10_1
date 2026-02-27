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
    /// </summary>
    [MapProperty(
        nameof(TaskDaEseguire.IdLavorazioneFaseDateReadingNavigation) + "." + nameof(LavorazioniFasiDataReading.IdProceduraLavorazione),
        nameof(TaskDaEseguireDto.IdProceduraLavorazione))]
    [MapProperty(
        nameof(TaskDaEseguire.IdLavorazioneFaseDateReadingNavigation) + "." + nameof(LavorazioniFasiDataReading.IdFaseLavorazione),
        nameof(TaskDaEseguireDto.IdFaseLavorazione))]
    [MapProperty(
        nameof(TaskDaEseguire.IdLavorazioneFaseDateReadingNavigation) + "." + nameof(LavorazioniFasiDataReading.IdFaseLavorazioneNavigation) + "." + nameof(FasiLavorazione.FaseLavorazione),
        nameof(TaskDaEseguireDto.FaseLavorazione))]
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
