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
    /// Handler base per task produzione ADER4 che leggono dati da staging DatiMailIngestion.
    /// Ogni implementazione specifica quale TipoDato leggere (ScansioneCaptiva, ScansioneSorter, etc.).
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

        /// <summary>
        /// Tipo di dato che questo handler deve leggere dallo staging (override nelle classi derivate).
        /// </summary>
        protected abstract string TipoDatoStaging { get; }

        /// <summary>
        /// Nome operatore da usare nei DatiLavorazione generati (override nelle classi derivate).
        /// </summary>
        protected abstract string NomeOperatore { get; }

        /// <summary>
        /// Calcola Fogli da Documenti (override se logica diversa).
        /// Default: Documenti / 2.
        /// </summary>
        protected virtual int CalcolaFogli(int documenti) => documenti / 2;

        /// <inheritdoc />
        public abstract string HandlerCode { get; }

        /// <inheritdoc />
        public string? GetServiceCode() => LavorazioniCodes.ADER4;

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
                "Handler {Code}: lettura staging per {TipoDato}, periodo {Start:d}-{End:d}",
                HandlerCode,
                TipoDatoStaging,
                context.StartDataLavorazione,
                context.EndDataLavorazione ?? context.StartDataLavorazione);

            var dataMin = DateOnly.FromDateTime(context.StartDataLavorazione);
            var dataMax = context.EndDataLavorazione.HasValue
                ? DateOnly.FromDateTime(context.EndDataLavorazione.Value)
                : DateOnly.FromDateTime(context.StartDataLavorazione);

            using var scope = ScopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IMailIngestionService>();

            var stagingRecords = await ingestionService.GetUnprocessedAsync(
                LavorazioniCodes.ADER4,
                TipoDatoStaging,
                centro: null,
                dataMin,
                dataMax);

            if (stagingRecords.Count == 0)
            {
                Logger.LogInformation("Nessun dato staging per {TipoDato}", TipoDatoStaging);
                return new List<DatiLavorazione>();
            }

            var datiLavorazione = stagingRecords.Select(s => new DatiLavorazione
            {
                Operatore = NomeOperatore,
                DataLavorazione = s.DataRiferimento.ToDateTime(TimeOnly.MinValue),
                Documenti = s.Quantita,
                Fogli = CalcolaFogli(s.Quantita),
                Pagine = s.Quantita,
                AppartieneAlCentroSelezionato = true
            }).ToList();

            var ids = stagingRecords.Select(s => s.Id).ToList();
            await ingestionService.MarkAsProcessedAsync(ids, 0);

            Logger.LogInformation(
                "Handler {Code}: {Count} record letti dallo staging e marcati come elaborati",
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

        public override string HandlerCode => "ADER4_CAPTIVA";
        protected override string TipoDatoStaging => "ScansioneCaptiva";
        protected override string NomeOperatore => "SISTEMA";
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

        public override string HandlerCode => "ADER4_SORTER";
        protected override string TipoDatoStaging => "ScansioneSorter";
        protected override string NomeOperatore => "SISTEMA_SORTER";
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

        public override string HandlerCode => "ADER4_SORTER_BUSTE";
        protected override string TipoDatoStaging => "ScansioneSorterBuste";
        protected override string NomeOperatore => "SISTEMA_SORTER_BUSTE";
        protected override int CalcolaFogli(int documenti) => documenti; // 1 busta = 1 foglio
    }
}
