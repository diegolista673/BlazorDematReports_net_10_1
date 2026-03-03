using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.Handlers.MailHandlers;
using BlazorDematReports.Core.Services.Email;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4;

/// <summary>
/// Processore ingestion specifico per ADER4.
/// Legge email ADER4 via IEmailBatchProcessor e salva dati aggregati in DatiMailIngestion.
/// </summary>
public sealed class Ader4IngestionProcessor : IMailIngestionProcessor
{
    private readonly ILogger<Ader4IngestionProcessor> _logger;
    private readonly IEmailBatchProcessor _emailService;

    public Ader4IngestionProcessor(
        ILogger<Ader4IngestionProcessor> logger,
        IEmailBatchProcessor emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public string ServiceCode => LavorazioniCodes.ADER4;

    /// <summary>
    /// Legge email ADER4, estrae metadata e salva in staging.
    /// Mapping dati ADER4:
    /// - ScansioneCaptiva   → metadata["ScansioneCaptiva"]
    /// - ScansioneSorter    → metadata["ScansioneSorter"]
    /// - ScansioneSorterBuste → metadata["ScansioneSorterBuste"]
    /// - PreAccettazione    → metadata["PreAccettazione"]
    /// - Ripartizione       → metadata["Ripartizione"]
    /// - Restituzione       → metadata["Restituzione"]
    /// </summary>
    public async Task<IngestionResult> ProcessAndSaveAsync(
        IMailIngestionService ingestionService,
        CancellationToken ct)
    {
        _logger.LogInformation("Inizio ingestion ADER4");

        var emailResults = await _emailService.ProcessEmailsAsync(ct);

        if (emailResults.TotalEmailsFound == 0)
        {
            _logger.LogInformation("ADER4: nessuna email da processare");
            return new IngestionResult { RecordsSaved = 0, EmailsProcessed = 0 };
        }

        int recordsSaved = 0;

        foreach (var email in emailResults.SuccessfulEmails)
        {
            if (email.ExtractedMetadata == null || !email.ExtractedMetadata.TryGetValue("DataRiferimento", out var dataRifStr))
            {
                _logger.LogWarning("ADER4: email {Subject} senza DataRiferimento nei metadata", email.Subject);
                continue;
            }

            if (!DateOnly.TryParse(dataRifStr, out var dataRif))
            {
                _logger.LogWarning("ADER4: DataRiferimento non valida: {Data}", dataRifStr);
                continue;
            }

            var metadata = email.ExtractedMetadata;

            // Salva ogni tipo di dato come record separato (se quantità > 0)
            if (TryGetInt(metadata, "ScansioneCaptiva", out var captiva) && captiva > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "ScansioneCaptiva", null, captiva);
                recordsSaved++;
            }

            if (TryGetInt(metadata, "ScansioneSorter", out var sorter) && sorter > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "ScansioneSorter", null, sorter);
                recordsSaved++;
            }

            if (TryGetInt(metadata, "ScansioneSorterBuste", out var sorterBuste) && sorterBuste > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "ScansioneSorterBuste", null, sorterBuste);
                recordsSaved++;
            }

            if (TryGetInt(metadata, "PreAccettazione", out var preAcc) && preAcc > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "PreAccettazione", null, preAcc);
                recordsSaved++;
            }

            if (TryGetInt(metadata, "Ripartizione", out var ripart) && ripart > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "Ripartizione", null, ripart);
                recordsSaved++;
            }

            if (TryGetInt(metadata, "Restituzione", out var restit) && restit > 0)
            {
                await ingestionService.UpsertAsync(
                    LavorazioniCodes.ADER4, dataRif, "Restituzione", null, restit);
                recordsSaved++;
            }
        }

        _logger.LogInformation(
            "ADER4 ingestion completata: {Emails} email processate, {Records} record salvati",
            emailResults.SuccessfulEmails.Count, recordsSaved);

        return new IngestionResult
        {
            RecordsSaved = recordsSaved,
            EmailsProcessed = emailResults.SuccessfulEmails.Count
        };
    }

    private static bool TryGetInt(Dictionary<string, string> dict, string key, out int value)
    {
        value = 0;
        return dict.TryGetValue(key, out var str) && int.TryParse(str, out value);
    }
}
