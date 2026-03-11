using BlazorDematReports.Core.Services.Email;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Hera16;

/// <summary>
/// Servizio email per HERA16.
/// Legge allegati CSV dall'email di report giornaliero HERA16 via Exchange EWS,
/// raggruppa le righe per operatore (OperatoreScansione / OperatoreIndex / OperatoreClassificazione)
/// e popola <see cref="RigheElaborate"/> con <see cref="DatiMailCsvDto"/> per-operatore.
///
/// Formato CSV atteso (una riga = un documento elaborato):
/// <code>
/// codice_mercato;codice_offerta;tipo_documento;data_scansione;operatore_scan;data_classificazione;operatore_classificazione;data_index;operatore_index;data_pubblicazione;codice_scatola;progr_scansione;identificativo_allegato  
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
    /// Le date di lavorazione vengono lette direttamente dalle colonne CSV (data_scansione,
    /// data_index, data_classificazione), non dai metadata dell'email.
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

        metadata.TryGetValue("IdEvento", out var idEvento);
        metadata.TryGetValue("Centro", out var centro);

        int countPrecedente = _righeElaborate.Count;
        AggiungiRigheHera16(csvData, idEvento, centro);

        Logger.LogInformation(
            "Allegato HERA16 {FileName} processato: {RowCount} righe CSV, {Righe} DatiMailCsvDto aggiunti",
            attachment.FileName, csvData.Rows.Count, _righeElaborate.Count - countPrecedente);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Aggrega le righe CSV per le tre fasi HERA16 usando la data effettiva di ciascuna colonna,
    /// applicando le stesse regole dei report SQL di produzione:
    /// Scansione  : GROUP BY operatore_scan, DATE(data_scansione)
    /// Index      : GROUP BY operatore_index, DATE(data_index)
    ///              WHERE operatore_index NOT IN ('-','engine')
    ///              AND tipo_documento NOT IN ('BRIT','DR01','XXXX')
    /// Classificazione: GROUP BY operatore_classificazione, DATE(data_classificazione)
    ///              WHERE operatore_classificazione NOT IN ('-','engine')
    /// </summary>
    private void AggiungiRigheHera16(DataTable csvData, string? idEvento, string? centro)
    {
        // Scansione: GROUP BY operatore_scan, DATE(data_scansione)
        AggiungiRighePerColonnaOperatore(
            csvData, "operatore_scan", "data_scansione", "Scansione", idEvento, centro);

        // Index: escludi operatore '-'/'engine' e tipo_documento in ('BRIT','DR01','XXXX')
        AggiungiRighePerColonnaOperatore(
            csvData, "operatore_index", "data_index", "Index", idEvento, centro,
            excludedOperatori: ["-", "engine"],
            excludedTipiDocumento: ["BRIT", "DR01", "XXXX"]);

        // Classificazione: escludi operatore '-'/'engine'
        AggiungiRighePerColonnaOperatore(
            csvData, "operatore_classificazione", "data_classificazione", "Classificazione", idEvento, centro,
            excludedOperatori: ["-", "engine"]);
    }

    /// <summary>
    /// Per una coppia (colonnaOperatore, colonnaData) raggruppa per (operatore, data) e
    /// conta i documenti distinti tramite COUNT(DISTINCT codice_mercato + codice_offerta + tipo_documento),
    /// applicando eventuali filtri su operatore e tipo_documento.
    /// </summary>
    private void AggiungiRighePerColonnaOperatore(
        DataTable csvData,
        string colonnaOperatore,
        string colonnaData,
        string tipoRisultato,
        string? idEvento,
        string? centro,
        string[]? excludedOperatori = null,
        string[]? excludedTipiDocumento = null)
    {
        if (!csvData.Columns.Contains(colonnaOperatore))
        {
            Logger.LogDebug("Colonna {Colonna} non presente nel CSV HERA16", colonnaOperatore);
            return;
        }

        if (!csvData.Columns.Contains(colonnaData))
        {
            Logger.LogDebug("Colonna data {Colonna} non presente nel CSV HERA16", colonnaData);
            return;
        }

        var perGruppo = csvData.AsEnumerable()
            .Select(r => new
            {
                Operatore     = r.Field<string>(colonnaOperatore)?.Trim(),
                DataStr       = r.Field<string>(colonnaData)?.Trim(),
                Chiave        = (r.Field<string>("codice_mercato")  ?? string.Empty)
                              + (r.Field<string>("codice_offerta")   ?? string.Empty)
                              + (r.Field<string>("tipo_documento")   ?? string.Empty),
                TipoDocumento = r.Field<string>("tipo_documento")?.Trim()
            })
            .Where(r =>
                !string.IsNullOrWhiteSpace(r.Operatore) &&
                (excludedOperatori    is null || !excludedOperatori.Any(e    => string.Equals(e, r.Operatore,     StringComparison.OrdinalIgnoreCase))) &&
                (excludedTipiDocumento is null || !excludedTipiDocumento.Any(e => string.Equals(e, r.TipoDocumento, StringComparison.OrdinalIgnoreCase)))
            )
            .Select(r => new { r.Operatore, Data = ParseDateOnly(r.DataStr), r.Chiave })
            .Where(r => r.Data.HasValue)
            .GroupBy(r => (Operatore: r.Operatore!, Data: r.Data!.Value))
            .Select(g => new DatiMailCsvDto(
                "HERA16",
                g.Key.Data,
                g.Key.Operatore,
                tipoRisultato,
                g.Select(r => r.Chiave).Distinct(StringComparer.Ordinal).Count(),
                idEvento,
                centro));

        foreach (var dto in perGruppo)
        {
            _righeElaborate.Add(dto);
            Logger.LogInformation(
                "HERA16 {Tipo}: operatore '{Op}' data {Data} documenti {Doc}",
                tipoRisultato, dto.Operatore, dto.DataLavorazione, dto.Documenti);
        }
    }

    /// <summary>
    /// Converte una stringa data CSV in <see cref="DateOnly"/>, ignorando l'eventuale componente oraria.
    /// Formato atteso: "dd/MM/yyyy" oppure "dd/MM/yyyy HH:mm:ss".
    /// Restituisce null se la stringa non e' valida o e' il segnaposto '-'.
    /// </summary>
    private static DateOnly? ParseDateOnly(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr) || dateStr == "-")
            return null;

        // Prende solo la parte data (dd/MM/yyyy), scartando l'orario se presente
        var spazio = dateStr.IndexOf(' ');
        var datePart = spazio > 0 ? dateStr[..spazio] : dateStr;

        return DateOnly.TryParseExact(datePart, "dd/MM/yyyy",
                   CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
               ? d
               : null;
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
