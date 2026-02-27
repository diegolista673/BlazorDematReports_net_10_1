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
    /// QueryProcedureLavorazioni → QueryProcedureLavorazioniDto (denormalizza Centro dalla chain complessa).
    /// </summary>
    [MapProperty(
        nameof(QueryProcedureLavorazioni.IdproceduraLavorazioneNavigation) + "." + nameof(ProcedureLavorazioni.NomeProcedura),
        nameof(QueryProcedureLavorazioniDto.NomeProcedura))]
    public partial QueryProcedureLavorazioniDto EntityToDto(QueryProcedureLavorazioni entity);

    /// <summary>
    /// Mapping custom per Centro (chain profonda: proc → cliente → centroLavorazione).
    /// </summary>
    private string? MapCentro(ProcedureLavorazioni? proc)
        => proc?.IdproceduraClienteNavigation?.IdclienteNavigation?.IdCentroLavorazioneNavigation?.Centro;

    /// <summary>
    /// QueryProcedureLavorazioniDto → QueryProcedureLavorazioni (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(QueryProcedureLavorazioni.IdproceduraLavorazioneNavigation))]
    public partial QueryProcedureLavorazioni DtoToEntity(QueryProcedureLavorazioniDto dto);
}
