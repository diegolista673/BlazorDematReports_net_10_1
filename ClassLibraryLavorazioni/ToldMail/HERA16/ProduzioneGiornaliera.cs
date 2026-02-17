using LibraryLavorazioni.Utility.Interfaces;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.VisualBasic;
using NLog;
using System.Data;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Task = System.Threading.Tasks.Task;

namespace LibraryLavorazioni.LavorazioniViaMail.HERA16
{
    // Classe principale per la gestione della produzione giornaliera
    // Include metodi per gestire allegati, inviare email, e aggiornare database
    public class ProduzioneGiornaliera
    {
        // Logger per registrare informazioni e errori
        private readonly Logger logger;

        // Configurazione per la gestione delle lavorazioni
        private readonly ILavorazioniConfigManager lavorazioniConfigManager;

        // Servizio per la gestione delle email
        private ExchangeService service;

        // Percorso per salvare i file di report
        private string pathFile = AppDomain.CurrentDomain.BaseDirectory + @"\report";

        // Percorso per salvare i file di archivio
        private string pathFileArchive = AppDomain.CurrentDomain.BaseDirectory + @"\archive";

        // Costruttore per inizializzare la classe
        public ProduzioneGiornaliera(ILavorazioniConfigManager _lavorazioniConfigManager)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            lavorazioniConfigManager = _lavorazioniConfigManager;
            service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
        }

        // Metodo per ottenere allegati dalle email
        public async Task GetAttachmentsAsync()
        {
            // Configurazione delle credenziali per il servizio Exchange
            service.Credentials = new WebCredentials("verona.edp", "200902hope*", "postel.it");
            service.Url = new Uri("https://postaweb.postel.it/ews/exchange.asmx");

            // Filtro per cercare email con un determinato oggetto
            List<SearchFilter> SearchFilterCollection = new List<SearchFilter>();
            SearchFilterCollection.Add(new SearchFilter.ContainsSubstring(ItemSchema.Subject, "DEMAT_HERA16"));

            // Creazione del filtro di ricerca
            SearchFilter searchFilter = new SearchFilter.SearchFilterCollection(LogicalOperator.Or, SearchFilterCollection.ToArray());

            // Configurazione della vista per ottenere le email
            ItemView view = new ItemView(100, 0, OffsetBasePoint.Beginning);
            view.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Ascending);
            view.PropertySet = new PropertySet(BasePropertySet.IdOnly, ItemSchema.Subject, ItemSchema.DateTimeReceived);

            // Ricerca delle email nella casella di posta
            FindItemsResults<Item> results = service.FindItems(WellKnownFolderName.Inbox, searchFilter, view);

