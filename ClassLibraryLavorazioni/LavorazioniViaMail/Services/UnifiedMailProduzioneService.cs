using Entities.Models.DbApplication;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni.LavorazioniViaMail.Services
{
    /// <summary>
    /// Servizio unificato per elaborazione mail ADER4/HERA16.
    /// Inserisce dati in ProduzioneSistema con metadata (EventoId, NomeAllegato, Centro).
    /// </summary>
    public class UnifiedMailProduzioneService
    {
        private readonly ILogger<UnifiedMailProduzioneService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<DematReportsContext> _dbContextFactory;

        public UnifiedMailProduzioneService(
            ILogger<UnifiedMailProduzioneService> logger,
            IConfiguration configuration,
            IDbContextFactory<DematReportsContext> dbContextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        /// <summary>
        /// Processa email ADER4 (Verona + Genova).
        /// </summary>
        public async Task<int> ProcessAder4Async(CancellationToken ct = default)
        {
            _logger.LogInformation("[ADER4] Inizio elaborazione");

            var config = _configuration.GetSection("MailServices:ADER4");
            var service = CreateExchangeService(config);

            var totalInserted = 0;

            // Email Verona
            var emailsVerona = FindEmails(service, config["SubjectVerona"]!);
            totalInserted += await ProcessEmailsAsync(emailsVerona, "VERONA", config, ct);

            // Email Genova
            var emailsGenova = FindEmails(service, config["SubjectGenova"]!);
            totalInserted += await ProcessEmailsAsync(emailsGenova, "GENOVA", config, ct);

            _logger.LogInformation("[ADER4] Completato: {Total} righe totali", totalInserted);
            return totalInserted;
        }

        /// <summary>
        /// Processa email HERA16.
        /// </summary>
        public async Task<int> ProcessHera16Async(CancellationToken ct = default)
        {
            _logger.LogInformation("[HERA16] Inizio elaborazione");

            var config = _configuration.GetSection("MailServices:HERA16");
            var service = CreateExchangeService(config);

            var emails = FindEmails(service, config["SubjectFilter"]!);
            var totalInserted = await ProcessEmailsAsync(emails, "VERONA", config, ct);

            _logger.LogInformation("[HERA16] Completato: {Total} righe totali", totalInserted);
            return totalInserted;
        }

        /// <summary>
        /// Processa lista email.
        /// </summary>
        private async Task<int> ProcessEmailsAsync(
            List<EmailMessage> emails,
            string centro,
            IConfigurationSection config,
            CancellationToken ct)
        {
            var totalInserted = 0;

            foreach (var email in emails)
            {
                email.Load(new PropertySet(
                    EmailMessageSchema.Attachments,
                    EmailMessageSchema.Body,
                    EmailMessageSchema.DateTimeReceived));

                var eventoId = ExtractEventoId(email.Body.Text);
                var dataRiferimento = ExtractDataRiferimento(email.Body.Text);

                foreach (var attachment in email.Attachments.OfType<FileAttachment>())
                {
                    if (!attachment.Name.Contains(config["AttachmentPattern"]!))
                        continue;

                    attachment.Load();

                    // Parse CSV
                    var csvTable = ParseCsvFromBytes(attachment.Content);

                    // Inserisci per ogni fase
                    var inserted = await InsertProduzionePerFasiAsync(
                        csvTable,
                        centro,
                        eventoId,
                        attachment.Name,
                        dataRiferimento,
                        config,
                        ct);

                    totalInserted += inserted;

                    _logger.LogInformation("[{Service}] {Centro} - {File}: {Rows} righe",
                        config.Key, centro, attachment.Name, inserted);
                }

                // Archivia email
                ArchiveEmail(email, config["ArchiveFolder"]!);
            }


            return totalInserted;
        }

        /// <summary>
        /// Inserisce dati in ProduzioneSistema per tutte le fasi configurate.
        /// </summary>
        private async Task<int> InsertProduzionePerFasiAsync(
            DataTable csvTable,
            string centro,
            string? eventoId,
            string nomeAllegato,
            DateTime dataRiferimento,
            IConfigurationSection config,
            CancellationToken ct)
        {
            var totalRows = 0;
            var fasi = config.GetSection("Fasi").GetChildren();

            using var context = await _dbContextFactory.CreateDbContextAsync(ct);

            foreach (var fase in fasi)
            {
                var centroConfig = fase.GetSection(centro);
                if (!centroConfig.Exists())
                    continue;

                var idProcedura = centroConfig.GetValue<int>("IdProcedura");
                var idFase = centroConfig.GetValue<int>("IdFase");

                // Estrai dati per questa fase
                var records = ExtractRecordsForFase(csvTable, fase.Key, dataRiferimento);

                // Inserisci in ProduzioneSistema
                foreach (var record in records)
                {
                    context.ProduzioneSistemas.Add(new ProduzioneSistema
                    {
                        Operatore = record.Operatore,
                        DataLavorazione = record.DataLavorazione,
                        Documenti = record.Documenti,
                        Fogli = record.Fogli,
                        Pagine = record.Pagine,
                        IdProceduraLavorazione = idProcedura,
                        IdFaseLavorazione = idFase,
                        IdCentro = 1, // TODO: Recuperare da configurazione
                        IdOperatore = 1, // TODO: Lookup da Operatore
                        DataAggiornamento = DateTime.Now,
                        FlagInserimentoAuto = true,

                        // Metadata
                        EventoId = eventoId,
                        NomeAllegato = nomeAllegato,
                        CentroElaborazione = centro
                    });

                    totalRows++;
                }
            }

            await context.SaveChangesAsync(ct);
            return totalRows;
        }

        /// <summary>
        /// Estrae records per specifica fase (riusa logica LINQ esistente).
        /// </summary>
        private List<ProduzioneRecord> ExtractRecordsForFase(
            DataTable table,
            string faseName,
            DateTime dataLavorazione)
        {
            return faseName switch
            {
                // ADER4 - Sorter Buste
                "SorterBuste" => table.AsEnumerable()
                    .Where(r => IsSorterBuste(r))
                    .GroupBy(r => r.Field<string>("Postazione")?.ToLower())
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key!,
                        DataLavorazione = dataLavorazione,
                        Documenti = g.Sum(r => GetIntValue(r, "Numero documenti"))
                    }).ToList(),

                // ADER4 - Sorter
                "Sorter" => table.AsEnumerable()
                    .Where(r => IsSorter(r))
                    .GroupBy(r => r.Field<string>("Postazione")?.ToLower())
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key!,
                        DataLavorazione = dataLavorazione,
                        Documenti = g.Sum(r => GetIntValue(r, "Numero documenti"))
                    }).ToList(),

                // ADER4 - Captiva
                "Captiva" => table.AsEnumerable()
                    .Where(r => IsCaptiva(r))
                    .GroupBy(r => r.Field<string>("Postazione")?.ToLower())
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key!,
                        DataLavorazione = dataLavorazione,
                        Documenti = g.Sum(r => GetIntValue(r, "Numero documenti"))
                    }).ToList(),

                // HERA16 - Scansione
                "Scansione" => table.AsEnumerable()
                    .Where(r => HasValue(r, "operatore_scan") && HasValue(r, "data_scansione"))
                    .GroupBy(r => new
                    {
                        Op = r.Field<string>("operatore_scan"),
                        Data = ParseDate(r.Field<string>("data_scansione"))
                    })
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key.Op,
                        DataLavorazione = g.Key.Data,
                        Documenti = g.Count()
                    }).ToList(),

                // HERA16 - Classificazione
                "Classificazione" => table.AsEnumerable()
                    .Where(r => HasValue(r, "operatore_classificazione") && HasValue(r, "data_classificazione"))
                    .GroupBy(r => new
                    {
                        Op = r.Field<string>("operatore_classificazione"),
                        Data = ParseDate(r.Field<string>("data_classificazione"))
                    })
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key.Op,
                        DataLavorazione = g.Key.Data,
                        Documenti = g.Count()
                    }).ToList(),

                // HERA16 - Indicizzazione
                "Indicizzazione" => table.AsEnumerable()
                    .Where(r => HasValue(r, "operatore_index") && HasValue(r, "data_index"))
                    .GroupBy(r => new
                    {
                        Op = r.Field<string>("operatore_index"),
                        Data = ParseDate(r.Field<string>("data_index"))
                    })
                    .Select(g => new ProduzioneRecord
                    {
                        Operatore = g.Key.Op,
                        DataLavorazione = g.Key.Data,
                        Documenti = g.Count()
                    }).ToList(),

                _ => new List<ProduzioneRecord>()
            };
        }

        // ========== HELPER METHODS ==========

        private ExchangeService CreateExchangeService(IConfigurationSection config)
        {
            return new ExchangeService(ExchangeVersion.Exchange2013_SP1)
            {
                Credentials = new WebCredentials(
                    config["Username"],
                    config["Password"],
                    config["Domain"]),
                Url = new Uri(config["ExchangeUrl"]!)
            };
        }

        private List<EmailMessage> FindEmails(ExchangeService service, string subjectFilter)
        {
            var filter = new SearchFilter.ContainsSubstring(ItemSchema.Subject, subjectFilter);
            var view = new ItemView(100);
            var results = service.FindItems(WellKnownFolderName.Inbox, filter, view);
            return results.Items.OfType<EmailMessage>().ToList();
        }

        private DataTable ParseCsvFromBytes(byte[] content)
        {
            using var stream = new MemoryStream(content);
            using var reader = new CsvReader(
                new StreamReader(stream),
                hasHeaders: true,
                delimiter: ';',
                quote: '\0',
                escape: '\0',
                comment: '\0',
                trimmingOptions: ValueTrimmingOptions.All);

            var table = new DataTable();
            var headers = reader.GetFieldHeaders();

            foreach (var header in headers)
                table.Columns.Add(header, typeof(string));

            while (reader.ReadNextRecord())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader[i];
                table.Rows.Add(row);
            }

            return table;
        }

        private void ArchiveEmail(EmailMessage email, string folderName)
        {
            var viewF = new FolderView(1) { Traversal = FolderTraversal.Deep };
            var filter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, folderName);
            var results = email.Service.FindFolders(WellKnownFolderName.Root, filter, viewF);

            if (results.TotalCount > 0)
            {
                email.IsRead = true;
                email.Update(ConflictResolutionMode.AutoResolve);
                email.Move(results.Folders[0].Id);
                _logger.LogInformation("Email archiviata in {Folder}", folderName);
            }
        }

        private string? ExtractEventoId(string body)
        {
            var match = Regex.Match(body, @"Identificativo evento:\s*(\S+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private DateTime ExtractDataRiferimento(string body)
        {
            var match = Regex.Match(body, @"Periodo di riferimento:\s*(\S+)");
            return match.Success && DateTime.TryParse(match.Groups[1].Value, out var date)
                ? date
                : DateTime.Today;
        }

        // Filtri ADER4
        private bool IsSorterBuste(DataRow r)
        {
            var codice = r.Field<string>("Codice Scatola");
            return !string.IsNullOrEmpty(codice) && codice.Length >= 10 &&
                   codice.Substring(4, 6) == "999X93" &&
                   codice.Substring(0, 3) == "MN4";
        }

        private bool IsSorter(DataRow r)
        {
            var codice = r.Field<string>("Codice Scatola");
            return !string.IsNullOrEmpty(codice) && codice.Length >= 10 &&
                   (codice.Substring(4, 6) == "999X91" || codice.Substring(4, 6) == "999X92") &&
                   codice.Substring(0, 3) == "MN4";
        }

        private bool IsCaptiva(DataRow r)
        {
            var codice = r.Field<string>("Codice Scatola");
            return !string.IsNullOrEmpty(codice) && codice.Length >= 9 &&
                   codice.Substring(4, 5) != "999X9";
        }

        private bool HasValue(DataRow r, string column)
        {
            return !string.IsNullOrEmpty(r.Field<string>(column));
        }

        private int GetIntValue(DataRow r, string column)
        {
            var value = r.Field<string>(column);
            return int.TryParse(value, out var num) ? num : 0;
        }

        private DateTime ParseDate(string? dateStr)
        {
            return DateTime.TryParse(dateStr, out var date) ? date : DateTime.Today;
        }

        private class ProduzioneRecord
        {
            public string? Operatore { get; set; }
            public DateTime DataLavorazione { get; set; }
            public int? Documenti { get; set; }
            public int? Fogli { get; set; }
            public int? Pagine { get; set; }
        }
    }
}
