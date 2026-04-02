using BlazorDematReports.Core.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;

/// <summary>
/// Implementazione mock di <see cref="Hera16EmailService"/> per ambienti di sviluppo/test.
/// Sostituisce la connessione Exchange EWS con la lettura diretta di file CSV
/// da una cartella locale configurabile, permettendo il test del parsing HERA16
/// e del bulk insert nella tabella HERA16 senza accesso alla posta aziendale.
///
/// Attivazione: impostare "MailServices:HERA16:UseMockService": true in appsettings.Development.json.
/// Cartella default: {BaseDirectory}/TestData/HERA16/  (override con "MailServices:HERA16:MockDataPath")
///
/// Formato file CSV atteso: HERA16_Report_YYYYMMDD.csv con colonne
/// OperatoreScansione, OperatoreIndex, OperatoreClassificazione (una riga per documento).
/// </summary>
public sealed class LocalCsvHera16EmailService : Hera16EmailService, IEmailBatchProcessor
{
    private readonly ILogger<LocalCsvHera16EmailService> _localLogger;
    private readonly string _mockDataPath;

    /// <summary>
    /// Inizializza il servizio mock HERA16.
    /// Usa <see cref="ILoggerFactory"/> per creare logger tipizzati sia per la classe base
    /// (<see cref="Hera16EmailService"/>) che per questa classe derivata.
    /// </summary>
    public LocalCsvHera16EmailService(IConfiguration configuration, ILoggerFactory loggerFactory)
        : base(configuration, loggerFactory.CreateLogger<Hera16EmailService>())
    {
        _localLogger = loggerFactory.CreateLogger<LocalCsvHera16EmailService>();
        _mockDataPath = configuration["MailServices:HERA16:MockDataPath"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "HERA16");
    }

    /// <summary>
    /// Legge tutti i file *.csv dalla cartella locale configurata,
    /// processa quelli che corrispondono ai pattern HERA16 e costruisce
    /// un <see cref="BatchEmailProcessingResult"/> identico a quello prodotto dal servizio reale.
    /// I metadati <c>DataLavorazione</c> e <c>IdEvento</c> vengono impostati alla data di ieri
    /// (periodo ingestion standard per HERA16).
    /// </summary>
    public override async Task<BatchEmailProcessingResult> ProcessEmailsAsync(CancellationToken ct = default)
    {
        _localLogger.LogInformation("Mock HERA16: lettura CSV da cartella locale {Path}", _mockDataPath);

        RawCsvAttachments_Clear();

        if (!Directory.Exists(_mockDataPath))
        {
            _localLogger.LogWarning(
                "Mock HERA16: cartella {Path} non trovata. Crearla e aggiungere file CSV di test",
                _mockDataPath);
            return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
        }

        var csvFiles = Directory.GetFiles(_mockDataPath, "*.csv");
        if (csvFiles.Length == 0)
        {
            _localLogger.LogInformation("Mock HERA16: nessun file CSV trovato in {Path}", _mockDataPath);
            return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
        }

        // Metadata condivisi: usa la data di ieri (periodo ingestion standard)
        var dataRif = DateTime.Today.AddDays(-1);
        var metadata = new Dictionary<string, string>
        {
            ["DataLavorazione"] = dataRif.ToString("yyyy-MM-dd"),
            ["IdEvento"]        = $"MOCK-HERA16-{dataRif:yyyyMMdd}"
        };

        var processedAttachments = new List<AttachmentInfo>();

        foreach (var csvFile in csvFiles)
        {
            ct.ThrowIfCancellationRequested();

            var fileName       = Path.GetFileName(csvFile);
            var matchesPattern = MatchesAnyPattern(fileName);
            var content        = await File.ReadAllBytesAsync(csvFile, ct);

            var attachmentInfo = new AttachmentInfo
            {
                FileName       = fileName,
                Content        = content,
                MatchesPattern = matchesPattern,
                ContentType    = "text/csv"
            };

            if (!matchesPattern)
                _localLogger.LogDebug("Mock HERA16: file {FileName} non corrisponde al pattern HERA16, elaborato comunque in modalita mock", fileName);

            _localLogger.LogInformation("Mock HERA16: elaborazione file {FileName}", fileName);
            await ProcessAttachmentAsync(attachmentInfo, metadata, ct);
            processedAttachments.Add(attachmentInfo);
        }

        _localLogger.LogInformation(
            "Mock HERA16: batch completato - {FileCount} file processati, {Righe} righe CSV totali",
            processedAttachments.Count, RawCsvAttachments.Sum(a => a.Data.Rows.Count));

        var emailResult = new EmailProcessingResult
        {
            Subject           = $"[MOCK] HERA16 - {dataRif:yyyy-MM-dd}",
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