            if (results != null)
            {
                if (results.TotalCount == 0)
                {
                    logger.Info("No email received");
                    MailHera16 MailHera16 = new MailHera16();
                    MailHera16.Pervenuta = false;
                    InviaMail(MailHera16.SubjectAnswer, MailHera16.BodyAnswer);
                }
                else
                {
                    foreach (Item item in results.Items)
                    {
                        if (item is EmailMessage)
                        {
                            MailHera16 MailHera16 = new MailHera16();

                            // Filtro per email con oggetto specifico
                            if (item.Subject.Contains("Report produzione giornaliera"))
                            {
                                item.Load(new PropertySet(EmailMessageSchema.Attachments, EmailMessageSchema.Subject, EmailMessageSchema.DateTimeReceived, EmailMessageSchema.Body));

                                MailHera16.Body = item.Body.Text;
                                MailHera16.IDEvento = findBodyText(MailHera16.Body, "Identificativo evento:");
                                MailHera16.DataRiferimento = Convert.ToDateTime(findBodyText(MailHera16.Body, "Periodo di riferimento:"));
                                MailHera16.Oggetto = item.Subject;
                                MailHera16.DataRicezione = item.DateTimeReceived;
                                MailHera16.Pervenuta = true;

                                // Gestione degli allegati
                                if (item.Attachments.Count > 0)
                                {
                                    foreach (Microsoft.Exchange.WebServices.Data.Attachment attachment in item.Attachments)
                                    {
                                        if (attachment is FileAttachment)
                                        {
                                            FileAttachment fileAttachment = (FileAttachment)attachment;
                                            var pathFullFileName = Path.Combine(pathFile, attachment.Name);
                                            fileAttachment.Load(pathFullFileName);

                                            MailHera16.AllegatoPresente = true;
                                            MailHera16.Allegati.Add(fileAttachment.Name);

                                            logger.Info("Read a file attachment with a name = " + fileAttachment.Name + " - email del : " + item.DateTimeReceived);

                                            // Filtro per nome allegato
                                            if (fileAttachment.Name.Contains("file di produzione giornaliera"))
                                            {
                                                var table = LeggiCsv(pathFullFileName);

                                                if (table.Rows.Count > 0)
                                                {
                                                    // Inserimento dati nella tabella
                                                    UpdateDatatableProduzione(table, fileAttachment.Name);
                                                    await CopyToSQLTableAsync(table, fileAttachment.Name, lavorazioniConfigManager.CnxnHera!);
                                                    MailHera16.Esito = true;
                                                    logger.Info("tabella HERA16 aggiornata");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            logger.Error("Errore sul tipo di attachment EWS");
                                            throw new Exception("Errore sul tipo di attachment EWS");
                                        }
                                    }
                                }
                                else
                                {
                                    MailHera16.AllegatoPresente = false;
                                    MailHera16.Esito = true;
                                    logger.Info("Nessun allegato alla mail :" + MailHera16.Oggetto + " - ricevuta il : " + MailHera16.DataRicezione);
                                }

                                InviaMail(MailHera16.SubjectAnswer, MailHera16.BodyAnswer);
                            }

                            CopiaEmail((EmailMessage)item);
                            SpostaEmail((EmailMessage)item);
                        }
                        else
                        {
                            logger.Error("Errore sul tipo di EMAIL EWS");
                            throw new Exception("Errore sul tipo di EMAIL EWS");
                        }
                    }
                }
            }
            else
            {
                logger.Info("Nessuna email");
            }
        }

        // Metodo per aggiornare la tabella con informazioni aggiuntive
        private DataTable UpdateDatatableProduzione(DataTable dt, string nomeFile)
        {
            var dataCaricamento = DateTime.Now;
            DataColumn newColumn = new DataColumn("nome_file", typeof(string));
            newColumn.DefaultValue = nomeFile;
            dt.Columns.Add(newColumn);

            DataColumn newColumn1 = new DataColumn("data_caricamento_file", typeof(DateTime));
            newColumn1.DefaultValue = dataCaricamento;
            dt.Columns.Add(newColumn1);

            foreach (DataRow r in dt.AsEnumerable())
            {
                if (!CheckValidData(r["data_scansione"].ToString()!))
                    r["data_scansione"] = null;

                if (!CheckValidData(r["data_classificazione"].ToString()!))
                    r["data_classificazione"] = null;

                if (!CheckValidData(r["data_index"].ToString()!))
                    r["data_index"] = null;

                if (!CheckValidData(r["data_pubblicazione"].ToString()!))
                    r["data_pubblicazione"] = null;
            }

            return dt;
        }

        // Metodo per verificare la validità di una data
        private bool CheckValidData(string record)
        {
            bool res = false;
            if (Information.IsDate(record))
                res = true;

            return res;
        }

        // Metodo per copiare dati in una tabella SQL
        private async Task CopyToSQLTableAsync(DataTable dt, string nomeFile, string cnxn)
        {
            try
            {
                Microsoft.Data.SqlClient.SqlBulkCopy bulkCopy;
                bulkCopy = new Microsoft.Data.SqlClient.SqlBulkCopy(cnxn, Microsoft.Data.SqlClient.SqlBulkCopyOptions.TableLock);

                using (bulkCopy)
                {
                    bulkCopy.DestinationTableName = "HERA16";
                    bulkCopy.ColumnMappings.Add("codice_mercato", "codice_mercato");
                    bulkCopy.ColumnMappings.Add("codice_offerta", "codice_offerta");
                    bulkCopy.ColumnMappings.Add("tipo_documento", "tipo_documento");

                    bulkCopy.ColumnMappings.Add("data_scansione", "data_scansione");
                    bulkCopy.ColumnMappings.Add("operatore_scan", "operatore_scan");

                    bulkCopy.ColumnMappings.Add("data_classificazione", "data_classificazione");
                    bulkCopy.ColumnMappings.Add("operatore_classificazione", "operatore_classificazione");

                    bulkCopy.ColumnMappings.Add("data_index", "data_index");
                    bulkCopy.ColumnMappings.Add("operatore_index", "operatore_index");

                    bulkCopy.ColumnMappings.Add("data_pubblicazione", "data_pubblicazione");
                    bulkCopy.ColumnMappings.Add("codice_scatola", "codice_scatola");
                    bulkCopy.ColumnMappings.Add("progr_scansione", "progr_scansione");

                    bulkCopy.ColumnMappings.Add("nome_file", "nome_file");
                    bulkCopy.ColumnMappings.Add("data_caricamento_file", "data_caricamento_file");
                    bulkCopy.ColumnMappings.Add("identificativo_allegato", "identificativo_allegato");

                    bulkCopy.BatchSize = dt.Rows.Count;
                    await bulkCopy.WriteToServerAsync(dt);
                }

                logger.Info("file inserito in tabella HERA16 :" + nomeFile);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw new Exception(ex.Message);
            }
        }

        // Metodo per trovare testo nel corpo di una email
        private string findBodyText(string bodyText, string searchString)
        {
            string searchedText = "";
            string regexPattern = searchString + @"\s*(\S+)";
            Match regexResults = Regex.Match(bodyText, regexPattern);
            if (regexResults.Success)
                searchedText = regexResults.Groups[1].ToString();

            return searchedText;
        }

        // Metodo per leggere un file CSV e caricarlo in una DataTable
        private DataTable LeggiCsv(string pathFile)
        {
            char quotingCharacter = '\0';  // Nessun carattere di quotatura
            char escapeCharacter = '\0';
            char commentCharacter = '\0';
            char delimiter = ';';
            bool hasHeader = true;

            DataTable csvTable = new DataTable();

            using (var reader = new CsvReader(new StreamReader(pathFile), hasHeader, delimiter, quotingCharacter, escapeCharacter, commentCharacter, ValueTrimmingOptions.All))
            {
                int fieldCount = reader.FieldCount;
                string[] headers = reader.GetFieldHeaders();

                foreach (string headerLabel in headers)
                {
                    csvTable.Columns.Add(headerLabel, typeof(string));
                }

                while (reader.ReadNextRecord())
                {
                    DataRow newRow = csvTable.NewRow();

                    for (int i = 0; i <= fieldCount - 1; i++)
                    {
                        newRow[i] = reader[i];
                    }

                    csvTable.Rows.Add(newRow);
                }
            }

            return csvTable;
        }

        // Metodo per spostare una email in una cartella dedicata
        private void SpostaEmail(EmailMessage email)
        {
            var viewF = new FolderView(1);
            viewF.Traversal = FolderTraversal.Deep;

            var filter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, "HERA16");

            var resultsFolder = service.FindFolders(WellKnownFolderName.Root, filter, viewF);
            if (resultsFolder.TotalCount < 1)
                throw new Exception("Cannot find HERA16 folder");
            else
                foreach (Folder f in resultsFolder)
                {
                    if (f.DisplayName == "HERA16")
                    {
                        email.IsRead = true;
                        email.Update(ConflictResolutionMode.AutoResolve);
                        email.Move(f.Id);
                        logger.Info("La mail del : " + email.DateTimeReceived + " è stata spostata da Inbox a " + f.DisplayName);
                    }
                }
        }

