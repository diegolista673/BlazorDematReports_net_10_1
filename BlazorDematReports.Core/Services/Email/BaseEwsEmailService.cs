using LumenWorks.Framework.IO.Csv;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Net;
using System.Text.RegularExpressions;

namespace BlazorDematReports.Core.Services.Email
{
    public abstract class BaseEwsEmailService
    {
        private readonly ILogger _logger;
        private readonly EwsEmailServiceConfig _config;
        private ExchangeService? _exchangeService;

        protected BaseEwsEmailService(EwsEmailServiceConfig config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected EwsEmailServiceConfig Config => _config;
        protected ILogger Logger => _logger;
        #region Exchange Service Initialization

        protected virtual void InitializeExchangeService()
        {
            _logger.LogInformation("Inizializzazione Exchange: URL={ExchangeUrl}, User={Username}, Domain={Domain}", _config.ExchangeUrl, _config.Username, _config.Domain);
            _exchangeService = new ExchangeService(_config.ExchangeVersion)
            {
                Credentials     = new WebCredentials(_config.Username, _config.Password, _config.Domain),
                Url             = _config.ExchangeUrl,
                WebProxy        = null,
                UserAgent       = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                KeepAlive       = true,
                PreAuthenticate = false,
                EnableScpLookup = false
            };
            _logger.LogInformation("Servizio Exchange inizializzato");
        }

        protected ExchangeService GetExchangeService()
        {
            if (_exchangeService is null) InitializeExchangeService();
            return _exchangeService!;
        }

        #endregion


        #region Email Search and Processing

        public virtual async System.Threading.Tasks.Task<BatchEmailProcessingResult> ProcessEmailsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Inizio elaborazione email batch");
            var service      = GetExchangeService();
            var searchFilter = BuildSearchFilter();
            var itemView     = new ItemView(_config.MaxEmailsPerRun, 0, OffsetBasePoint.Beginning)
            {
                PropertySet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.Subject, ItemSchema.DateTimeReceived)
            };
            itemView.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Ascending);
            FindItemsResults<Item> searchResults;
            try
            {
                searchResults = await service.FindItems(WellKnownFolderName.Inbox, searchFilter, itemView);
                _logger.LogInformation("Trovate {Count} email", searchResults.Items.Count);
            }
            catch (Exception ex) { _logger.LogError(ex, "Errore ricerca email"); throw; }

            if (searchResults.TotalCount == 0)
            {
                _logger.LogInformation("Nessuna email da processare");
                await OnNoEmailsFoundAsync(ct);
                return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
            }

            var successfulEmails = new List<EmailProcessingResult>();
            var failedEmails     = new List<EmailProcessingResult>();

            foreach (var item in searchResults.Items)
            {
                ct.ThrowIfCancellationRequested();
                if (item is not EmailMessage emailMessage) { _logger.LogWarning("Item non e EmailMessage, skip"); continue; }
                try
                {
                    var result = await ProcessSingleEmailAsync(emailMessage, ct);
                    if (result.Success) { successfulEmails.Add(result); _logger.LogInformation("Email OK: {Subject}", result.Subject); }
                    else { failedEmails.Add(result); _logger.LogWarning("Email KO: {Subject}, {Error}", result.Subject, result.ErrorMessage); }
                    await MoveEmailToArchiveFolderAsync(emailMessage, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore email {Subject}", emailMessage.Subject);
                    failedEmails.Add(new EmailProcessingResult { Subject = emailMessage.Subject ?? "Unknown", ReceivedDate = emailMessage.DateTimeReceived, Success = false, ErrorMessage = ex.Message });
                }
            }

            var batchResult = new BatchEmailProcessingResult { TotalEmailsFound = searchResults.TotalCount, SuccessfulEmails = successfulEmails, FailedEmails = failedEmails };
            _logger.LogInformation("Batch completato: Totali={Total}, OK={Success}, KO={Failed}", batchResult.TotalEmailsFound, successfulEmails.Count, failedEmails.Count);
            return batchResult;
        }

