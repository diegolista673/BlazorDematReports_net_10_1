using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.DataReading.Dto;
using BlazorDematReports.Core.Utility.Models;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per ProduzioneSistema e DatiElaborati.
/// Source generator produce codice a compile-time.
/// </summary>
[Mapper]
public partial class ProduzioneSistemaMapper
{
    /// <summary>
    /// ProduzioneSistemaDTO → ProduzioneSistema (ignora ID e navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(ProduzioneSistema.IdProduzioneSistema))]
    [MapperIgnoreTarget(nameof(ProduzioneSistema.IdCentroNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneSistema.IdFaseLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneSistema.IdOperatoreNavigation))]
    [MapperIgnoreTarget(nameof(ProduzioneSistema.IdProceduraLavorazioneNavigation))]
    public partial ProduzioneSistema DtoToEntity(ProduzioneSistemaDTO dto);

    /// <summary>
    /// ProduzioneSistema → ProduzioneSistemaDto (denormalizza navigation).
    /// </summary>
    [MapProperty(
        nameof(ProduzioneSistema.IdOperatoreNavigation) + "." + nameof(Operatori.Operatore),
        nameof(ProduzioneSistemaDto.Operatore))]
    [MapProperty(
        nameof(ProduzioneSistema.IdProceduraLavorazioneNavigation) + "." + nameof(ProcedureLavorazioni.NomeProcedura),
        nameof(ProduzioneSistemaDto.Lavorazione))]
    [MapProperty(
        nameof(ProduzioneSistema.IdFaseLavorazioneNavigation) + "." + nameof(FasiLavorazione.FaseLavorazione),
        nameof(ProduzioneSistemaDto.Fase))]
    public partial ProduzioneSistemaDto EntityToDto(ProduzioneSistema entity);

    /// <summary>DatiElaborati ↔ ProduzioneSistemaDTO (nomi identici).</summary>
    public partial ProduzioneSistemaDTO DatiElaboratiToDto(DatiElaborati dati);
    public partial DatiElaborati DtoToDatiElaborati(ProduzioneSistemaDTO dto);
}