        // Metodo per copiare una email in una cartella dedicata
        private void CopiaEmail(EmailMessage email)
        {
            int i = 1;
            try
            {
                while (true)
                {
                    email.Subject = RemoveSpecialChars(email.Subject);
                    var dest = pathFileArchive + @"\" + email.Subject + "_" + i + ".eml";
                    if (File.Exists(dest))
                    {
                        i++;
                        continue;
                    }
                    else
                    {
                        email.Load(new PropertySet(EmailMessageSchema.Attachments, EmailMessageSchema.Subject, EmailMessageSchema.DateTimeReceived, EmailMessageSchema.MimeContent));
                        MimeContent mc = email.MimeContent;
                        FileStream fs = new FileStream(dest, FileMode.Create);

                        fs.Write(mc.Content, 0, mc.Content.Length);
                        fs.Close();

                        logger.Info("La mail del : " + email.DateTimeReceived + " è stata copiata da Inbox a " + pathFileArchive);

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw new Exception("Errore copia in archive file email : " + ex.Message);
            }
        }

        // Metodo per inviare una email
        public void InviaMail(string subjectAnswer, string bodyAnswer)
        {
            try
            {
                ExchangeService service = new Microsoft.Exchange.WebServices.Data.ExchangeService(ExchangeVersion.Exchange2013_SP1)
                {
                    Credentials = new WebCredentials("verona.edp", "200902hope*", "postel.it")
                };

                service.Url = new Uri("https://postaweb.postel.it/ews/exchange.asmx");

                EmailMessage msg = new EmailMessage(service);
                msg.ToRecipients.Add("diego.lista@postel.it");
                msg.ToRecipients.Add("diego.lista@posteitaliane.it");
                msg.Subject = subjectAnswer;
                msg.Body = bodyAnswer;

                msg.Send();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        // Metodo per creare un file zip
        public void CreaZip()
        {
            var filePaths = Directory.GetFiles(pathFileArchive, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".eml", StringComparison.OrdinalIgnoreCase));

            if (filePaths.Any())
            {
                try
                {
                    int i = 1;
                    while (true)
                    {
                        var dest = pathFileArchive + @"\" + DateTime.Today.ToString("yyyyMMdd") + "_" + i + ".zip";

                        if (File.Exists(dest))
                        {
                            i++;
                            continue;
                        }
                        else
                        {
                            using (ZipArchive archive = ZipFile.Open(dest, ZipArchiveMode.Create))
                            {
                                foreach (var fPath in filePaths)
                                {
                                    archive.CreateEntryFromFile(fPath, Path.GetFileName(fPath));
                                }
                            }
                            logger.Info("filezip creato");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                    throw new Exception("Errore creazione file zip : " + ex.Message);
                }
            }
        }

        // Metodo per svuotare una cartella
        private void ClearFolder()
        {
            string[] files = Directory.GetFiles(pathFile);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        // Metodo per svuotare una cartella di file .eml
        private void ClearFolderEml()
        {
            var filePaths = Directory.GetFiles(pathFileArchive, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".eml", StringComparison.OrdinalIgnoreCase));
            foreach (string file in filePaths)
            {
                File.Delete(file);
            }
        }

        // Metodo per rimuovere caratteri speciali da una stringa
        public string RemoveSpecialChars(string input)
        {
            return Regex.Replace(input, @"[^0-9a-zA-Z _]+", string.Empty);
        }

        // Metodo per elaborare allegati
        public async Task CaricaAllegato()
        {
            try
            {
                bool exists = Directory.Exists(pathFile);
                if (!exists)
                    Directory.CreateDirectory(pathFile);

                bool existsArchive = Directory.Exists(pathFileArchive);
                if (!existsArchive)
                    Directory.CreateDirectory(pathFileArchive);

                await GetAttachmentsAsync();
                CreaZip();
                ClearFolder();
                ClearFolderEml();
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}
