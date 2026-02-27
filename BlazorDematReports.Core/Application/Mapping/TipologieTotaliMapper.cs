using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per TipologieTotali e TipologieTotaliProduzione.
/// </summary>
[Mapper]
public partial class TipologieTotaliMapper
{
    /// <summary>TipologieTotali ↔ TipologieTotaliDto (nomi identici).</summary>
    public partial TipologieTotaliDto TotaleToDto(TipologieTotali entity);

    [MapperIgnoreTarget(nameof(TipologieTotali.LavorazioniFasiTipoTotales))]
    [MapperIgnoreTarget(nameof(TipologieTotali.TipologieTotaliProduziones))]
    public partial TipologieTotali DtoToTotale(TipologieTotaliDto dto);

    /// <summary>
    /// TipologieTotaliProduzione → TipologieTotaliDto (cross-type mapping con rename).
    /// </summary>
    [MapProperty(nameof(TipologieTotaliProduzione.IdtipologiaTotale), nameof(TipologieTotaliDto.IdTipoTotale))]
    [MapProperty(
        nameof(TipologieTotaliProduzione.IdtipologiaTotaleNavigation) + "." + nameof(TipologieTotali.TipoTotale),
        nameof(TipologieTotaliDto.TipoTotale))]
    public partial TipologieTotaliDto ProduzioneTotaleToDto(TipologieTotaliProduzione entity);
}
