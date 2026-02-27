using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per RepartiProduzione e FormatoDati (entità semplici).
/// </summary>
[Mapper]
public partial class AltriDatiMapper
{
    /// <summary>RepartiProduzione ↔ RepartiProduzioneDto (nomi identici).</summary>
    public partial RepartiProduzioneDto RepartoToDto(RepartiProduzione entity);

    [MapperIgnoreTarget(nameof(RepartiProduzione.ProcedureLavorazionis))]
    public partial RepartiProduzione DtoToReparto(RepartiProduzioneDto dto);

    /// <summary>FormatoDati ↔ FormatoDatiDto (nomi identici).</summary>
    public partial FormatoDatiDto FormatoToDto(FormatoDati entity);

    [MapperIgnoreTarget(nameof(FormatoDati.ProcedureLavorazionis))]
    public partial FormatoDati DtoToFormato(FormatoDatiDto dto);
}
