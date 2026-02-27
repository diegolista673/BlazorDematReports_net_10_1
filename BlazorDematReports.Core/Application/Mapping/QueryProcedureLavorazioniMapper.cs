using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per QueryProcedureLavorazioni.
/// </summary>
[Mapper]
public partial class QueryProcedureLavorazioniMapper
{
    /// <summary>
    /// QueryProcedureLavorazioni → QueryProcedureLavorazioniDto (denormalizza NomeProcedura).
    /// </summary>
    [MapProperty(
        nameof(QueryProcedureLavorazioni.IdproceduraLavorazioneNavigation) + "." + nameof(ProcedureLavorazioni.NomeProcedura),
        nameof(QueryProcedureLavorazioniDto.NomeProcedura))]
    public partial QueryProcedureLavorazioniDto EntityToDto(QueryProcedureLavorazioni entity);

    /// <summary>
    /// QueryProcedureLavorazioniDto → QueryProcedureLavorazioni (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(QueryProcedureLavorazioni.IdproceduraLavorazioneNavigation))]
    public partial QueryProcedureLavorazioni DtoToEntity(QueryProcedureLavorazioniDto dto);
}
