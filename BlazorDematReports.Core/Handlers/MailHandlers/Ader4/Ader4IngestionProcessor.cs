using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Handlers.MailHandlers;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4;

/// <summary>
/// Processore ingestion ADER4.
/// Legge email via Ader4EmailService, ottiene righe per-operatore (GROUP BY postazione)
/// e salva in bulk nella tabella DatiMailCsv via <see cref="IMailCsvService"/>.
/// IMailCsvService e' Scoped: viene risolto per-invocazione tramite uno scope temporaneo
/// per evitare la captive dependency (Singleton che consuma un servizio Scoped).
/// </summary>
public sealed class Ader4IngestionProcessor : IMailIngestionProcessor
{
    private readonly ILogger<Ader4IngestionProcessor> _logger;
    private readonly Ader4EmailService _emailService;
    // IServiceScopeFactory e' sempre Singleton-safe e permette di risolvere
    // servizi Scoped (come IMailCsvService) su richiesta, senza captive dependency.
    private readonly IServiceScopeFactory _scopeFactory;

    public Ader4IngestionProcessor(
        ILogger<Ader4IngestionProcessor> logger,
        Ader4EmailService emailService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _emailService = emailService;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public string ServiceCode => LavorazioniCodes.ADER4;

    /// <inheritdoc />
    public async Task<IngestionResult> ProcessAndSaveAsync(CancellationToken ct)
    {
        _logger.LogInformation("Inizio ingestion ADER4");

        var emailResults = await _emailService.ProcessEmailsAsync(ct);

        if (emailResults.TotalEmailsFound == 0)
        {
            _logger.LogInformation("ADER4: nessuna email da processare");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = 0 };
        }

        var righe = _emailService.RigheElaborate;

        if (righe.Count == 0)
        {
            _logger.LogWarning("ADER4: email processate ma nessuna riga CSV estratta");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = emailResults.TotalEmailsFound };
        }

        // Crea uno scope temporaneo per risolvere IMailCsvService (Scoped) da un contesto Singleton
        await using var scope = _scopeFactory.CreateAsyncScope();
        var mailCsvService = scope.ServiceProvider.GetRequiredService<IAder4MailCsvService>();
        await mailCsvService.UpsertBulkAsync(righe, ct);

        _logger.LogInformation(
            "ADER4 ingestion completata: {Emails} email processate, {Records} righe salvate",
            emailResults.TotalEmailsFound, righe.Count);

        return new IngestionResult
        {
            RecordsSaved    = righe.Count,
            EmailsProcessed = emailResults.TotalEmailsFound
        };
    }
}
