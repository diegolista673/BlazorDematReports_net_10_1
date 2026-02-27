using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per LavorazioniFasi.
/// </summary>
[Mapper]
public partial class LavorazioniFasiMapper
{
    /// <summary>Dependency: mapper per TaskDaEseguire nelle collection.</summary>
    private readonly TaskDaEseguireMapper _taskMapper = new();

    #region FasiLavorazione

    /// <summary>FasiLavorazione ↔ FasiLavorazioneDto (nomi identici).</summary>
    public partial FasiLavorazioneDto FaseToDto(FasiLavorazione entity);

    [MapperIgnoreTarget(nameof(FasiLavorazione.LavorazioniFasiDataReadings))]
    [MapperIgnoreTarget(nameof(FasiLavorazione.LavorazioniFasiTipoTotales))]
    [MapperIgnoreTarget(nameof(FasiLavorazione.ProduzioneOperatoris))]
    [MapperIgnoreTarget(nameof(FasiLavorazione.ProduzioneSistemas))]
    [MapperIgnoreTarget(nameof(FasiLavorazione.ConfigurazioneFaseCentros))]
    public partial FasiLavorazione DtoToFase(FasiLavorazioneDto dto);

    #endregion

    #region LavorazioniFasiDataReading

    /// <summary>
    /// LavorazioniFasiDataReading → LavorazioniFasiDataReadingDto (denormalizza nomi).
    /// </summary>
    [MapperIgnoreTarget(nameof(LavorazioniFasiDataReadingDto.Lavorazione))]
    [MapProperty(
        nameof(LavorazioniFasiDataReading.IdFaseLavorazioneNavigation) + "." + nameof(FasiLavorazione.FaseLavorazione),
        nameof(LavorazioniFasiDataReadingDto.FaseLavorazione))]
    [MapProperty(
        nameof(LavorazioniFasiDataReading.TaskDaEseguires),
        nameof(LavorazioniFasiDataReadingDto.TaskDaEseguireDto))]
    public partial LavorazioniFasiDataReadingDto FaseReadingToDto(LavorazioniFasiDataReading entity);

    /// <summary>
    /// LavorazioniFasiDataReadingDto → LavorazioniFasiDataReading (ignora navigation).
    /// </summary>
    [MapProperty(
        nameof(LavorazioniFasiDataReadingDto.TaskDaEseguireDto),
        nameof(LavorazioniFasiDataReading.TaskDaEseguires))]
    [MapperIgnoreTarget(nameof(LavorazioniFasiDataReading.IdProceduraLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(LavorazioniFasiDataReading.IdFaseLavorazioneNavigation))]
    public partial LavorazioniFasiDataReading DtoToFaseReading(LavorazioniFasiDataReadingDto dto);

    #endregion

    #region LavorazioniFasiTipoTotale

    /// <summary>
    /// LavorazioniFasiTipoTotale → LavorazioniFasiTipoTotaleDto (denormalizza TipoTotale).
    /// </summary>
    [MapProperty(
        nameof(LavorazioniFasiTipoTotale.IdProceduraLavorazioneNavigation) + "." + nameof(ProcedureLavorazioni.NomeProcedura),
        nameof(LavorazioniFasiTipoTotaleDto.NomeProcedura))]
    [MapProperty(
        nameof(LavorazioniFasiTipoTotale.IdFaseNavigation) + "." + nameof(FasiLavorazione.FaseLavorazione),
        nameof(LavorazioniFasiTipoTotaleDto.Fase))]
    [MapProperty(
        nameof(LavorazioniFasiTipoTotale.IdTipologiaTotaleNavigation) + "." + nameof(TipologieTotali.TipoTotale),
        nameof(LavorazioniFasiTipoTotaleDto.TipologiaTotale))]
    public partial LavorazioniFasiTipoTotaleDto TipoTotaleToDto(LavorazioniFasiTipoTotale entity);

    /// <summary>
    /// LavorazioniFasiTipoTotaleDto → LavorazioniFasiTipoTotale (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(LavorazioniFasiTipoTotale.IdProceduraLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(LavorazioniFasiTipoTotale.IdFaseNavigation))]
    [MapperIgnoreTarget(nameof(LavorazioniFasiTipoTotale.IdTipologiaTotaleNavigation))]
    public partial LavorazioniFasiTipoTotale DtoToTipoTotale(LavorazioniFasiTipoTotaleDto dto);

    #endregion
}
