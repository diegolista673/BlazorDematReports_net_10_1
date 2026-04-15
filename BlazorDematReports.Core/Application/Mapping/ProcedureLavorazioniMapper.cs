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
    [MapProperty("LavorazioniFasiDataReadings", "LavorazioniFasiDataReadingsDto")]
    public partial ProcedureLavorazioniDto ProceduraToDto(ProcedureLavorazioni entity);

    /// <summary>
    /// Mapping custom per LavorazioniFasiDataReadingsDto.
    /// Arricchisce ogni task con IdProceduraLavorazione, IdFaseLavorazione, FaseLavorazione,
    /// TaskName (← IdTaskHangFire), Descrizione (← DescrizioneConfigurazione) e TipoFonte
    /// direttamente dalle entità originali, bypassando il mapping inline generato da Mapperly.
    /// </summary>
    private List<LavorazioniFasiDataReadingDto> MapFasiCollection(ICollection<LavorazioniFasiDataReading> fasi)
        => (fasi ?? [])
            .Select(fase =>
            {
                var dto = _fasiMapper.FaseReadingToDto(fase);

                // Lookup O(1) dalle entità originali (navigation properties già caricate da EF)
                var entityById = (fase.TaskDaEseguires ?? [])
                    .ToDictionary(t => t.IdTaskDaEseguire);

                foreach (var task in dto.TaskDaEseguireDto)
                {
                    task.IdProceduraLavorazione = fase.IdProceduraLavorazione;
                    task.IdFaseLavorazione       = fase.IdFaseLavorazione;
                    task.FaseLavorazione         = dto.FaseLavorazione;

                    if (!entityById.TryGetValue(task.IdTaskDaEseguire, out var entity))
                        continue;

                    task.TaskName    = entity.IdConfigurazioneDatabaseNavigation?.HandlerClassName;
                    task.Descrizione = entity.IdConfigurazioneDatabaseNavigation?.DescrizioneConfigurazione;
                    task.TipoFonte   = entity.IdConfigurazioneDatabaseNavigation?.TipoFonte.ToString();
                }

                return dto;
            })
            .ToList();



    /// <summary>
    /// ProcedureLavorazioniDto → ProcedureLavorazioni (ignora navigation e collection).
    /// </summary>
    [MapProperty(
        nameof(ProcedureLavorazioniDto.LavorazioniFasiDataReadingsDto),
        nameof(ProcedureLavorazioni.LavorazioniFasiDataReadings))]

    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdformatoDatiProduzioneNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdoperatoreNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdproceduraClienteNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.IdrepartiNavigation))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.LavorazioniFasiTipoTotales))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ProduzioneOperatoris))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ProduzioneSistemas))]
    [MapperIgnoreTarget(nameof(ProcedureLavorazioni.ConfigurazioneFaseCentros))]

    public partial ProcedureLavorazioni DtoToProcedura(ProcedureLavorazioniDto dto);
}
