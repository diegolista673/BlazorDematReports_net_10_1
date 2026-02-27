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
    /// <summary>Dependency: mapper per le fasi nested.</summary>
    private readonly LavorazioniFasiMapper _fasiMapper = new();

    /// <summary>Dependency: mapper per le query nested.</summary>
    private readonly QueryProcedureLavorazioniMapper _queryMapper = new();

    /// <summary>
    /// ProcedureLavorazioni → ProcedureLavorazioniDto (projection con denormalizzazione).
    /// I campi denormalizzati vengono risolti tramite path delle navigation properties.
    /// </summary>
    [MapProperty("IdformatoDatiProduzioneNavigation.FormatoDatiProduzione", "FormatoDatiProduzione")]
    [MapProperty("IdrepartiNavigation.Reparti", "Reparto")]
    [MapProperty("IdproceduraClienteNavigation.ProceduraCliente", "ProceduraCliente")]
    [MapProperty("IdproceduraClienteNavigation.IdclienteNavigation.NomeCliente", "NomeCliente")]
    [MapProperty("IdproceduraClienteNavigation.IdclienteNavigation.IdCliente", "IdCliente")]
    [MapProperty("IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro", "Centro")]
    [MapProperty("NomeServizio", "ServizioElaborazione")]
    [MapProperty("LavorazioniFasiDataReadings", "LavorazioniFasiDataReadingsDto")]
    [MapProperty("QueryProcedureLavorazionis", "QueryProcedureLavorazioniDto")]
    public partial ProcedureLavorazioniDto ProceduraToDto(ProcedureLavorazioni entity);

    /// <summary>
    /// Mapping custom per LavorazioniFasiDataReadingsDto.
    /// Arricchisce ogni task con IdProceduraLavorazione, IdFaseLavorazione e FaseLavorazione
    /// recuperandoli dal parent LavorazioniFasiDataReading (evita navigation non incluse in EF).
    /// </summary>
    private List<LavorazioniFasiDataReadingDto> MapFasiCollection(ICollection<LavorazioniFasiDataReading> fasi)
        => (fasi ?? [])
            .Select(fase =>
            {
                var dto = _fasiMapper.FaseReadingToDto(fase);
                foreach (var task in dto.TaskDaEseguireDto)
                {
                    task.IdProceduraLavorazione = fase.IdProceduraLavorazione;
                    task.IdFaseLavorazione = fase.IdFaseLavorazione;
                    task.FaseLavorazione = dto.FaseLavorazione;
                }
                return dto;
            })
            .ToList();

    /// <summary>Mapping custom per QueryProcedureLavorazioniDto (collection).</summary>
    private List<QueryProcedureLavorazioniDto> MapQueryCollection(ICollection<QueryProcedureLavorazioni> queries)
        => (queries ?? []).Select(_queryMapper.EntityToDto).ToList();

    /// <summary>
    /// ProcedureLavorazioniDto → ProcedureLavorazioni (ignora navigation e collection).
    /// </summary>
    [MapProperty(
        nameof(ProcedureLavorazioniDto.LavorazioniFasiDataReadingsDto),
        nameof(ProcedureLavorazioni.LavorazioniFasiDataReadings))]
    [MapProperty(
        nameof(ProcedureLavorazioniDto.ServizioElaborazione),
        nameof(ProcedureLavorazioni.NomeServizio))]
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
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.TaskServiceLavorazionis))]
    public partial ProcedureLavorazioni DtoToProcedura(ProcedureLavorazioniDto dto);
}
