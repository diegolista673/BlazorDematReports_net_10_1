using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Handlers.MailHandlers;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;

/// <summary>
/// Processore ingestion HERA16.
/// Legge email con allegati CSV tramite <see cref="Hera16EmailService"/>
/// e inserisce le righe grezze nella tabella <c>HERA16</c> via <see cref="IHera16DataService"/>.
/// I dati vengono poi letti dagli handler produzione
/// (<see cref="Hera16ScansioneHandler"/>, <see cref="Hera16IndexHandler"/>, <see cref="Hera16ClassificazioneHandler"/>)
/// tramite query SQL aggregate.
/// </summary>
public sealed class Hera16IngestionProcessor : IMailIngestionProcessor
{
    private readonly ILogger<Hera16IngestionProcessor> _logger;
    private readonly Hera16EmailService _emailService;
    private readonly IHera16DataService _hera16DataService;

    public Hera16IngestionProcessor(
        ILogger<Hera16IngestionProcessor> logger,
        Hera16EmailService emailService,
        IHera16DataService hera16DataService)
    {
        _logger           = logger;
        _emailService     = emailService;
        _hera16DataService = hera16DataService;
    }

    /// <inheritdoc />
    public string ServiceCode => LavorazioniCodes.HERA16;

    /// <inheritdoc />
    public async Task<IngestionResult> ProcessAndSaveAsync(CancellationToken ct)
    {
        _logger.LogInformation("Inizio ingestion HERA16");

        var emailResults = await _emailService.ProcessEmailsAsync(ct);

        if (emailResults.TotalEmailsFound == 0)
        {
            _logger.LogInformation("HERA16: nessuna email da processare");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = 0 };
        }

        // Inserimento righe grezze nella tabella HERA16
        var rawAttachments = _emailService.RawCsvAttachments;
        if (rawAttachments.Count > 0)
        {
            await _hera16DataService.BulkInsertAsync(rawAttachments, ct);
            _logger.LogInformation(
                "HERA16: {Count} allegati CSV inseriti in tabella HERA16",
                rawAttachments.Count);
        }

        _logger.LogInformation(
            "HERA16 ingestion completata: {Emails} email, {Raw} allegati grezzi",
            emailResults.TotalEmailsFound, rawAttachments.Count);

        return new IngestionResult
        {
            RecordsSaved    = rawAttachments.Sum(a => a.Data.Rows.Count),
            EmailsProcessed = emailResults.TotalEmailsFound
        };
    }
}

