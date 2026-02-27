using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per Clienti e ProcedureCliente.
/// </summary>
[Mapper]
public partial class ClientiMapper
{
    #region Clienti

    /// <summary>
    /// Clienti → ClientiDto (denormalizza Centro).
    /// </summary>
    [MapProperty(
        nameof(Clienti.IdCentroLavorazioneNavigation) + "." + nameof(CentriLavorazione.Centro),
        nameof(ClientiDto.Centro))]
    public partial ClientiDto ClienteToDto(Clienti entity);

    /// <summary>
    /// ClientiDto → Clienti (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(Clienti.IdCentroLavorazioneNavigation))]
    [MapperIgnoreTarget(nameof(Clienti.ProcedureClientes))]
    public partial Clienti DtoToCliente(ClientiDto dto);

    #endregion

    #region ProcedureCliente

    /// <summary>
    /// ProcedureCliente → ProcedureClienteDto (denormalizza Cliente, Centro, Operatore).
    /// </summary>
    public partial ProcedureClienteDto ProceduraClienteToDto(ProcedureCliente entity);


    /// <summary>
    /// ProcedureClienteDto → ProcedureCliente (ignora navigation).
    /// </summary>
    [MapperIgnoreTarget(nameof(ProcedureCliente.IdclienteNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureCliente.IdoperatoreNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureCliente.ProcedureLavorazionis))]
    public partial ProcedureCliente DtoToProceduraCliente(ProcedureClienteDto dto);

    #endregion
}
