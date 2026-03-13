using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Models;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;

/// <summary>
/// Handler base per task produzione HERA16.
/// Interroga la tabella <c>HERA16</c> tramite EF Core
/// (popolata dall'ingestion CSV via <see cref="Hera16IngestionProcessor"/>)
/// e restituisce i dati di produzione per-operatore.
/// Aggiorna <c>ElaboratoIl</c> sui record letti al termine dell'esecuzione.
/// </summary>
public abstract class Hera16QueryHandlerBase : IProductionDataHandler
{
    protected readonly ILogger Logger;
    protected readonly IServiceScopeFactory ScopeFactory;

    protected Hera16QueryHandlerBase(
        ILogger logger,
        IServiceScopeFactory scopeFactory)
    {
        Logger       = logger;
        ScopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public abstract string HandlerCode { get; }

    /// <inheritdoc />
    public string? GetServiceCode() => string.Empty;

    /// <inheritdoc />
    public HandlerMetadata GetMetadata() => new()
    {
        ServiceCode          = LavorazioniCodes.HERA16,
        RequiresEmailService = false,
        Category             = "HERA16 SQL Query"
    };

    /// <summary>
    /// Esegue la query LINQ su HERA16, filtra nel DB e aggrega in memoria per operatore/data.
    /// Restituisce i dati aggregati e la lista di <c>IdCounter</c> dei record letti,
    /// usata per aggiornare <c>ElaboratoIl</c>.
    /// </summary>
    protected abstract Task<(List<DatiLavorazione> Dati, List<int> Ids)> QueryHera16Async(
        DematReportsContext db,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct);

    /// <inheritdoc />
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        ProductionExecutionContext context,
        CancellationToken ct = default)
    {
        var startDate = context.StartDataLavorazione;
        var endDate   = context.EndDataLavorazione ?? context.StartDataLavorazione;

        Logger.LogInformation(
            "Handler {Code}: query su HERA16, periodo {Start:dd/MM/yyyy}-{End:dd/MM/yyyy}",
            HandlerCode, startDate, endDate);

        using var scope = ScopeFactory.CreateScope();
        var db               = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
        var hera16DataService = scope.ServiceProvider.GetRequiredService<IHera16DataService>();

        var (datiLavorazione, ids) = await QueryHera16Async(db, startDate, endDate, ct);

        if (ids.Count > 0)
            await hera16DataService.MarkAsProcessedAsync(ids, ct);

        Logger.LogInformation(
            "Handler {Code}: {Count} record estratti da HERA16, {Ids} righe marcate come elaborate",
            HandlerCode, datiLavorazione.Count, ids.Count);

        return datiLavorazione;
    }

    /// <summary>
    /// Aggrega in memoria le righe proiettate contando le combinazioni distinte
    /// (codice_mercato + codice_offerta + tipo_documento) per operatore e data.
    /// </summary>
    protected static List<DatiLavorazione> AggregaPerOperatore<T>(
        IEnumerable<T> rows,
        Func<T, string?> getOperatore,
        Func<T, DateTime> getData,
        Func<T, string> getKey)
    {
        return rows
            .GroupBy(r => (Operatore: getOperatore(r), Data: getData(r)))
            .Select(g =>
            {
                var count = g.Select(getKey).Distinct().Count();
                return new DatiLavorazione
                {
                    Operatore                     = g.Key.Operatore ?? string.Empty,
                    DataLavorazione               = g.Key.Data,
                    Documenti                     = count,
                    Fogli                         = count,
                    Pagine                        = count * 2,
                    AppartieneAlCentroSelezionato = true
                };
            })
            .ToList();
    }
}

