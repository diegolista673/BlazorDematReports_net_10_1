using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per CentriLavorazione e cross-mapping con CentriVisibili.
/// </summary>
[Mapper]
public partial class CentriMapper
{
    #region CentriLavorazione

    /// <summary>CentriLavorazione ↔ CentriLavorazioneDto (nomi quasi identici).</summary>
    [MapProperty(nameof(CentriLavorazione.Idcentro), nameof(CentriLavorazioneDto.Idcentro))]
    public partial CentriLavorazioneDto CentroToDto(CentriLavorazione entity);

    [MapProperty(nameof(CentriLavorazioneDto.Idcentro), nameof(CentriLavorazione.Idcentro))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.Clientis))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.Operatoris))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.ProduzioneSistemas))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.CentriVisibilis))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.ConfigurazioneFaseCentros))]
    public partial CentriLavorazione DtoToCentro(CentriLavorazioneDto dto);

    #endregion

    #region CentriVisibili → CentriLavorazione (cross-type)

    /// <summary>
    /// CentriVisibiliDto → CentriLavorazione (cross-type per legacy compatibility).
    /// </summary>
    [MapProperty(nameof(CentriVisibiliDto.IdCentro), nameof(CentriLavorazione.Idcentro))]
    [MapProperty(nameof(CentriVisibiliDto.Centro), nameof(CentriLavorazione.Centro))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.Sigla))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.Clientis))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.Operatoris))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.ProduzioneSistemas))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.CentriVisibilis))]
    [MapperIgnoreTarget(nameof(CentriLavorazione.ConfigurazioneFaseCentros))]
    public partial CentriLavorazione CentroVisibileDtoToCentro(CentriVisibiliDto dto);

    /// <summary>CentriLavorazione → CentriVisibiliDto (cross-type inverso).</summary>
    [MapProperty(nameof(CentriLavorazione.Idcentro), nameof(CentriVisibiliDto.IdCentro))]
    [MapProperty(nameof(CentriLavorazione.Centro), nameof(CentriVisibiliDto.Centro))]
    public partial CentriVisibiliDto CentroToCentroVisibileDto(CentriLavorazione entity);

    #endregion
}
