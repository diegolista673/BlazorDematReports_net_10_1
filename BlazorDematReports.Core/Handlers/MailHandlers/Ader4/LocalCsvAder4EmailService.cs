using BlazorDematReports.Core.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4;

/// <summary>
/// Implementazione mock di <see cref="IEmailBatchProcessor"/> per ADER4.
/// Sostituisce la connessione Exchange EWS con la lettura diretta di file CSV
/// da una cartella locale configurabile, permettendo il test del parsing e della
/// pipeline di produzione senza accesso alla posta aziendale.
///
/// Attivazione: impostare "MailServices:ADER4:UseMockService": true in appsettings.Development.json.
/// Cartella default: {BaseDirectory}/TestData/ADER4/  (override con "MailServices:ADER4:MockDataPath")
/// </summary>
public sealed class LocalCsvAder4EmailService : Ader4EmailService, IEmailBatchProcessor
{
    private readonly ILogger<LocalCsvAder4EmailService> _localLogger;
    private readonly string _mockDataPath;

    /// <summary>
    /// Inizializza il servizio mock.
    /// Usa <see cref="ILoggerFactory"/> per creare logger tipizzati sia per la classe base
    /// (<see cref="Ader4EmailService"/>) che per questa classe derivata.
    /// </summary>
    public LocalCsvAder4EmailService(IConfiguration configuration, ILoggerFactory loggerFactory)
        : base(configuration, loggerFactory.CreateLogger<Ader4EmailService>())
    {
        _localLogger = loggerFactory.CreateLogger<LocalCsvAder4EmailService>();
        _mockDataPath = configuration["MailServices:ADER4:MockDataPath"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ADER4");
    }

    /// <summary>
    /// Legge tutti i file *.csv dalla cartella locale configurata,
    /// processa quelli che corrispondono ai pattern ADER4 e costruisce
    /// un <see cref="BatchEmailProcessingResult"/> identico a quello prodotto dal servizio reale.
    /// I metadati <c>DataLavorazione</c> e <c>IdEvento</c> vengono impostati alla data odierna.
    /// </summary>
    public override async Task<BatchEmailProcessingResult> ProcessEmailsAsync(CancellationToken ct = default)
    {
        _localLogger.LogInformation("Mock ADER4: lettura CSV da cartella locale {Path}", _mockDataPath);

        RigheElaborate_Clear();

        if (!Directory.Exists(_mockDataPath))
        {
            _localLogger.LogWarning(
                "Mock ADER4: cartella {Path} non trovata. Crearla e aggiungere file CSV di test",
                _mockDataPath);
            return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
        }

        var csvFiles = Directory.GetFiles(_mockDataPath, "*.csv");
        if (csvFiles.Length == 0)
        {
            _localLogger.LogInformation("Mock ADER4: nessun file CSV trovato in {Path}", _mockDataPath);
            return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
        }

        // Metadata condivisi per tutti gli allegati della stessa "email simulata"
        var metadata = new Dictionary<string, string>
        {
            ["DataLavorazione"] = DateTime.Today.ToString("yyyy-MM-dd"),
            ["IdEvento"]        = $"MOCK-{DateTime.Today:yyyyMMdd}"
        };

        var processedAttachments = new List<AttachmentInfo>();

        foreach (var csvFile in csvFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName    = Path.GetFileName(csvFile);
            var matchesPattern = MatchesAnyPattern(fileName);

            var attachmentInfo = new AttachmentInfo
            {
                FileName      = fileName,
                LocalFilePath = csvFile,
                FileSizeBytes = new FileInfo(csvFile).Length,
                MatchesPattern = matchesPattern,
                ContentType   = "text/csv"
            };

            if (!matchesPattern)
            {
                _localLogger.LogDebug(
                    "Mock ADER4: file {FileName} ignorato (nessun pattern corrisponde)", fileName);
                continue;
            }

            _localLogger.LogInformation("Mock ADER4: elaborazione file {FileName}", fileName);
            await ProcessAttachmentAsync(attachmentInfo, metadata, ct);
            processedAttachments.Add(attachmentInfo);
        }

        _localLogger.LogInformation(
            "Mock ADER4: batch completato - {FileCount} file processati - metadata: {Metadata}",
            processedAttachments.Count,
            string.Join(", ", metadata.Select(kv => $"{kv.Key}={kv.Value}")));

        var emailResult = new EmailProcessingResult
        {
            Subject           = $"[MOCK] ADER4 - {DateTime.Today:yyyy-MM-dd}",
            ReceivedDate      = DateTime.Now,
            Success           = true,
            Attachments       = processedAttachments,
            ExtractedMetadata = metadata
        };

        return new BatchEmailProcessingResult
        {
            TotalEmailsFound = 1,
            SuccessfulEmails = [emailResult]
        };
    }
}