        protected virtual async System.Threading.Tasks.Task<EmailProcessingResult> ProcessSingleEmailAsync(EmailMessage emailMessage, CancellationToken ct)
        {
            await emailMessage.Load(new PropertySet(EmailMessageSchema.Attachments, EmailMessageSchema.Subject, EmailMessageSchema.DateTimeReceived, EmailMessageSchema.Body));
            var bodyText = emailMessage.Body?.Text ?? string.Empty;
            var attachments = new List<AttachmentInfo>();
            var extractedMetadata = new Dictionary<string, string>();
            ExtractMetadataFromBody(bodyText, extractedMetadata);
            emailMessage.IsRead = true;
            await emailMessage.Update(ConflictResolutionMode.AutoResolve);

            if (emailMessage.Attachments.Count == 0)
                return new EmailProcessingResult { Subject = emailMessage.Subject ?? string.Empty, ReceivedDate = emailMessage.DateTimeReceived, Body = bodyText, Success = true, ExtractedMetadata = extractedMetadata };

            foreach (var attachment in emailMessage.Attachments)
            {
                if (attachment is not FileAttachment fileAttachment) { _logger.LogWarning("Attachment non e FileAttachment, skip"); continue; }
                try
                {
                    var info = await DownloadAttachmentAsync(fileAttachment, ct);
                    attachments.Add(info);
                    if (info.MatchesPattern) await ProcessAttachmentAsync(info, extractedMetadata, ct);
                }
                catch (Exception ex) { _logger.LogError(ex, "Errore download allegato {Name}", fileAttachment.Name); }
            }
            return new EmailProcessingResult { Subject = emailMessage.Subject ?? string.Empty, ReceivedDate = emailMessage.DateTimeReceived, Body = bodyText, Success = true, Attachments = attachments, ExtractedMetadata = extractedMetadata };
        }

        #endregion

        #region Attachment Handling

        /// <summary>
        /// Scarica allegato direttamente in memoria tramite MemoryStream.
        /// Non scrive mai su disco: risolve il problema dei file vuoti (0 byte)
        /// causato da fileAttachment.Load(localPath) su EWS.
        /// </summary>
        protected virtual async System.Threading.Tasks.Task<AttachmentInfo> DownloadAttachmentAsync(FileAttachment fileAttachment, CancellationToken ct)
        {
            var fileName = fileAttachment.Name;
            _logger.LogDebug("Lettura allegato in memoria: {FileName}", fileName);

            using var memStream = new MemoryStream();
            await System.Threading.Tasks.Task.Run(() => fileAttachment.Load(memStream), ct);
            var content = memStream.ToArray();

            if (content.Length == 0)
                _logger.LogWarning("Allegato {FileName} ricevuto con 0 byte da EWS", fileName);

            var matchesPattern = MatchesAnyPattern(fileName);
            _logger.LogInformation("Allegato letto: {FileName}, Size={Size} bytes, Matches={Matches}", fileName, content.Length, matchesPattern);

            return new AttachmentInfo
            {
                FileName       = fileName,
                Content        = content,
                MatchesPattern = matchesPattern,
                ContentType    = fileAttachment.ContentType
            };
        }

