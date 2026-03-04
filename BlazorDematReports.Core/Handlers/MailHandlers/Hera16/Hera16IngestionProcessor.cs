using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Handlers.MailHandlers;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16;

/// <summary>
/// Processore ingestion HERA16.
/// Legge email con allegati CSV tramite <see cref="Hera16EmailService"/>, ottiene le righe
/// per-operatore (GROUP BY OperatoreScansione / OperatoreIndex / OperatoreClassificazione)
/// e salva in bulk nella tabella <c>DatiMailCsv</c>.
/// </summary>
public sealed class Hera16IngestionProcessor : IMailIngestionProcessor
{
    private readonly ILogger<Hera16IngestionProcessor> _logger;
    private readonly Hera16EmailService _emailService;

    public Hera16IngestionProcessor(
        ILogger<Hera16IngestionProcessor> logger,
        Hera16EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    /// <inheritdoc />
    public string ServiceCode => LavorazioniCodes.HERA16;

    /// <inheritdoc />
    public async Task<IngestionResult> ProcessAndSaveAsync(
        IMailCsvService mailCsvService,
        CancellationToken ct)
    {
        _logger.LogInformation("Inizio ingestion HERA16");

        var emailResults = await _emailService.ProcessEmailsAsync(ct);

        if (emailResults.TotalEmailsFound == 0)
        {
            _logger.LogInformation("HERA16: nessuna email da processare");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = 0 };
        }

        var righe = _emailService.RigheElaborate;

        if (righe.Count == 0)
        {
            _logger.LogWarning("HERA16: email processate ma nessuna riga CSV estratta");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = emailResults.TotalEmailsFound };
        }

        await mailCsvService.UpsertBulkAsync(righe, ct);

        _logger.LogInformation(
            "HERA16 ingestion completata: {Emails} email processate, {Records} righe salvate",
            emailResults.TotalEmailsFound, righe.Count);

        return new IngestionResult
        {
            RecordsSaved    = righe.Count,
            EmailsProcessed = emailResults.TotalEmailsFound
        };
    }
}
