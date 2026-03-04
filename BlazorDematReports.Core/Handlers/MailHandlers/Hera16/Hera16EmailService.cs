using BlazorDematReports.Core.Services.Email;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16;

/// <summary>
/// Servizio email per HERA16.
/// Legge allegati CSV dall'email di report giornaliero HERA16 via Exchange EWS,
/// raggruppa le righe per operatore (OperatoreScansione / OperatoreIndex / OperatoreClassificazione)
/// e popola <see cref="RigheElaborate"/> con <see cref="DatiMailCsvDto"/> per-operatore.
///
/// Formato CSV atteso (una riga = un documento elaborato):
/// <code>
/// DataLavorazione;OperatoreScansione;OperatoreIndex;OperatoreClassificazione;CodiceMercato;...
/// </code>
/// Le colonne Operatore* possono essere valorizzate indipendentemente l'una dall'altra.
/// </summary>
public class Hera16EmailService : BaseEwsEmailService, IEmailBatchProcessor
{
    private readonly IConfiguration _configuration;
    private readonly List<DatiMailCsvDto> _righeElaborate = [];

    /// <summary>Righe per-operatore estratte dall'ultima elaborazione email.</summary>
    public IReadOnlyList<DatiMailCsvDto> RigheElaborate => _righeElaborate;

    /// <summary>Svuota la lista righe prima di una nuova elaborazione.</summary>
    protected void RigheElaborate_Clear() => _righeElaborate.Clear();

    /// <summary>
    /// Inizializza una nuova istanza di <see cref="Hera16EmailService"/>.
    /// </summary>
    public Hera16EmailService(IConfiguration configuration, ILogger<Hera16EmailService> logger)
        : base(CreateConfig(configuration), logger)
    {
        _configuration = configuration;
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
            ExchangeUrl         = new Uri(mailConfig["ExchangeUrl"] ?? "https://postaweb.postel.it/ews/exchange.asmx"),
            SubjectFilters      = [mailConfig["SubjectFilter"] ?? "HERA16 - Report di produzione"],
            AttachmentPatterns  = ["HERA16_Report*", "HERA16_Produzione*"],
            ArchiveFolderName   = mailConfig["ArchiveFolder"] ?? "HERA16",
            LocalAttachmentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report", "HERA16"),
            LocalArchivePath    = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "HERA16"),
            MaxEmailsPerRun     = 100,
            CreateZipArchive    = true,
            CleanupAfterProcessing = true
        };
    }

    /// <summary>
    /// Processa allegato CSV HERA16.
    /// Ogni riga del CSV rappresenta un documento; le colonne operatore possono essere valorizzate
    /// indipendentemente. Aggruppa per operatore e accumula righe in <see cref="RigheElaborate"/>.
    /// </summary>
    protected override async Task ProcessAttachmentAsync(
        AttachmentInfo attachment,
        Dictionary<string, string> metadata,
        CancellationToken ct)
    {
        Logger.LogInformation("Elaborazione allegato HERA16: {FileName}", attachment.FileName);

        var csvData = ReadCsvFile(attachment.LocalFilePath);

        if (csvData.Rows.Count == 0)
        {
            Logger.LogWarning("File CSV HERA16 {FileName} vuoto", attachment.FileName);
            return;
        }

        metadata.TryGetValue("DataLavorazione", out var dataRifStr);
        metadata.TryGetValue("IdEvento", out var idEvento);
        metadata.TryGetValue("Centro", out var centro);

        if (!DateOnly.TryParse(dataRifStr, out var dataRif))
        {
            Logger.LogWarning("DataLavorazione non valida '{Data}' in {FileName}", dataRifStr, attachment.FileName);
            return;
        }

        AggiungiRigheHera16(csvData, dataRif, idEvento, centro);

        Logger.LogInformation(
            "Allegato HERA16 {FileName} processato: {RowCount} righe CSV, {Righe} DatiMailCsvDto aggiunti",
            attachment.FileName, csvData.Rows.Count, _righeElaborate.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Aggrega le righe CSV per le tre dimensioni operatore di HERA16.
    /// Una riga CSV con OperatoreScansione="MARIO" contribuisce a Scansione[MARIO]++, ecc.
    /// </summary>
    private void AggiungiRigheHera16(
        DataTable csvData,
        DateOnly dataRif,
        string? idEvento,
        string? centro)
    {
        // Scansione: GROUP BY OperatoreScansione, COUNT(*)
        AggiungiRighePerColonnaOperatore(csvData, dataRif, "OperatoreScansione", "Scansione", idEvento, centro);

        // Index: GROUP BY OperatoreIndex, COUNT(*)
        AggiungiRighePerColonnaOperatore(csvData, dataRif, "OperatoreIndex", "Index", idEvento, centro);

        // Classificazione: GROUP BY OperatoreClassificazione, COUNT(*)
        AggiungiRighePerColonnaOperatore(csvData, dataRif, "OperatoreClassificazione", "Classificazione", idEvento, centro);
    }

    /// <summary>
    /// Per una data colonna operatore, raggruppa le righe non nulle e aggiunge DatiMailCsvDto.
    /// </summary>
    private void AggiungiRighePerColonnaOperatore(
        DataTable csvData,
        DateOnly dataRif,
        string colonnaOperatore,
        string tipoRisultato,
        string? idEvento,
        string? centro)
    {
        if (!csvData.Columns.Contains(colonnaOperatore))
        {
            Logger.LogDebug("Colonna {Colonna} non presente nel CSV HERA16", colonnaOperatore);
            return;
        }

        var perOperatore = csvData.AsEnumerable()
            .Select(r => r.Field<string>(colonnaOperatore)?.Trim())
            .Where(op => !string.IsNullOrWhiteSpace(op))
            .GroupBy(op => op!)
            .Select(g => new DatiMailCsvDto(
                "HERA16", dataRif, g.Key, tipoRisultato, g.Count(), idEvento, centro));

        foreach (var dto in perOperatore)
        {
            _righeElaborate.Add(dto);
            Logger.LogInformation(
                "HERA16 {Tipo}: operatore '{Op}' documenti {Doc}",
                tipoRisultato, dto.Operatore, dto.Documenti);
        }
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
