using BlazorDematReports.Core.Services.Email;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4
{
    /// <summary>
    /// Implementazione servizio email per ADER4 (Equitalia 4).
    /// Legge allegati CSV, raggruppa per colonna 'postazione' (operatore reale)
    /// e popola la lista RigheElaborate con DatiMailCsvDto per-operatore.
    /// </summary>
    public class Ader4EmailService : BaseEwsEmailService, IEmailBatchProcessor
    {
        private readonly IConfiguration _configuration;
        private readonly List<DatiMailCsvDto> _righeElaborate = [];

        /// <summary>Righe per-operatore estratte dall'ultima elaborazione email.</summary>
        public IReadOnlyList<DatiMailCsvDto> RigheElaborate => _righeElaborate;

        /// <summary>Svuota la lista righe prima di una nuova elaborazione.</summary>
        protected void RigheElaborate_Clear() => _righeElaborate.Clear();

        /// <summary>
        /// Inizializza una nuova istanza di Ader4EmailService.
        /// </summary>
        /// <param name="configuration">Configurazione applicazione.</param>
        /// <param name="logger">Logger per registrazione eventi.</param>
        public Ader4EmailService(IConfiguration configuration, ILogger<Ader4EmailService> logger)
            : base(CreateConfig(configuration), logger)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Crea configurazione EWS da appsettings.json per ADER4.
        /// </summary>
        private static EwsEmailServiceConfig CreateConfig(IConfiguration configuration)
        {
            var mailConfig = configuration.GetSection("MailServices:ADER4");

            return new EwsEmailServiceConfig
            {   
                Username = mailConfig["Username"] ?? throw new InvalidOperationException("ADER4 Username mancante"),
                Password = mailConfig["Password"] ?? throw new InvalidOperationException("ADER4 Password mancante"),
                Domain = mailConfig["Domain"] ?? "postel.it",
                ExchangeUrl = new Uri(mailConfig["ExchangeUrl"] ?? "https://webmail.postel.it/ews/exchange.asmx"),
                SubjectFilters = new[]
                {
                    mailConfig["SubjectVerona"] ?? "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)",
                    mailConfig["SubjectGenova"] ?? "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)"
                },
                AttachmentPatterns = new[]
                {
                    "EQTMN4_Dispacci_Preaccettati*",
                    "EQTMN4_Dispacci_Ripartiti*",
                    "EQTMN4_Scatole_Scansionate*",
                    "EQTMN4_Scatole_Restituite*"
                },
                ArchiveFolderName = mailConfig["ArchiveFolder"] ?? "EQUITALIA_4",
                MaxEmailsPerRun = 100
            };
        }

        /// <summary>
        /// Processa allegato CSV ADER4: raggruppa per 'postazione' (operatore) e accumula righe.
        /// </summary>
        protected override async Task ProcessAttachmentAsync(
            AttachmentInfo attachment,
            Dictionary<string, string> metadata,
            CancellationToken ct)
        {
            Logger.LogInformation("Elaborazione allegato ADER4: {FileName}", attachment.FileName);

            if (attachment.Content.Length == 0)
            {
                Logger.LogWarning("File CSV {FileName} vuoto (0 byte), ignorato", attachment.FileName);
                return;
            }

            var csvData = ReadCsvFromBytes(attachment.Content);

            if (csvData.Rows.Count == 0)
            {
                Logger.LogWarning("File CSV {FileName} senza righe, ignorato", attachment.FileName);
                return;
            }

            metadata.TryGetValue("DataLavorazione", out var dataRifStr);
            metadata.TryGetValue("IdEvento", out var idEvento);
            metadata.TryGetValue("Centro", out var centro);

            if (!DateOnly.TryParse(dataRifStr, out var dataRif))
            {
                Logger.LogWarning("DataLavorazione non valida '{Data}' in {FileName}, riga ignorata", dataRifStr, attachment.FileName);
                return;
            }

            if (attachment.FileName.Contains("EQTMN4_Scatole_Scansionate"))
                AggiungiRigheScatoleScansionate(csvData, dataRif, idEvento, centro, attachment.FileName);
            else if (attachment.FileName.Contains("EQTMN4_Dispacci_Preaccettati"))
                AggiungiRigheDispacci(csvData, "PreAccettazione", dataRif, idEvento, centro, attachment.FileName);
            else if (attachment.FileName.Contains("EQTMN4_Dispacci_Ripartiti"))
                AggiungiRigheDispacci(csvData, "Ripartizione", dataRif, idEvento, centro, attachment.FileName);
            else if (attachment.FileName.Contains("EQTMN4_Scatole_Restituite"))
                AggiungiRigheDispacci(csvData, "Restituzione", dataRif, idEvento, centro, attachment.FileName);

            Logger.LogInformation("Allegato ADER4 {FileName} processato: {RowCount} righe CSV", attachment.FileName, csvData.Rows.Count);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Aggiunte righe per-operatore da Scatole_Scansionate, raggruppate per 'postazione'.
        /// Ogni gruppo produce tre TipoRisultato: ScansioneCaptiva, ScansioneSorter, ScansioneSorterBuste.
        /// </summary>
        private void AggiungiRigheScatoleScansionate(
            DataTable csvData,
            DateOnly dataRif,
            string? idEvento,
            string? centro,
            string? nomeFile)
        {
            var righeValide = csvData.AsEnumerable()
                .Where(r =>
                {
                    var doc = r.Field<string>("Numero documenti");
                    var cod = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrWhiteSpace(doc) && int.TryParse(doc, out _)
                        && !string.IsNullOrWhiteSpace(cod);
                })
                .ToList();

            // Raggruppa per postazione
            var perPostazione = righeValide
                .GroupBy(r => r.Field<string>("postazione") ?? "SCONOSCIUTO");

            foreach (var gruppo in perPostazione)
            {
                var operatore = gruppo.Key;

                var captiva = gruppo
                    .Where(r => IsCaptiva(r.Field<string>("Codice Scatola")!))
                    .Sum(r => ParseDoc(r.Field<string>("Numero documenti")));

                var sorter = gruppo
                    .Where(r => IsSorter(r.Field<string>("Codice Scatola")!))
                    .Sum(r => ParseDoc(r.Field<string>("Numero documenti")));

                var sorterBuste = gruppo
                    .Where(r => IsSorterBuste(r.Field<string>("Codice Scatola")!))
                    .Sum(r => ParseDoc(r.Field<string>("Numero documenti")));

                if (captiva > 0)
                    _righeElaborate.Add(new DatiMailCsvDto("ADER4", dataRif, operatore, "ScansioneCaptiva", captiva, idEvento, centro, nomeFile));
                if (sorter > 0)
                    _righeElaborate.Add(new DatiMailCsvDto("ADER4", dataRif, operatore, "ScansioneSorter", sorter, idEvento, centro, nomeFile));
                if (sorterBuste > 0)
                    _righeElaborate.Add(new DatiMailCsvDto("ADER4", dataRif, operatore, "ScansioneSorterBuste", sorterBuste, idEvento, centro, nomeFile));
            }

            Logger.LogInformation(
                "ScatoleScansionate: {Operatori} operatori trovati, {Righe} righe aggiunte",
                perPostazione.Count(), _righeElaborate.Count);
        }

        /// <summary>
        /// Aggiunge righe per-operatore dai file Dispacci, raggruppate per 'postazione'.
        /// </summary>
        private void AggiungiRigheDispacci(
            DataTable csvData,
            string tipoRisultato,
            DateOnly dataRif,
            string? idEvento,
            string? centro,
            string? nomeFile)
        {
            Logger.LogInformation("Elaborazione Dispacci {Tipo}: {RowCount} righe", tipoRisultato, csvData.Rows.Count);

            var perPostazione = csvData.AsEnumerable()
                .Where(r =>
                {
                    var doc = r.Field<string>("Numero Documenti");
                    return !string.IsNullOrWhiteSpace(doc) && int.TryParse(doc, out _);
                })
                .GroupBy(r => r.Field<string>("postazione") ?? "SCONOSCIUTO");

            foreach (var gruppo in perPostazione)
            {
                var totale = gruppo.Sum(r => ParseDoc(r.Field<string>("Numero Documenti")));
                if (totale > 0)
                    _righeElaborate.Add(new DatiMailCsvDto("ADER4", dataRif, gruppo.Key, tipoRisultato, totale, idEvento, centro, nomeFile));
            }
        }

        /// <summary>
        /// Estrae metadata specifici ADER4 dal body email.
        /// </summary>
        protected override void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
        {
            base.ExtractMetadataFromBody(bodyText, metadata);

            ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
            ExtractMetadataField(bodyText, "Periodo di riferimento:", metadata, "DataLavorazione");
        }

        // Classificazione codice scatola (regole ADER4)
        private static bool IsCaptiva(string cod)
            => cod.Length >= 9 && cod.Substring(4, 5) != "999X9";

        private static bool IsSorter(string cod)
            => cod.Length >= 10
            && (cod.Substring(4, 6) == "999X91" || cod.Substring(4, 6) == "999X92")
            && cod.StartsWith("MN4", StringComparison.Ordinal);

        private static bool IsSorterBuste(string cod)
            => cod.Length >= 10
            && cod.Substring(4, 6) == "999X93"
            && cod.StartsWith("MN4", StringComparison.Ordinal);

        private static int ParseDoc(string? value)
            => int.TryParse(value?.Trim(), out var n) ? n : 0;
    }
}
