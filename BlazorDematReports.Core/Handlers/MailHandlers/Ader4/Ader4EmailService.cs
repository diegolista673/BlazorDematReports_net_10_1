using BlazorDematReports.Core.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BlazorDematReports.Core.Handlers.MailHandlers.Ader4
{
    /// <summary>
    /// Implementazione servizio email per ADER4 (Equitalia 4).
    /// Gestisce import CSV da allegati email con logica specifica Sorter/Captiva.
    /// </summary>
    public class Ader4EmailService : BaseEwsEmailService, IEmailBatchProcessor
    {
        private readonly IConfiguration _configuration;

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
                ExchangeUrl = new Uri(mailConfig["ExchangeUrl"] ?? "https://postaweb.postel.it/ews/exchange.asmx"),
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
                LocalAttachmentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "report", "ADER4"),
                LocalArchivePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "archive", "ADER4"),
                MaxEmailsPerRun = 100,
                CreateZipArchive = true,
                CleanupAfterProcessing = true
            };
        }

        /// <summary>
        /// Processa allegato CSV specifico per ADER4.
        /// </summary>
        protected override async Task ProcessAttachmentAsync(
            AttachmentInfo attachment,
            Dictionary<string, string> metadata,
            CancellationToken ct)
        {
            Logger.LogInformation("Elaborazione allegato ADER4: {FileName}", attachment.FileName);

            // Leggi CSV
            var csvData = ReadCsvFile(attachment.LocalFilePath);

            if (csvData.Rows.Count == 0)
            {
                Logger.LogWarning("File CSV {FileName} vuoto", attachment.FileName);
                return;
            }

            if (attachment.FileName.Contains("EQTMN4_Scatole_Scansionate"))
            {
                await ProcessScatoleScansionateAsync(csvData, metadata, ct);
            }
            else if (attachment.FileName.Contains("EQTMN4_Dispacci_Preaccettati"))
            {
                await ProcessDispacciAsync(csvData, "PreAccettazione", metadata, ct);
            }
            else if (attachment.FileName.Contains("EQTMN4_Dispacci_Ripartiti"))
            {
                await ProcessDispacciAsync(csvData, "Ripartizione", metadata, ct);
            }
            else if (attachment.FileName.Contains("EQTMN4_Scatole_Restituite"))
            {
                await ProcessDispacciAsync(csvData, "Restituzione", metadata, ct);
            }

            Logger.LogInformation(
                "Allegato ADER4 {FileName} processato: {RowCount} righe",
                attachment.FileName,
                csvData.Rows.Count
            );
        }

        /// <summary>
        /// Processa file scatole scansionate (logica Sorter/Captiva).
        /// </summary>
        private async Task ProcessScatoleScansionateAsync(
            DataTable csvData,
            Dictionary<string, string> metadata,
            CancellationToken ct)
        {
            Logger.LogInformation("Elaborazione Scatole Scansionate: {RowCount} righe", csvData.Rows.Count);

            // Estrai totali per tipo scansione
            var totali = CalculateTotaliScansionati(csvData);

            metadata["ScansioneCaptiva"] = totali.ScansioneCaptiva.ToString();
            metadata["ScansioneSorter"] = totali.ScansioneSorter.ToString();
            metadata["ScartiScansioneSorter"] = totali.ScartiScansioneSorter.ToString();
            metadata["ScansioneSorterBuste"] = totali.ScansioneSorterBuste.ToString();
            metadata["ScartiScansioneSorterBuste"] = totali.ScartiScansioneSorterBuste.ToString();

            Logger.LogInformation(
                "Totali Scansionati - Captiva:{Captiva}, Sorter:{Sorter}, ScartiSorter:{ScartiS}, SorterBuste:{SorterB}, ScartiBuste:{ScartiB}",
                totali.ScansioneCaptiva,
                totali.ScansioneSorter,
                totali.ScartiScansioneSorter,
                totali.ScansioneSorterBuste,
                totali.ScartiScansioneSorterBuste
            );

            // TODO: Inserimento dati in database (implementare in classe derivata o via DI)
                await Task.CompletedTask;
            }

            /// <summary>
            /// Processa file dispacci (Pre-accettazione, Ripartizione, Restituzione).
            /// Calcola il totale documenti e lo inserisce nei metadata con la chiave del tipo dispaccio.
            /// </summary>
            private async Task ProcessDispacciAsync(
                DataTable csvData,
                string tipoDispaccio,
                Dictionary<string, string> metadata,
                CancellationToken ct)
            {
                Logger.LogInformation("Elaborazione Dispacci {Tipo}: {RowCount} righe", tipoDispaccio, csvData.Rows.Count);

                int totale = CalculateTotaleDocumenti(csvData);
                metadata[tipoDispaccio] = totale.ToString();

                Logger.LogInformation("Totale Dispacci {Tipo}: {Totale}", tipoDispaccio, totale);

                await Task.CompletedTask;
            }

            /// <summary>
        /// Calcola totali per file scatole scansionate (logica specifica ADER4).
        /// </summary>
        private TotaliScansionati CalculateTotaliScansionati(DataTable csvData)
        {
            var totali = new TotaliScansionati();

            // Captiva: codiceScatola[4:9] != "999X9"
            totali.ScansioneCaptiva = csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero documenti");
                    var codiceScatola = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrEmpty(numeroDoc) &&
                           int.TryParse(numeroDoc, out _) &&
                           !string.IsNullOrEmpty(codiceScatola) &&
                           codiceScatola.Length >= 9 &&
                           codiceScatola.Substring(4, 5) != "999X9";
                })
                .Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"));

            // Sorter: codiceScatola[4:10] == "999X91" || "999X92", codiceScatola[0:3] == "MN4"
            totali.ScansioneSorter = csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero documenti");
                    var codiceScatola = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrEmpty(numeroDoc) &&
                           int.TryParse(numeroDoc, out _) &&
                           !string.IsNullOrEmpty(codiceScatola) &&
                           codiceScatola.Length >= 10 &&
                           (codiceScatola.Substring(4, 6) == "999X91" || codiceScatola.Substring(4, 6) == "999X92") &&
                           codiceScatola.Substring(0, 3) == "MN4";
                })
                .Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"));

            // Scarti Sorter: codiceScatola[4:10] == "999X91" || "999X92", codiceScatola[0:3] != "MN4"
            totali.ScartiScansioneSorter = csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero documenti");
                    var codiceScatola = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrEmpty(numeroDoc) &&
                           int.TryParse(numeroDoc, out _) &&
                           !string.IsNullOrEmpty(codiceScatola) &&
                           codiceScatola.Length >= 10 &&
                           (codiceScatola.Substring(4, 6) == "999X91" || codiceScatola.Substring(4, 6) == "999X92") &&
                           codiceScatola.Substring(0, 3) != "MN4";
                })
                .Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"));

            // Sorter Buste: codiceScatola[4:10] == "999X93", codiceScatola[0:3] == "MN4"
            totali.ScansioneSorterBuste = csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero documenti");
                    var codiceScatola = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrEmpty(numeroDoc) &&
                           int.TryParse(numeroDoc, out _) &&
                           !string.IsNullOrEmpty(codiceScatola) &&
                           codiceScatola.Length >= 10 &&
                           codiceScatola.Substring(4, 6) == "999X93" &&
                           codiceScatola.Substring(0, 3) == "MN4";
                })
                .Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"));

            // Scarti Sorter Buste: codiceScatola[4:10] == "999X93", codiceScatola[0:3] != "MN4"
            totali.ScartiScansioneSorterBuste = csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero documenti");
                    var codiceScatola = r.Field<string>("Codice Scatola");
                    return !string.IsNullOrEmpty(numeroDoc) &&
                           int.TryParse(numeroDoc, out _) &&
                           !string.IsNullOrEmpty(codiceScatola) &&
                           codiceScatola.Length >= 10 &&
                           codiceScatola.Substring(4, 6) == "999X93" &&
                           codiceScatola.Substring(0, 3) != "MN4";
                })
                .Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"));

            return totali;
        }

        /// <summary>
        /// Calcola totale documenti da colonna "Numero Documenti".
        /// </summary>
        private int CalculateTotaleDocumenti(DataTable csvData)
        {
            return csvData.AsEnumerable()
                .Where(r =>
                {
                    var numeroDoc = r.Field<string>("Numero Documenti");
                    return !string.IsNullOrEmpty(numeroDoc) && int.TryParse(numeroDoc, out _);
                })
                .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));
        }

        /// <summary>
        /// Estrae metadata specifici ADER4 dal body email.
        /// </summary>
        protected override void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
        {
            base.ExtractMetadataFromBody(bodyText, metadata);

            // Metadata aggiuntivi ADER4
            ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
            ExtractMetadataField(bodyText, "Periodo di riferimento:", metadata, "DataRiferimento");
        }



        /// <summary>
        /// DTO per totali scansionati.
        /// </summary>
        private sealed class TotaliScansionati
        {
            public int ScansioneCaptiva { get; set; }
            public int ScansioneSorter { get; set; }
            public int ScartiScansioneSorter { get; set; }
            public int ScansioneSorterBuste { get; set; }
            public int ScartiScansioneSorterBuste { get; set; }
        }
    }
}
