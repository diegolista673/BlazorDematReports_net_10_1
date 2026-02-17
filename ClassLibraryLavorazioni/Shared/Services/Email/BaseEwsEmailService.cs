using LumenWorks.Framework.IO.Csv;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni.Shared.Services.Email
{
    /// <summary>
    /// Classe base per gestione email Exchange Web Services con allegati.
    /// Fornisce funzionalità comuni per lettura email, download allegati, parsing CSV e archiviazione.
    /// </summary>
    public abstract class BaseEwsEmailService
    {
        private readonly ILogger _logger;
        private readonly EwsEmailServiceConfig _config;
        private ExchangeService? _exchangeService;

        /// <summary>
        /// Inizializza una nuova istanza di BaseEwsEmailService.
        /// </summary>
        /// <param name="config">Configurazione servizio email.</param>
        /// <param name="logger">Logger per registrazione eventi.</param>
        protected BaseEwsEmailService(EwsEmailServiceConfig config, ILogger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            EnsureDirectoriesExist();
        }

        /// <summary>
        /// Configurazione servizio email (accessibile da classi derivate).
        /// </summary>
        protected EwsEmailServiceConfig Config => _config;

        /// <summary>
        /// Logger (accessibile da classi derivate).
        /// </summary>
        protected ILogger Logger => _logger;

        #region Exchange Service Initialization

        /// <summary>
        /// Inizializza il servizio Exchange Web Services con credenziali configurate.
        /// </summary>
        protected virtual void InitializeExchangeService()
        {
            _logger.LogInformation(
                "Inizializzazione servizio Exchange: URL={ExchangeUrl}, User={Username}, Domain={Domain}",
                _config.ExchangeUrl,
                _config.Username,
                _config.Domain
            );

            _exchangeService = new ExchangeService(_config.ExchangeVersion)
            {
                Credentials = new WebCredentials(_config.Username, _config.Password, _config.Domain),
                Url = _config.ExchangeUrl
            };

            _logger.LogInformation("Servizio Exchange inizializzato con successo");
        }

        /// <summary>
        /// Ottiene il servizio Exchange (inizializza se necessario).
        /// </summary>
        protected ExchangeService GetExchangeService()
        {
            if (_exchangeService == null)
            {
                InitializeExchangeService();
            }

            return _exchangeService!;
        }

        #endregion

        #region Email Search and Processing

        /// <summary>
        /// Cerca e processa email nella inbox secondo i filtri configurati.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Risultato batch elaborazione email.</returns>
        public async System.Threading.Tasks.Task<BatchEmailProcessingResult> ProcessEmailsAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Inizio elaborazione email batch");

            var service = GetExchangeService();
            var searchFilter = BuildSearchFilter();
            var itemView = new ItemView(_config.MaxEmailsPerRun, 0, OffsetBasePoint.Beginning)
            {
                PropertySet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.Subject, ItemSchema.DateTimeReceived)
            };
            itemView.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Ascending);

            FindItemsResults<Item> searchResults;
            try
            {
                searchResults = service.FindItems(WellKnownFolderName.Inbox, searchFilter, itemView);
                _logger.LogInformation("Trovate {Count} email matching filtri", searchResults.TotalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore ricerca email in Inbox");
                throw;
            }

            if (searchResults.TotalCount == 0)
            {
                _logger.LogInformation("Nessuna email da processare");
                await OnNoEmailsFoundAsync(ct);
                return new BatchEmailProcessingResult { TotalEmailsFound = 0 };
            }

            var successfulEmails = new List<EmailProcessingResult>();
            var failedEmails = new List<EmailProcessingResult>();

            foreach (var item in searchResults.Items)
            {
                ct.ThrowIfCancellationRequested();

                if (item is not EmailMessage emailMessage)
                {
                    _logger.LogWarning("Item {Subject} non è EmailMessage, skip", item.Subject);
                    continue;
                }

                try
                {
                    var result = await ProcessSingleEmailAsync(emailMessage, ct);
                    
                    if (result.Success)
                    {
                        successfulEmails.Add(result);
                        _logger.LogInformation(
                            "Email processata con successo: {Subject}, Allegati: {Count}",
                            result.Subject,
                            result.Attachments.Count
                        );
                    }
                    else
                    {
                        failedEmails.Add(result);
                        _logger.LogWarning(
                            "Email processata con errori: {Subject}, Errore: {Error}",
                            result.Subject,
                            result.ErrorMessage
                        );
                    }

                    // Sposta email in cartella archivio
                    await MoveEmailToArchiveFolderAsync(emailMessage, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore elaborazione email {Subject}", emailMessage.Subject);
                    failedEmails.Add(new EmailProcessingResult
                    {
                        Subject = emailMessage.Subject ?? "Unknown",
                        ReceivedDate = emailMessage.DateTimeReceived,
                        Success = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            // Crea zip allegati se configurato
            string? zipPath = null;
            if (_config.CreateZipArchive && successfulEmails.Any(e => e.HasAttachments))
            {
                zipPath = await CreateZipArchiveAsync(ct);
            }

            // Cleanup allegati locali se configurato
            if (_config.CleanupAfterProcessing)
            {
                CleanupLocalAttachments();
            }

            var batchResult = new BatchEmailProcessingResult
            {
                TotalEmailsFound = searchResults.TotalCount,
                SuccessfulEmails = successfulEmails,
                FailedEmails = failedEmails,
                ZipArchivePath = zipPath
            };

            _logger.LogInformation(
                "Elaborazione batch completata: Totali={Total}, Successi={Success}, Errori={Failed}, Allegati={Attachments}",
                batchResult.TotalEmailsFound,
                successfulEmails.Count,
                failedEmails.Count,
                batchResult.TotalAttachmentsDownloaded
            );

            return batchResult;
        }

        /// <summary>
        /// Processa singola email con allegati.
        /// </summary>
        /// <param name="emailMessage">Email da processare.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Risultato elaborazione email.</returns>
        protected virtual async System.Threading.Tasks.Task<EmailProcessingResult> ProcessSingleEmailAsync(
            EmailMessage emailMessage, 
            CancellationToken ct)
        {
            // Load full email properties
            emailMessage.Load(new PropertySet(
                EmailMessageSchema.Attachments,
                EmailMessageSchema.Subject,
                EmailMessageSchema.DateTimeReceived,
                EmailMessageSchema.Body
            ));

            var bodyText = emailMessage.Body?.Text ?? string.Empty;
            var attachments = new List<AttachmentInfo>();
            var extractedMetadata = new Dictionary<string, string>();

            // Estrai metadata da body (implementazione personalizzabile)
            ExtractMetadataFromBody(bodyText, extractedMetadata);

            // Marca email come letta
            emailMessage.IsRead = true;
            emailMessage.Update(ConflictResolutionMode.AutoResolve);

            if (emailMessage.Attachments.Count == 0)
            {
                _logger.LogInformation("Email {Subject} senza allegati", emailMessage.Subject);
                return new EmailProcessingResult
                {
                    Subject = emailMessage.Subject ?? string.Empty,
                    ReceivedDate = emailMessage.DateTimeReceived,
                    Body = bodyText,
                    Success = true,
                    ExtractedMetadata = extractedMetadata
                };
            }

            // Scarica allegati
            foreach (var attachment in emailMessage.Attachments)
            {
                if (attachment is not FileAttachment fileAttachment)
                {
                    _logger.LogWarning("Attachment {Name} non è FileAttachment, skip", attachment.Name);
                    continue;
                }

                try
                {
                    var attachmentInfo = await DownloadAttachmentAsync(fileAttachment, ct);
                    attachments.Add(attachmentInfo);

                    // Processa allegato se necessario (es. parsing CSV)
                    if (attachmentInfo.MatchesPattern)
                    {
                        await ProcessAttachmentAsync(attachmentInfo, extractedMetadata, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore download allegato {Name}", fileAttachment.Name);
                }
            }

            return new EmailProcessingResult
            {
                Subject = emailMessage.Subject ?? string.Empty,
                ReceivedDate = emailMessage.DateTimeReceived,
                Body = bodyText,
                Success = true,
                Attachments = attachments,
                ExtractedMetadata = extractedMetadata
            };
        }

        #endregion

        #region Attachment Handling

        /// <summary>
        /// Scarica singolo allegato file e salva localmente.
        /// </summary>
        /// <param name="fileAttachment">Allegato da scaricare.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Informazioni allegato scaricato.</returns>
        protected virtual async System.Threading.Tasks.Task<AttachmentInfo> DownloadAttachmentAsync(
            FileAttachment fileAttachment, 
            CancellationToken ct)
        {
            var fileName = fileAttachment.Name;
            var localPath = Path.Combine(_config.LocalAttachmentPath, fileName);

            _logger.LogDebug("Download allegato {FileName} -> {LocalPath}", fileName, localPath);

            // Load attachment content
            await System.Threading.Tasks.Task.Run(() => fileAttachment.Load(localPath), ct);

            var fileInfo = new FileInfo(localPath);
            var matchesPattern = MatchesAnyPattern(fileName);

            _logger.LogInformation(
                "Allegato scaricato: {FileName}, Size={Size} bytes, MatchesPattern={Matches}",
                fileName,
                fileInfo.Length,
                matchesPattern
            );

            return new AttachmentInfo
            {
                FileName = fileName,
                LocalFilePath = localPath,
                FileSizeBytes = fileInfo.Length,
                MatchesPattern = matchesPattern,
                ContentType = fileAttachment.ContentType
            };
        }

        /// <summary>
        /// Verifica se nome file corrisponde a pattern configurati.
        /// </summary>
        protected virtual bool MatchesAnyPattern(string fileName)
        {
            if (_config.AttachmentPatterns == null || !_config.AttachmentPatterns.Any())
                return true; // No filter = match all

            foreach (var pattern in _config.AttachmentPatterns)
            {
                // Supporta wildcard pattern (es. "*.csv", "file*")
                var regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processa allegato dopo download (es. parsing CSV, inserimento DB).
        /// Override in classi derivate per logica specifica.
        /// </summary>
        /// <param name="attachment">Allegato da processare.</param>
        /// <param name="metadata">Metadata estratti da email.</param>
        /// <param name="ct">Token di cancellazione.</param>
        protected virtual System.Threading.Tasks.Task ProcessAttachmentAsync(
            AttachmentInfo attachment, 
            Dictionary<string, string> metadata, 
            CancellationToken ct)
        {
            _logger.LogDebug("ProcessAttachmentAsync chiamato (no override implementation)");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #endregion

        #region CSV Parsing

        /// <summary>
        /// Legge file CSV e restituisce DataTable.
        /// </summary>
        /// <param name="csvFilePath">Path file CSV da leggere.</param>
        /// <param name="delimiter">Delimitatore colonne (default: ';').</param>
        /// <param name="hasHeader">Indica se CSV ha riga header (default: true).</param>
        /// <returns>DataTable con dati CSV.</returns>
        protected virtual DataTable ReadCsvFile(
            string csvFilePath, 
            char delimiter = ';', 
            bool hasHeader = true)
        {
            _logger.LogDebug("Lettura file CSV: {FilePath}, Delimiter={Delimiter}", csvFilePath, delimiter);

            var dataTable = new DataTable();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using var reader = new CsvReader(
                    new StreamReader(csvFilePath),
                    hasHeader,
                    delimiter,
                    quote: '\0',
                    escape: '\0',
                    comment: '\0',
                    ValueTrimmingOptions.All
                );

                int fieldCount = reader.FieldCount;
                string[] headers = reader.GetFieldHeaders();

                // Crea colonne DataTable
                foreach (string header in headers)
                {
                    dataTable.Columns.Add(header, typeof(string));
                }

                // Leggi righe CSV
                while (reader.ReadNextRecord())
                {
                    DataRow newRow = dataTable.NewRow();
                    for (int i = 0; i < fieldCount; i++)
                    {
                        newRow[i] = reader[i];
                    }
                    dataTable.Rows.Add(newRow);
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "CSV letto con successo: {RowCount} righe, {ColumnCount} colonne, Tempo={Elapsed}ms",
                    dataTable.Rows.Count,
                    dataTable.Columns.Count,
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Errore lettura CSV {FilePath}, Tempo={Elapsed}ms", csvFilePath, stopwatch.ElapsedMilliseconds);
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
        /// <param name="bodyText">Testo body email.</param>
        /// <param name="metadata">Dictionary da popolare con metadata estratti.</param>
        protected virtual void ExtractMetadataFromBody(string bodyText, Dictionary<string, string> metadata)
        {
            // Pattern comuni (override per pattern specifici)
            ExtractMetadataField(bodyText, "Identificativo evento:", metadata, "IdEvento");
            ExtractMetadataField(bodyText, "Periodo di riferimento:", metadata, "DataRiferimento");
            ExtractMetadataField(bodyText, "Data evento:", metadata, "DataEvento");
        }

        /// <summary>
        /// Estrae singolo campo metadata da body text.
        /// </summary>
        protected void ExtractMetadataField(
            string bodyText, 
            string searchPattern, 
            Dictionary<string, string> metadata, 
            string metadataKey)
        {
            if (string.IsNullOrWhiteSpace(bodyText) || string.IsNullOrWhiteSpace(searchPattern))
                return;

            string regexPattern = Regex.Escape(searchPattern) + @"\s*(\S+)";
            Match match = Regex.Match(bodyText, regexPattern);

            if (match.Success)
            {
                string value = match.Groups[1].Value;
                metadata[metadataKey] = value;
                _logger.LogDebug("Metadata estratto: {Key}={Value}", metadataKey, value);
            }
        }

        #endregion

        #region Archive Management

        /// <summary>
        /// Sposta email in cartella archivio Exchange.
        /// </summary>
        protected virtual async System.Threading.Tasks.Task MoveEmailToArchiveFolderAsync(EmailMessage emailMessage, CancellationToken ct)
        {
            try
            {
                var service = GetExchangeService();
                var folderView = new FolderView(1) { Traversal = FolderTraversal.Deep };
                var filter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, _config.ArchiveFolderName);

                var folderResults = service.FindFolders(WellKnownFolderName.Root, filter, folderView);

                if (folderResults.TotalCount == 0)
                {
                    _logger.LogWarning("Cartella archivio {FolderName} non trovata", _config.ArchiveFolderName);
                    return;
                }

                var archiveFolder = folderResults.Folders.FirstOrDefault(f => f.DisplayName == _config.ArchiveFolderName);
                if (archiveFolder != null)
                {
                    await System.Threading.Tasks.Task.Run(() => emailMessage.Move(archiveFolder.Id), ct);
                    _logger.LogInformation(
                        "Email {Subject} spostata in cartella {Folder}",
                        emailMessage.Subject,
                        _config.ArchiveFolderName
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore spostamento email {Subject} in archivio", emailMessage.Subject);
            }
        }

        /// <summary>
        /// Crea archivio zip con allegati scaricati.
        /// </summary>
        protected virtual async System.Threading.Tasks.Task<string> CreateZipArchiveAsync(CancellationToken ct)
        {
            var zipFileName = $"{DateTime.Today:yyyyMMdd}.zip";
            var zipFilePath = Path.Combine(_config.LocalArchivePath, zipFileName);

            _logger.LogInformation("Creazione archivio zip: {ZipPath}", zipFilePath);

            try
            {
                if (File.Exists(zipFilePath))
                {
                    File.Delete(zipFilePath);
                }

                await System.Threading.Tasks.Task.Run(() => 
                    ZipFile.CreateFromDirectory(_config.LocalAttachmentPath, zipFilePath), 
                    ct
                );

                var fileInfo = new FileInfo(zipFilePath);
                _logger.LogInformation("Archivio zip creato: {ZipPath}, Size={Size} bytes", zipFilePath, fileInfo.Length);

                return zipFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore creazione archivio zip {ZipPath}", zipFilePath);
                throw;
            }
        }

        /// <summary>
        /// Elimina allegati locali temporanei.
        /// </summary>
        protected virtual void CleanupLocalAttachments()
        {
            try
            {
                var files = Directory.GetFiles(_config.LocalAttachmentPath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                _logger.LogInformation("Cleanup allegati locali completato: {Count} file eliminati", files.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore cleanup allegati locali");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Costruisce filtro ricerca email combinando subject filters configurati (OR logic).
        /// </summary>
        protected virtual SearchFilter BuildSearchFilter()
        {
            var filters = _config.SubjectFilters
                .Select(filter => new SearchFilter.ContainsSubstring(ItemSchema.Subject, filter))
                .Cast<SearchFilter>()
                .ToList();

            return new SearchFilter.SearchFilterCollection(LogicalOperator.Or, filters);
        }

        /// <summary>
        /// Assicura esistenza directory locali per allegati e archivio.
        /// </summary>
        protected virtual void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_config.LocalAttachmentPath))
            {
                Directory.CreateDirectory(_config.LocalAttachmentPath);
                _logger.LogInformation("Directory allegati creata: {Path}", _config.LocalAttachmentPath);
            }

            if (!Directory.Exists(_config.LocalArchivePath))
            {
                Directory.CreateDirectory(_config.LocalArchivePath);
                _logger.LogInformation("Directory archivio creata: {Path}", _config.LocalArchivePath);
            }
        }

        /// <summary>
        /// Hook invocato quando nessuna email viene trovata.
        /// Override per implementare notifiche custom (es. invio email alert).
        /// </summary>
        protected virtual System.Threading.Tasks.Task OnNoEmailsFoundAsync(CancellationToken ct)
        {
            _logger.LogInformation("OnNoEmailsFoundAsync: nessuna azione default implementata");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        #endregion
    }
}