        /// <summary>Verifica se nome file corrisponde a pattern configurati (wildcard * e ?).</summary>
        protected virtual bool MatchesAnyPattern(string fileName)
        {
            if (_config.AttachmentPatterns is null || !_config.AttachmentPatterns.Any())
                return true;

            foreach (var pattern in _config.AttachmentPatterns)
            {
                var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Processa allegato dopo download (es. parsing CSV, inserimento DB).
        /// Override in classi derivate per logica specifica.
        /// </summary>
        protected virtual System.Threading.Tasks.Task ProcessAttachmentAsync(AttachmentInfo attachment, Dictionary<string, string> metadata, CancellationToken ct)
        {
            _logger.LogDebug("ProcessAttachmentAsync base: nessuna elaborazione");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #endregion

        #region CSV Parsing

        /// <summary>
        /// Legge il contenuto CSV da un byte array in memoria.
        /// Non richiede file su disco.
        /// </summary>
        protected virtual DataTable ReadCsvFromBytes(byte[] content, char delimiter = ';', bool hasHeader = true)
        {
            _logger.LogDebug("Lettura CSV da memoria: {Size} bytes, Delimiter={Delimiter}", content.Length, delimiter);

            var dataTable = new DataTable();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var stream     = new MemoryStream(content);
                using var textReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                using var reader     = new CsvReader(textReader, hasHeader, delimiter, quote: '\0', escape: '\0', comment: '\0', ValueTrimmingOptions.All);

                int fieldCount = reader.FieldCount;
                foreach (var h in reader.GetFieldHeaders())
                    dataTable.Columns.Add(h, typeof(string));

                while (reader.ReadNextRecord())
                {
                    var row = dataTable.NewRow();
                    for (int i = 0; i < fieldCount; i++)
                        row[i] = reader[i];
                    dataTable.Rows.Add(row);
                }

                sw.Stop();
                _logger.LogInformation("CSV letto: {Rows} righe, {Cols} colonne, {Elapsed}ms",
                    dataTable.Rows.Count, dataTable.Columns.Count, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Errore lettura CSV da memoria, {Elapsed}ms", sw.ElapsedMilliseconds);
                throw;
            }

            return dataTable;
        }

        #endregion

        #region Metadata Extraction

        /// <summary>
        /// Estrae metadata dal body email usando regex.
        /// Override in classi derivate per pattern specifici.
        /// </summary>
        protected virtual void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
        {
            ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
            ExtractMetadataField(bodyText, "Periodo di riferimento:", metadata, "DataLavorazione");
            ExtractMetadataField(bodyText, "Data evento:", metadata, "DataEvento");
        }

        /// <summary>Estrae singolo campo metadata dal body text tramite regex.</summary>
        protected void ExtractMetadataField(string bodyText, string searchPattern, Dictionary<string, string> metadata, string metadataKey)
        {
            if (string.IsNullOrWhiteSpace(bodyText) || string.IsNullOrWhiteSpace(searchPattern))
                return;

            var match = Regex.Match(bodyText, Regex.Escape(searchPattern) + @"\s*(\S+)");
            if (match.Success)
            {
                metadata[metadataKey] = match.Groups[1].Value;
                _logger.LogDebug("Metadata: {Key}={Value}", metadataKey, match.Groups[1].Value);
            }
        }

        #endregion

        #region Archive Management

        /// <summary>Sposta email in cartella archivio Exchange.</summary>
        protected virtual async System.Threading.Tasks.Task MoveEmailToArchiveFolderAsync(EmailMessage emailMessage, CancellationToken ct)
        {
            try
            {
                var service       = GetExchangeService();
                var folderView    = new FolderView(1) { Traversal = FolderTraversal.Deep };
                var filter        = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, _config.ArchiveFolderName);
                var folderResults = await service.FindFolders(WellKnownFolderName.Root, filter, folderView);

                if (folderResults.Folders is null || !folderResults.Folders.Any())
                {
                    _logger.LogWarning("Cartella {Folder} non trovata", _config.ArchiveFolderName);
                    return;
                }

                var archiveFolder = folderResults.Folders.FirstOrDefault(f => f.DisplayName == _config.ArchiveFolderName);
                if (archiveFolder is not null)
                {
                    await System.Threading.Tasks.Task.Run(() => emailMessage.Move(archiveFolder.Id), ct);
                    _logger.LogInformation("Email {Subject} spostata in {Folder}", emailMessage.Subject, _config.ArchiveFolderName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore spostamento email {Subject}", emailMessage.Subject);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>Costruisce filtro ricerca email combinando subject filters (OR logic).</summary>
        protected virtual SearchFilter BuildSearchFilter()
        {
            var filters = _config.SubjectFilters
                .Select(f => new SearchFilter.ContainsSubstring(ItemSchema.Subject, f))
                .Cast<SearchFilter>()
                .ToList();

            return new SearchFilter.SearchFilterCollection(LogicalOperator.Or, filters);
        }

        /// <summary>Hook invocato quando nessuna email viene trovata. Override per notifiche custom.</summary>
        protected virtual System.Threading.Tasks.Task OnNoEmailsFoundAsync(CancellationToken ct)
        {
            _logger.LogInformation("Nessuna email trovata da processare");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #endregion
    }
}
