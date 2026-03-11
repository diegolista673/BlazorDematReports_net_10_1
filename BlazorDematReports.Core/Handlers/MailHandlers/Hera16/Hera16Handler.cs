using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16;

/// <summary>
/// Handler base per task produzione HERA16 che leggono righe per-operatore da DatiMailCsv.
/// Ogni implementazione specifica quale TipoRisultato leggere (Scansione, Index, Classificazione).
/// L'operatore e' preso direttamente dal record staging (OperatoreScan/Index/Classificazione).
/// </summary>
public abstract class Hera16StagingHandlerBase : IProductionDataHandler
{
    protected readonly ILogger Logger;
    protected readonly IServiceScopeFactory ScopeFactory;

    protected Hera16StagingHandlerBase(ILogger logger, IServiceScopeFactory scopeFactory)
    {
        Logger = logger;
        ScopeFactory = scopeFactory;
    }

    /// <summary>TipoRisultato da leggere in DatiMailCsv (es. 'Scansione', 'Index', 'Classificazione').</summary>
    protected abstract string TipoRisultatoStaging { get; }

    /// <summary>Calcola Fogli da Documenti. Default: Documenti / 2.</summary>
    protected virtual int CalcolaFogli(int documenti) => documenti / 2;

    /// <inheritdoc />
    public abstract string HandlerCode { get; }

    /// <inheritdoc />
    public string? GetServiceCode() => "";

    /// <inheritdoc />
    public HandlerMetadata GetMetadata() => new()
    {
        ServiceCode = LavorazioniCodes.HERA16,
        RequiresEmailService = false,
        Category = "Staging Reader"
    };

    /// <inheritdoc />
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        ProductionExecutionContext context,
        CancellationToken ct = default)
    {
        Logger.LogInformation(
            "Handler {Code}: lettura staging {TipoRisultato}, periodo {Start:d}-{End:d}",
            HandlerCode, TipoRisultatoStaging,
            context.StartDataLavorazione,
            context.EndDataLavorazione ?? context.StartDataLavorazione);

        var dataMin = DateOnly.FromDateTime(context.StartDataLavorazione);
        var dataMax = context.EndDataLavorazione.HasValue
            ? DateOnly.FromDateTime(context.EndDataLavorazione.Value)
            : DateOnly.FromDateTime(context.StartDataLavorazione);

        using var scope = ScopeFactory.CreateScope();
        var mailCsvService = scope.ServiceProvider.GetRequiredService<IMailCsvService>();

        var stagingRecords = await mailCsvService.GetUnprocessedAsync(
            LavorazioniCodes.HERA16,
            TipoRisultatoStaging,
            dataMin,
            dataMax,
            centro: null,
            ct);

        if (stagingRecords.Count == 0)
        {
            Logger.LogInformation("Nessun dato staging HERA16 per {TipoRisultato}", TipoRisultatoStaging);
            return [];
        }

        // Operatore reale: OperatoreScan/Index/Classificazione dalla tabella Hera16
        var datiLavorazione = stagingRecords.Select(s => new DatiLavorazione
        {
            Operatore                     = s.Operatore,
            DataLavorazione               = s.DataLavorazione.ToDateTime(TimeOnly.MinValue),
            Documenti                     = s.Documenti,
            Fogli                         = CalcolaFogli(s.Documenti),
            Pagine                        = s.Documenti,
            AppartieneAlCentroSelezionato = true
        }).ToList();

        var ids = stagingRecords.Select(s => s.Id).ToList();
        await mailCsvService.MarkAsProcessedAsync(ids, ct);

        Logger.LogInformation(
            "Handler {Code}: {Count} record letti da DatiMailCsv e marcati come elaborati",
            HandlerCode, datiLavorazione.Count);

        return datiLavorazione;
    }
}

/// <summary>Handler HERA16 per fase Scansione.</summary>
[Description("HERA16 Scansione - legge staging tipo 'Scansione'")]
public sealed class Hera16ScansioneHandler : Hera16StagingHandlerBase
{
    public Hera16ScansioneHandler(ILogger<Hera16ScansioneHandler> logger, IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_SCANSIONE;
    protected override string TipoRisultatoStaging => "Scansione";
}

/// <summary>Handler HERA16 per fase Index.</summary>
[Description("HERA16 Index - legge staging tipo 'Index'")]
public sealed class Hera16IndexHandler : Hera16StagingHandlerBase
{
    public Hera16IndexHandler(ILogger<Hera16IndexHandler> logger, IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_INDEX;
    protected override string TipoRisultatoStaging => "Index";
}

/// <summary>Handler HERA16 per fase Classificazione.</summary>
[Description("HERA16 Classificazione - legge staging tipo 'Classificazione'")]
public sealed class Hera16ClassificazioneHandler : Hera16StagingHandlerBase
{
    public Hera16ClassificazioneHandler(ILogger<Hera16ClassificazioneHandler> logger, IServiceScopeFactory scopeFactory)
        : base(logger, scopeFactory) { }

    public override string HandlerCode => LavorazioniCodes.HERA16_CLASSIFICAZIONE;
    protected override string TipoRisultatoStaging => "Classificazione";
}