/// <summary>
/// Handler HERA16 per fase Scansione.
/// Aggrega per operatore_scan e data_scansione con COUNT DISTINCT documenti.
/// </summary>
[Description("HERA16 Scansione - query diretta su tabella HERA16")]
public sealed class Hera16ScansioneHandler : Hera16QueryHandlerBase
{
    public Hera16ScansioneHandler(
        ILogger<Hera16ScansioneHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_SCANSIONE;

    protected override async Task<(List<DatiLavorazione> Dati, List<int> Ids)> QueryHera16Async(
        DematReportsContext db,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var start = startDate.Date;
        var end   = endDate.Date.AddDays(1);

        var rows = await db.DatiMailCsvHera16
            .Where(h => h.DataScansione.HasValue
                     && h.DataScansione >= start
                     && h.DataScansione < end)
            .Select(h => new
            {
                h.IdCounter,
                h.OperatoreScan,
                h.DataScansione,
                h.CodiceMercato,
                h.CodiceOfferta,
                h.TipoDocumento
            })
            .ToListAsync(ct);

        var ids  = rows.Select(r => r.IdCounter).ToList();
        var dati = AggregaPerOperatore(
            rows,
            r => r.OperatoreScan,
            r => r.DataScansione!.Value.Date,
            r => (r.CodiceMercato ?? "") + (r.CodiceOfferta ?? "") + (r.TipoDocumento ?? ""));

        return (dati, ids);
    }
}

/// <summary>
/// Handler HERA16 per fase Index.
/// Esclude operatori '-'/'engine' e tipi documento 'BRIT','DR01','XXXX'.
/// </summary>
[Description("HERA16 Index - query diretta su tabella HERA16")]
public sealed class Hera16IndexHandler : Hera16QueryHandlerBase
{
    private static readonly string[] TipiEsclusi = ["BRIT", "DR01", "XXXX"];

    public Hera16IndexHandler(
        ILogger<Hera16IndexHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_INDEX;

    protected override async Task<(List<DatiLavorazione> Dati, List<int> Ids)> QueryHera16Async(
        DematReportsContext db,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var start = startDate.Date;
        var end   = endDate.Date.AddDays(1);

        var rows = await db.DatiMailCsvHera16
            .Where(h => h.DataIndex.HasValue
                     && h.DataIndex >= start
                     && h.DataIndex < end
                     && h.OperatoreIndex != "-"
                     && h.OperatoreIndex != "engine"
                     && !TipiEsclusi.Contains(h.TipoDocumento))
            .Select(h => new
            {
                h.IdCounter,
                h.OperatoreIndex,
                h.DataIndex,
                h.CodiceMercato,
                h.CodiceOfferta,
                h.TipoDocumento
            })
            .ToListAsync(ct);

        var ids  = rows.Select(r => r.IdCounter).ToList();
        var dati = AggregaPerOperatore(
            rows,
            r => r.OperatoreIndex,
            r => r.DataIndex!.Value.Date,
            r => (r.CodiceMercato ?? "") + (r.CodiceOfferta ?? "") + (r.TipoDocumento ?? ""));

        return (dati, ids);
    }
}

/// <summary>
/// Handler HERA16 per fase Classificazione.
/// Esclude operatori '-'/'engine'.
/// </summary>
[Description("HERA16 Classificazione - query diretta su tabella HERA16")]
public sealed class Hera16ClassificazioneHandler : Hera16QueryHandlerBase
{
    public Hera16ClassificazioneHandler(
        ILogger<Hera16ClassificazioneHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_CLASSIFICAZIONE;

    protected override async Task<(List<DatiLavorazione> Dati, List<int> Ids)> QueryHera16Async(
        DematReportsContext db,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var start = startDate.Date;
        var end   = endDate.Date.AddDays(1);

        var rows = await db.DatiMailCsvHera16
            .Where(h => h.DataClassificazione.HasValue
                     && h.DataClassificazione >= start
                     && h.DataClassificazione < end
                     && h.OperatoreClassificazione != "-"
                     && h.OperatoreClassificazione != "engine")
            .Select(h => new
            {
                h.IdCounter,
                h.OperatoreClassificazione,
                h.DataClassificazione,
                h.CodiceMercato,
                h.CodiceOfferta,
                h.TipoDocumento
            })
            .ToListAsync(ct);

        var ids  = rows.Select(r => r.IdCounter).ToList();
        var dati = AggregaPerOperatore(
            rows,
            r => r.OperatoreClassificazione,
            r => r.DataClassificazione!.Value.Date,
            r => (r.CodiceMercato ?? "") + (r.CodiceOfferta ?? "") + (r.TipoDocumento ?? ""));

        return (dati, ids);
    }
}

