using BlazorDematReports.Core.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.Handlers.MailHandlers.DatiMailCsvHera16;

/// <summary>
/// Servizio email per HERA16.
/// Legge allegati CSV dall'email di report giornaliero HERA16 via Exchange EWS
/// e li espone come DataTable grezzi in <see cref="RawCsvAttachments"/> per il
/// bulk insert nella tabella HERA16 tramite <see cref="Hera16DataService"/>.
/// I parametri di connessione e i pattern di filtro sono configurati in appsettings.json
/// i dati di produzione vengono poi letti direttamente dagli handler produzione tramite query SQL aggregate sulla tabella HERA16.
/// va fatta una distinct perchè i file ricevuti contengono righe duplicate perchè le informazioni 
/// di scansione, indicizzazione e classificazione sono presenti in file separati ma con righe identiche per ogni documento (stesso operatore, stessa data, stesso id evento).
/// </summary>
public class Hera16EmailService : BaseEwsEmailService, IEmailBatchProcessor
{
    private readonly List<(DataTable Data, string FileName)> _rawAttachments = [];

    /// <summary>Allegati CSV grezzi (una voce per file) pronti per il bulk insert in HERA16.</summary>
    public IReadOnlyList<(DataTable Data, string FileName)> RawCsvAttachments => _rawAttachments;

    /// <summary>Svuota la lista allegati grezzi prima di una nuova elaborazione.</summary>
    protected void RawCsvAttachments_Clear() => _rawAttachments.Clear();

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="Hera16EmailService"/>.
    /// </summary>
    public Hera16EmailService(IConfiguration configuration, ILogger<Hera16EmailService> logger)
        : base(CreateConfig(configuration), logger)
    {
    }

    /// <summary>
    /// Crea configurazione EWS da appsettings.json per HERA16.
    /// </summary>
    private static EwsEmailServiceConfig CreateConfig(IConfiguration configuration)
    {
        var mailConfig = configuration.GetSection("MailServices:HERA16");

        return new EwsEmailServiceConfig
        {
            Username            = mailConfig["Username"] ?? throw new InvalidOperationException("HERA16 Username mancante"),
            Password            = mailConfig["Password"] ?? throw new InvalidOperationException("HERA16 Password mancante"),
            Domain              = mailConfig["Domain"] ?? "postel.it",
            ExchangeUrl         = new Uri(mailConfig["ExchangeUrl"] ?? "https://webmail.postel.it/ews/exchange.asmx"),
            SubjectFilters      = [mailConfig["SubjectFilter"] ?? "HERA16 - Report di produzione"],
            AttachmentPatterns  = ["HERA16_Report*", "HERA16_Produzione*"],
            ArchiveFolderName   = mailConfig["ArchiveFolder"] ?? "HERA16",
            MaxEmailsPerRun     = 100
        };
    }

    /// <summary>
    /// Legge il CSV allegato e lo accoda in <see cref="RawCsvAttachments"/>
    /// per il successivo bulk insert nella tabella HERA16.
    /// </summary>
    protected override async Task ProcessAttachmentAsync(
        AttachmentInfo attachment,
        Dictionary<string, string> metadata,
        CancellationToken ct)
    {
        Logger.LogInformation("Lettura allegato HERA16: {FileName}", attachment.FileName);

        if (attachment.Content.Length == 0)
        {
            Logger.LogWarning("File CSV HERA16 {FileName} vuoto (0 byte), ignorato", attachment.FileName);
            return;
        }

        var csvData = ReadCsvFromBytes(attachment.Content);

        if (csvData.Rows.Count == 0)
        {
            Logger.LogWarning("File CSV HERA16 {FileName} senza righe, ignorato", attachment.FileName);
            return;
        }

        _rawAttachments.Add((csvData, attachment.FileName));

        Logger.LogInformation(
            "Allegato HERA16 {FileName}: {RowCount} righe accodate per bulk insert",
            attachment.FileName, csvData.Rows.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Estrae metadata specifici HERA16 dal body email.
    /// </summary>
    protected override void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
    {
        base.ExtractMetadataFromBody(bodyText, metadata);

        ExtractMetadataField(bodyText, "Data lavorazione:", metadata, "DataLavorazione");
        ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
    }
}

