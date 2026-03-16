using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per Operatori e CentriVisibili.
/// </summary>
[Mapper]
public partial class OperatoriMapper
{
    #region Operatori

    /// <summary>
    /// Operatori → OperatoriDto (denormalizza Centro).
    /// </summary>
    [MapProperty(
        nameof(Operatori.IdcentroNavigation) + "." + nameof(CentriLavorazione.Centro),
        nameof(OperatoriDto.Centro))]
    [MapProperty(
        nameof(Operatori.CentriVisibilis),
        nameof(OperatoriDto.CentriVisibiliDto))]
    public partial OperatoriDto OperatoreToDto(Operatori entity);

    /// <summary>
    /// OperatoriDto → Operatori (ignora collection e navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(Operatori.IdRuoloNavigation))]
    [MapperIgnoreTarget(nameof(Operatori.IdcentroNavigation))]
    [MapperIgnoreTarget(nameof(Operatori.CentriVisibilis))]
    [MapperIgnoreTarget(nameof(Operatori.ProcedureClientes))]
    [MapperIgnoreTarget(nameof(Operatori.ProcedureLavorazionis))]
    [MapperIgnoreTarget(nameof(Operatori.ProduzioneOperatoris))]
    [MapperIgnoreTarget(nameof(Operatori.ProduzioneSistemas))]
    public partial Operatori DtoToOperatore(OperatoriDto dto);

    #endregion

    #region CentriVisibili

    /// <summary>
    /// CentriVisibili → CentriVisibiliDto (denormalizza Centro).
    /// </summary>
    [MapProperty(
        nameof(CentriVisibili.IdCentroNavigation) + "." + nameof(CentriLavorazione.Centro),
        nameof(CentriVisibiliDto.Centro))]
    public partial CentriVisibiliDto CentroVisibileToDto(CentriVisibili entity);

    /// <summary>
    /// CentriVisibiliDto → CentriVisibili (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(CentriVisibili.IdCentroNavigation))]
    [MapperIgnoreTarget(nameof(CentriVisibili.IdOperatoreNavigation))]
    public partial CentriVisibili DtoToCentroVisibile(CentriVisibiliDto dto);

    #endregion

    #region OperatoriNormalizzati

    /// <summary>OperatoriNormalizzati ↔ OperatoriNormalizzatiDto (nomi identici).</summary>
    public partial OperatoriNormalizzatiDto NormalizzatoToDto(OperatoriNormalizzati entity);
    public partial OperatoriNormalizzati DtoToNormalizzato(OperatoriNormalizzatiDto dto);

    #endregion
}
