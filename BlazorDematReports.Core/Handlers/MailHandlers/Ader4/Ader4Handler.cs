using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4
{
    /// <summary>
    /// Handler base per task produzione ADER4 che leggono righe per-operatore da DatiMailCsv.
    /// Ogni implementazione specifica quale TipoRisultato leggere.
    /// L'operatore e' preso direttamente dal record staging (colonna 'postazione' del CSV).
    /// </summary>
    public abstract class Ader4StagingHandlerBase : IProductionDataHandler
    {
        protected readonly ILogger Logger;
        protected readonly IServiceScopeFactory ScopeFactory;

        protected Ader4StagingHandlerBase(
            ILogger logger,
            IServiceScopeFactory scopeFactory)
        {
            Logger = logger;
            ScopeFactory = scopeFactory;
        }

        /// <summary>TipoRisultato da leggere in DatiMailCsv (es. 'ScansioneCaptiva').</summary>
        protected abstract string TipoRisultatoStaging { get; }

        /// <summary>
        /// Calcola Fogli da Documenti. Default: Documenti / 2.
        /// </summary>
        protected virtual int CalcolaFogli(int documenti) => documenti / 2;

        /// <inheritdoc />
        public abstract string HandlerCode { get; }

        /// <inheritdoc />
        public string? GetServiceCode() => "";

        /// <inheritdoc />
        public HandlerMetadata GetMetadata() => new()
        {
            ServiceCode = LavorazioniCodes.ADER4,
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
                HandlerCode,
                TipoRisultatoStaging,
                context.StartDataLavorazione,
                context.EndDataLavorazione ?? context.StartDataLavorazione);

            var dataMin = DateOnly.FromDateTime(context.StartDataLavorazione);
            var dataMax = context.EndDataLavorazione.HasValue
                ? DateOnly.FromDateTime(context.EndDataLavorazione.Value)
                : DateOnly.FromDateTime(context.StartDataLavorazione);

            using var scope = ScopeFactory.CreateScope();
            var mailCsvService = scope.ServiceProvider.GetRequiredService<IMailCsvService>();

            var stagingRecords = await mailCsvService.GetUnprocessedAsync(
                LavorazioniCodes.ADER4,
                TipoRisultatoStaging,
                dataMin,
                dataMax,
                centro: null,
                ct);

            if (stagingRecords.Count == 0)
            {
                Logger.LogInformation("Nessun dato staging per {TipoRisultato}", TipoRisultatoStaging);
                return [];
            }

            // Operatore reale dal CSV (colonna 'postazione'), non piu hardcoded
            var datiLavorazione = stagingRecords.Select(s => new DatiLavorazione
            {
                Operatore               = s.Operatore,
                DataLavorazione         = s.DataLavorazione.ToDateTime(TimeOnly.MinValue),
                Documenti               = s.Documenti,
                Fogli                   = CalcolaFogli(s.Documenti),
                Pagine                  = s.Documenti,
                AppartieneAlCentroSelezionato = true
            }).ToList();

            var ids = stagingRecords.Select(s => s.Id).ToList();
            await mailCsvService.MarkAsProcessedAsync(ids, 0, ct);

            Logger.LogInformation(
                "Handler {Code}: {Count} record letti da DatiMailCsv e marcati come elaborati",
                HandlerCode, datiLavorazione.Count);

            return datiLavorazione;
        }
    }

    /// <summary>
    /// Handler per Scansione Captiva ADER4.
    /// </summary>
    [Description("ADER4 Captiva - legge staging ScansioneCaptiva")]
    public sealed class Ader4CaptivaHandler : Ader4StagingHandlerBase
    {
        public Ader4CaptivaHandler(
            ILogger<Ader4CaptivaHandler> logger,
            IServiceScopeFactory scopeFactory)
            : base(logger, scopeFactory)
        {
        }

        public override string HandlerCode => LavorazioniCodes.ADER4_CAPTIVA;
        protected override string TipoRisultatoStaging => "ScansioneCaptiva";
    }

    /// <summary>
    /// Handler per Scansione Sorter ADER4.
    /// </summary>
    [Description("ADER4 Sorter - legge staging ScansioneSorter")]
    public sealed class Ader4SorterHandler : Ader4StagingHandlerBase
    {
        public Ader4SorterHandler(
            ILogger<Ader4SorterHandler> logger,
            IServiceScopeFactory scopeFactory)
            : base(logger, scopeFactory)
        {
        }

        public override string HandlerCode => LavorazioniCodes.ADER4_SORTER;
        protected override string TipoRisultatoStaging => "ScansioneSorter";
    }

    /// <summary>
    /// Handler per Scansione Sorter Buste ADER4.
    /// </summary>
    [Description("ADER4 Sorter Buste - legge staging ScansioneSorterBuste")]
    public sealed class Ader4SorterBusteHandler : Ader4StagingHandlerBase
    {
        public Ader4SorterBusteHandler(
            ILogger<Ader4SorterBusteHandler> logger,
            IServiceScopeFactory scopeFactory)
            : base(logger, scopeFactory)
        {
        }

        public override string HandlerCode => LavorazioniCodes.ADER4_SORTER_BUSTE;
        protected override string TipoRisultatoStaging => "ScansioneSorterBuste";
        protected override int CalcolaFogli(int documenti) => documenti; // 1 busta = 1 foglio
    }
}
