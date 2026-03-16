using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per ProduzioneOperatori e TipologieTotaliProduzione.
/// </summary>
[Mapper]
public partial class ProduzioneOperatoriMapper
{
    #region TipologieTotaliProduzione

    /// <summary>
    /// TipologieTotaliProduzione → TipologieTotaliProduzioneDto (denormalizza TipoTotale).
    /// </summary>
    [MapProperty(
        nameof(TipologieTotaliProduzione.IdtipologiaTotaleNavigation) + "." + nameof(TipologieTotali.TipoTotale),
        nameof(TipologieTotaliProduzioneDto.TipologiaTotale))]
    public partial TipologieTotaliProduzioneDto TipologiaToDto(TipologieTotaliProduzione entity);

    /// <summary>
    /// TipologieTotaliProduzioneDto → TipologieTotaliProduzione (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(TipologieTotaliProduzione.IdtipologiaTotaleNavigation))]
    [MapperIgnoreTarget(nameof(TipologieTotaliProduzione.IdproduzioneOperatoreNavigation))]
    public partial TipologieTotaliProduzione DtoToTipologia(TipologieTotaliProduzioneDto dto);

    #endregion

    #region ProduzioneOperatori

    /// <summary>
    /// ProduzioneOperatori → ProduzioneOperatoriDto (denormalizza navigation + collection).
    /// </summary>
    [MapProperty(
        nameof(ProduzioneOperatori.IdOperatoreNavigation) + "." + nameof(Operatori.Operatore),
        nameof(ProduzioneOperatoriDto.Operatore))]
    [MapProperty(
        nameof(ProduzioneOperatori.IdProceduraLavorazioneNavigation) + "." + nameof(ProcedureLavorazioni.NomeProcedura),
        nameof(ProduzioneOperatoriDto.Lavorazione))]
    [MapProperty(
        nameof(ProduzioneOperatori.IdFaseLavorazioneNavigation) + "." + nameof(FasiLavorazione.FaseLavorazione),
        nameof(ProduzioneOperatoriDto.Fase))]
    [MapProperty(
        nameof(ProduzioneOperatori.IdTurnoNavigation) + "." + nameof(Turni.Turno),
        nameof(ProduzioneOperatoriDto.Turno))]
    [MapProperty(
        nameof(ProduzioneOperatori.TipologieTotaliProduziones),
        nameof(ProduzioneOperatoriDto.TipologieTotaliProduzioneDto))]
    public partial ProduzioneOperatoriDto OperatoreToDto(ProduzioneOperatori entity);

    /// <summary>
    /// ProduzioneOperatoriDto → ProduzioneOperatori (ignora collection e navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(ProduzioneOperatori.TipologieTotaliProduziones))]
    [MapperIgnoreTarget(nameof(ProduzioneOperatori.IdOperatoreNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneOperatori.IdProceduraLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneOperatori.IdFaseLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneOperatori.IdTurnoNavigation))]
    public partial ProduzioneOperatori DtoToOperatore(ProduzioneOperatoriDto dto);

    #endregion
}
