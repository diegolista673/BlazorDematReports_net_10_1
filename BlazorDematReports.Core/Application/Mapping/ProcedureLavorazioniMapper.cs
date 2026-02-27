using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per ProcedureLavorazioni (projection principale).
/// </summary>
[Mapper]
public partial class ProcedureLavorazioniMapper
{
    /// <summary>Dependency: mapper per nested entities.</summary>
    private readonly LavorazioniFasiMapper _fasiMapper = new();
    private readonly QueryProcedureLavorazioniMapper _queryMapper = new();

    /// <summary>
    /// ProcedureLavorazioni → ProcedureLavorazioniDto (projection con denormalizzazione).
    /// </summary>
    public partial ProcedureLavorazioniDto ProceduraToDto(ProcedureLavorazioni entity);

    /// <summary>Mapping custom per Centro (4-level chain).</summary>
    private string? MapCentro(ProcedureCliente? procCliente)
        => procCliente?.IdclienteNavigation?.IdCentroLavorazioneNavigation?.Centro;

    /// <summary>Mapping custom per IdCliente.</summary>
    private int MapIdCliente(ProcedureCliente? procCliente)
        => procCliente?.IdclienteNavigation?.IdCliente ?? 0;

    /// <summary>Mapping custom per NomeCliente.</summary>
    private string? MapNomeCliente(ProcedureCliente? procCliente)
        => procCliente?.IdclienteNavigation?.NomeCliente;

    /// <summary>Mapping custom per ProceduraCliente.</summary>
    private string? MapProceduraCliente(ProcedureCliente? procCliente)
        => procCliente?.ProceduraCliente;

    /// <summary>Mapping custom per FormatoDatiProduzione.</summary>
    private string? MapFormatoDatiProduzione(FormatoDati? formato)
        => formato?.FormatoDatiProduzione;

    /// <summary>Mapping custom per Reparto.</summary>
    private string? MapReparto(RepartiProduzione? reparto)
        => reparto?.Reparti;

    /// <summary>Mapping custom per QueryProcedureLavorazioniDto (collection).</summary>
    private List<QueryProcedureLavorazioniDto> MapQueryCollection(ICollection<QueryProcedureLavorazioni> queries)
        => queries.Select(_queryMapper.EntityToDto).ToList();

    /// <summary>Mapping custom per LavorazioniFasiDataReadingsDto (collection).</summary>
    private List<LavorazioniFasiDataReadingDto> MapFasiCollection(ICollection<LavorazioniFasiDataReading> fasi)
        => fasi.Select(_fasiMapper.FaseReadingToDto).ToList();

    /// <summary>
    /// ProcedureLavorazioniDto → ProcedureLavorazioni (ignora navigation e collection).
    /// </summary>
    [MapProperty(
        nameof(ProcedureLavorazioniDto.LavorazioniFasiDataReadingsDto),
        nameof(ProcedureLavorazioni.LavorazioniFasiDataReadings))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.Attiva))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdformatoDatiProduzioneNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdoperatoreNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdproceduraClienteNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdrepartiNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.LavorazioniFasiTipoTotales))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ProduzioneOperatoris))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ProduzioneSistemas))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.QueryProcedureLavorazionis))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ConfigurazioneFaseCentros))]
    public partial ProcedureLavorazioni DtoToProcedura(ProcedureLavorazioniDto dto);
}
