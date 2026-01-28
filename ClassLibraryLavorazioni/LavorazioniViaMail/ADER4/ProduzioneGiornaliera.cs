using Entities.Models.DbApplication;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO.Compression;
using System.Text.RegularExpressions;


namespace WorkerServiceAderEquitalia4
{
    /// <summary>
    /// Gestisce l'elaborazione della produzione giornaliera per Equitalia 4.
    /// Include lettura email Exchange, elaborazione allegati CSV e inserimento dati in database.
    /// </summary>
    public class ProduzioneGiornaliera
    {
        private readonly ILogger<ProduzioneGiornaliera> _logger;
        private readonly IDbContextFactory<DematReportsContext> _context;

        /// <summary>
        /// Enumerazione per identificare la destinazione dei dati (Verona o Genova).
        /// </summary>
        public enum Destinazione
        {
            /// <summary>
            /// Destinazione Verona.
            /// </summary>
            verona,
            /// <summary>
            /// Destinazione Genova.
            /// </summary>
            genova
        }


        private Destinazione destinazione;
        private ExchangeService? service;
        private string pathFile = AppDomain.CurrentDomain.BaseDirectory + @"\report";
        private string pathFileArchive = AppDomain.CurrentDomain.BaseDirectory + @"\archive";

        /// <summary>
        /// Inizializza una nuova istanza di ProduzioneGiornaliera.
        /// </summary>
        /// <param name="logger">Logger per registrare eventi e errori.</param>
        /// <param name="context">Factory per la creazione di contesti database.</param>
        public ProduzioneGiornaliera(ILogger<ProduzioneGiornaliera> logger,
                                     IDbContextFactory<DematReportsContext> context)
        {
            _logger = logger;
            _context = context;


        }

        /// <summary>
        /// Ottiene e elabora gli allegati dalle email Equitalia 4.
        /// Configura il servizio Exchange, cerca email specifiche e processa gli allegati CSV.
        /// </summary>
        public void GetAttachments()
        {
            //ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);
            service.Credentials = new WebCredentials("verona.edp", "200902hope*", "postel.it");
            //service.AutodiscoverUrl("edp.vr@postel.it");
            service.Url = new Uri("https://postaweb.postel.it/ews/exchange.asmx");

            List<SearchFilter> SearchFilterCollection = new List<SearchFilter>();
            SearchFilterCollection.Add(new SearchFilter.ContainsSubstring(ItemSchema.Subject, "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)"));
            SearchFilterCollection.Add(new SearchFilter.ContainsSubstring(ItemSchema.Subject, "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)"));

            // Dim stringSearched = "Data evento: " & DateString
            // SearchFilterCollection.Add(New SearchFilter.ContainsSubstring(ItemSchema.Body, stringSearched))

            // Create the search filter.
            SearchFilter searchFilter = new SearchFilter.SearchFilterCollection(LogicalOperator.Or, SearchFilterCollection.ToArray());

            ItemView view = new ItemView(100, 0, OffsetBasePoint.Beginning);
            view.OrderBy.Add(ItemSchema.DateTimeReceived, SortDirection.Ascending);
            view.PropertySet = (new PropertySet(BasePropertySet.IdOnly, ItemSchema.Subject, ItemSchema.DateTimeReceived));

            // trova tutte le mail. 
            FindItemsResults<Item> results = service.FindItems(WellKnownFolderName.Inbox, searchFilter, view);

            if (results != null)
            {
                if (results.TotalCount == 0)
                {
                    _logger.LogInformation("No email received");
                    MailEquitalia4 mailEquitalia = new MailEquitalia4();
                    mailEquitalia.Pervenuta = false;
                    InviaMail(mailEquitalia.SubjectAnswer, mailEquitalia.BodyAnswer);
                }
                else
                    foreach (Item item in results.Items)
                    {
                        MailEquitalia4 mailEquitalia = new MailEquitalia4();

                        if (item.Subject == "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Verona)")
                        {
                            destinazione = Destinazione.verona;
                        }

                        if (item.Subject == "DEMAT_EQTMN4@RMHRPRD0 - Report di produzione (Genova)")
                        {
                            destinazione = Destinazione.genova;
                        }


                        if (item is EmailMessage)
                        {
                            EmailMessage email = (EmailMessage)item;
                            email.Load(new PropertySet(EmailMessageSchema.Attachments, EmailMessageSchema.Subject, EmailMessageSchema.DateTimeReceived, EmailMessageSchema.Body));

                            mailEquitalia.Body = item.Body?.Text ?? string.Empty;
                            mailEquitalia.IDEvento = findBodyText(mailEquitalia.Body, "Identificativo evento:");
                            mailEquitalia.DataRiferimento = Convert.ToDateTime(findBodyText(mailEquitalia.Body, "Periodo di riferimento:"));
                            mailEquitalia.Oggetto = email.Subject ?? string.Empty;
                            mailEquitalia.DataRicezione = email.DateTimeReceived;
                            mailEquitalia.Pervenuta = true;

                            // allegato presente
                            if ((email.Attachments.Count > 0))
                            {
                                foreach (Microsoft.Exchange.WebServices.Data.Attachment attachment in email.Attachments)
                                {
                                    if ((attachment is FileAttachment))
                                    {
                                        FileAttachment fileAttachment = (FileAttachment)attachment;
                                        fileAttachment.Load();

                                        var fileName = fileAttachment.Name;
                                        _logger.LogInformation("Read a file attachment with a name = " + fileName + " - email del : " + email.DateTimeReceived);

                                        mailEquitalia.AllegatoPresente = true;
                                        mailEquitalia.Allegati.Add(fileName);

                                        var pathFullFileName = Path.Combine(pathFile, fileName);
                                        fileAttachment.Load(pathFullFileName);

                                        if (fileName.Contains("EQTMN4_Dispacci_Preaccettati") | fileName.Contains("EQTMN4_Dispacci_Ripartiti") | fileName.Contains("EQTMN4_Scatole_Scansionate") | fileName.Contains("EQTMN4_Scatole_Restituite"))
                                        {
                                            var table = LeggiCsv(pathFullFileName);

                                            if (table.Rows.Count > 0)
                                            {

                                                if (fileName.Contains("EQTMN4_Dispacci_Preaccettati"))
                                                    mailEquitalia.PreAccettazione = GetTotaleCsv(table);

                                                if (fileName.Contains("EQTMN4_Dispacci_Ripartiti"))
                                                    mailEquitalia.Ripartizione = GetTotaleCsv(table);

                                                if (fileName.Contains("EQTMN4_Scatole_Restituite"))
                                                    mailEquitalia.Restituzione = GetTotaleCsv(table);

                                                if (fileName.Contains("EQTMN4_Scatole_Scansionate"))
                                                {
                                                    TotaleMailCSV totali = GetTotaleCsvScansionati(table);

                                                    mailEquitalia.ScansioneCaptiva = totali.ScansioneCaptiva;
                                                    mailEquitalia.ScansioneSorter = totali.ScansioneSorter;
                                                    mailEquitalia.ScartiScansioneSorter = totali.ScartiScansioneSorter;

                                                    mailEquitalia.ScansioneSorterBuste = totali.ScansioneSorterBuste;
                                                    mailEquitalia.ScartiScansioneSorterBuste = totali.ScartiScansioneSorterBuste;


                                                    InsertRecordsOperatori(table, mailEquitalia, destinazione);
                                                }

                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogError("Errore sul tipo di attachment EWS");
                                        throw new Exception("Errore sul tipo di attachment EWS");
                                    }
                                }

                                // insert totali in tabella produzione
                                InsertRecordsProduzione(mailEquitalia, destinazione);

                                // sposta email nella cartella EQUITALIA_4
                                SpostaEmail(email);
                            }
                            else
                            {
                                mailEquitalia.AllegatoPresente = false;
                                mailEquitalia.Esito = true;
                                _logger.LogInformation("Nessun allegato alla mail :" + mailEquitalia.Oggetto + " - ricevuta il : " + mailEquitalia.DataRicezione);
                                SpostaEmail(email);
                            }
                        }
                        else
                        {
                            _logger.LogError("Errore sul tipo di EMAIL EWS");
                            throw new Exception("Errore sul tipo di EMAIL EWS");
                        }

                        InviaMail(mailEquitalia.SubjectAnswer, mailEquitalia.BodyAnswer);
                    }
            }
            else
            {
                _logger.LogInformation("Nessuna email");
            }


        }



        /// <summary>
        /// Insert in tabella totali di produzione
        /// </summary>
        /// <param name="mailEquitalia"></param>
        /// <param name="destinazione"></param>
        private void InsertRecordsProduzione(MailEquitalia4 mailEquitalia, Destinazione destinazione)
        {
            try
            {
                using (var context = _context.CreateDbContext())
                {

                    if (destinazione == Destinazione.verona)
                    {
                        AderEquitalia4ProduzioneVr prod = new AderEquitalia4ProduzioneVr();
                        prod.PreAccettazione = mailEquitalia.PreAccettazione;
                        prod.Ripartizione = mailEquitalia.Ripartizione;
                        prod.ScansioneCaptiva = mailEquitalia.ScansioneCaptiva;
                        prod.ScansioneSorter = mailEquitalia.ScansioneSorter;
                        prod.ScartiScansioneSorter = mailEquitalia.ScartiScansioneSorter;
                        prod.ScansioneSorterBuste = mailEquitalia.ScansioneSorterBuste;
                        prod.ScartiScansioneSorterBuste = mailEquitalia.ScartiScansioneSorterBuste;
                        prod.Restituzione = mailEquitalia.Restituzione;
                        prod.DataLavorazione = mailEquitalia.DataRiferimento;
                        prod.IdEvento = mailEquitalia.IDEvento;

                        context.AderEquitalia4ProduzioneVrs.Add(prod);

                    }


                    if (destinazione == Destinazione.genova)
                    {
                        AderEquitalia4ProduzioneGe prod = new AderEquitalia4ProduzioneGe();
                        prod.PreAccettazione = mailEquitalia.PreAccettazione;
                        prod.Ripartizione = mailEquitalia.Ripartizione;
                        prod.ScansioneCaptiva = mailEquitalia.ScansioneCaptiva;
                        prod.ScansioneSorter = mailEquitalia.ScansioneSorter;
                        prod.ScartiScansioneSorter = mailEquitalia.ScartiScansioneSorter;
                        prod.ScansioneSorterBuste = mailEquitalia.ScansioneSorterBuste;
                        prod.ScartiScansioneSorterBuste = mailEquitalia.ScartiScansioneSorterBuste;
                        prod.Restituzione = mailEquitalia.Restituzione;
                        prod.DataLavorazione = mailEquitalia.DataRiferimento;
                        prod.IdEvento = mailEquitalia.IDEvento;

                        context.AderEquitalia4ProduzioneGes.Add(prod);
                    }


                    context.SaveChanges();

                }

                mailEquitalia.Esito = true;
            }
            catch (DbUpdateException ex)
            {
                var sqlexception = ex.InnerException?.InnerException as SqlException;
                if (sqlexception != null)
                {
                    if (sqlexception.Errors.OfType<SqlError>().Any(se => se.Number == 2601 || (se.Number == 2627)))
                    {
                        // Duplicate Key Exception
                        mailEquitalia.Esito = false;
                        mailEquitalia.MessaggioErrore = ex.InnerException?.InnerException?.Message?.ToString() ?? "Errore sconosciuto";
                        _logger.LogError(ex.InnerException?.InnerException?.Message?.ToString() ?? "Errore sconosciuto");
                    }
                }
            }
        }



        /// <summary>
        /// Insert in tabella operatori, suddivisi per tipologia scansione ( sorter prodelco / Captiva ) 
        /// Sorter Prodelco identificato tramite la stringa di 6 lettere all'interno del codice scatola ES: MN4C999X910000003197 => 999X91 = codice sorter 999X9 + numero sorter 1
        /// Sorter Prodelco identificato tramite la stringa di 6 lettere all'interno del codice scatola ES: MN4C999X930000003198 => 999X93 = codice sorter 999X9 + numero sorter 3 Buste
        /// Sorter Prodelco documenti = MN4 e sorter = 999X91 / 999X92
        /// Sorter Prodelco Buste = MN4 e sorter = 999X93
        /// Sorter Prodelco scarti != MN4 e sorter = 999X9
        /// Captiva != 999X9 
        /// Captiva documenti = tutto ciò che non è sorter e quindi != 999X9
        /// Tutto questo perchè ciò che è scarto sorter viene poi riscansionato in captiva 
        /// Per Fatturazione passiva conteggiare sorter senza scarti + documenti captiva
        /// </summary>
        /// <param name="table"></param>
        /// <param name="mailEquitalia"></param>
        /// <param name="destinazione"></param>
        private void InsertRecordsOperatori(DataTable table, MailEquitalia4 mailEquitalia, Destinazione destinazione)
        {
            try
            {
                var grpSorterBuste = from row in table.AsEnumerable()
                                     let codiceScatola = row.Field<string>("Codice Scatola")
                                     let postazione = row.Field<string>("Postazione")
                                     where !string.IsNullOrEmpty(codiceScatola) && codiceScatola.Length >= 10 &&
                                           codiceScatola.Substring(4, 6) == "999X93" &&
                                           !string.IsNullOrEmpty(postazione)
                                     group row by new
                                     {
                                         Operatore = postazione.ToLower()
                                     } into grp
                                     select new
                                     {
                                         Operatore = grp.Key.Operatore,

                                         DocumentiSorterBuste = grp.Where(r =>
                                         {
                                             var numeroDoc = r.Field<string>("Numero documenti");
                                             var codiceScatolaLocal = r.Field<string>("Codice Scatola");
                                             return !string.IsNullOrEmpty(numeroDoc) &&
                                                    int.TryParse(numeroDoc, out int dummy) &&
                                                    !string.IsNullOrEmpty(codiceScatolaLocal) &&
                                                    codiceScatolaLocal.Length >= 10 &&
                                                    codiceScatolaLocal.Substring(4, 6) == "999X93" &&
                                                    codiceScatolaLocal.Substring(0, 3) == "MN4";
                                         }).Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0")),

                                         ScartiSorterBuste = grp.Where(r =>
                                         {
                                             var numeroDoc = r.Field<string>("Numero documenti");
                                             var codiceScatolaLocal = r.Field<string>("Codice Scatola");
                                             return !string.IsNullOrEmpty(numeroDoc) &&
                                                    int.TryParse(numeroDoc, out int dummy) &&
                                                    !string.IsNullOrEmpty(codiceScatolaLocal) &&
                                                    codiceScatolaLocal.Length >= 10 &&
                                                    codiceScatolaLocal.Substring(4, 6) == "999X93" &&
                                                    codiceScatolaLocal.Substring(0, 3) != "MN4";
                                         }).Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"))
                                     };


                var grpSorter = from row in table.AsEnumerable()
                                let codiceScatola = row.Field<string>("Codice Scatola")
                                let postazione = row.Field<string>("Postazione")
                                where !string.IsNullOrEmpty(codiceScatola) && codiceScatola.Length >= 10 &&
                                      (codiceScatola.Substring(4, 6) == "999X91" || codiceScatola.Substring(4, 6) == "999X92") &&
                                      !string.IsNullOrEmpty(postazione)
                                group row by new
                                {
                                    Operatore = postazione.ToLower()
                                } into grp
                                select new
                                {
                                    Operatore = grp.Key.Operatore,

                                    DocumentiSorter = grp.Where(r =>
                                    {
                                        var numeroDoc = r.Field<string>("Numero documenti");
                                        var codiceScatolaLocal = r.Field<string>("Codice Scatola");
                                        return !string.IsNullOrEmpty(numeroDoc) &&
                                               int.TryParse(numeroDoc, out int dummy) &&
                                               !string.IsNullOrEmpty(codiceScatolaLocal) &&
                                               codiceScatolaLocal.Length >= 3 &&
                                               codiceScatolaLocal.Substring(0, 3) == "MN4";
                                    }).Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0")),

                                    ScartiSorter = grp.Where(r =>
                                    {
                                        var numeroDoc = r.Field<string>("Numero documenti");
                                        var codiceScatolaLocal = r.Field<string>("Codice Scatola");
                                        return !string.IsNullOrEmpty(numeroDoc) &&
                                               int.TryParse(numeroDoc, out int dummy) &&
                                               !string.IsNullOrEmpty(codiceScatolaLocal) &&
                                               codiceScatolaLocal.Length >= 3 &&
                                               codiceScatolaLocal.Substring(0, 3) != "MN4";
                                    }).Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"))
                                };



                var grpCaptiva = from row in table.AsEnumerable()
                                 let codiceScatola = row.Field<string>("Codice Scatola")
                                 let postazione = row.Field<string>("Postazione")
                                 where !string.IsNullOrEmpty(codiceScatola) && codiceScatola.Length >= 9 &&
                                       codiceScatola.Substring(4, 5) != "999X9" &&
                                       !string.IsNullOrEmpty(postazione)
                                 group row by new
                                 {
                                     Operatore = postazione.ToLower()
                                 } into grp
                                 select new
                                 {
                                     Operatore = grp.Key.Operatore,

                                     DocumentiCaptiva = grp.Where(r =>
                                     {
                                         var numeroDoc = r.Field<string>("Numero documenti");
                                         var codiceScatolaLocal = r.Field<string>("Codice Scatola");
                                         return !string.IsNullOrEmpty(numeroDoc) &&
                                                int.TryParse(numeroDoc, out int dummy) &&
                                                !string.IsNullOrEmpty(codiceScatolaLocal) &&
                                                codiceScatolaLocal.Length >= 9 &&
                                                codiceScatolaLocal.Substring(4, 5) != "999X9";
                                     }).Sum(r => int.Parse(r.Field<string>("Numero documenti")?.Trim() ?? "0"))
                                 };



                using (var context = _context.CreateDbContext())
                {

                    foreach (var item in grpSorterBuste)
                    {
                        if (destinazione == Destinazione.verona)
                        {
                            AderEquitalia4OperatoriVr prod = new AderEquitalia4OperatoriVr();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiSorterBuste;
                            prod.Scarti = item.ScartiSorterBuste;
                            prod.TipoScansione = "Sorter_Buste";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriVrs.Add(prod);
                        }

                        if (destinazione == Destinazione.genova)
                        {
                            AderEquitalia4OperatoriGe prod = new AderEquitalia4OperatoriGe();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiSorterBuste;
                            prod.Scarti = item.ScartiSorterBuste;
                            prod.TipoScansione = "Sorter_Buste";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriGes.Add(prod);
                        }

                    }

                    foreach (var item in grpSorter)
                    {
                        if (destinazione == Destinazione.verona)
                        {
                            AderEquitalia4OperatoriVr prod = new AderEquitalia4OperatoriVr();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiSorter;
                            prod.Scarti = item.ScartiSorter;
                            prod.TipoScansione = "Sorter";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriVrs.Add(prod);
                        }

                        if (destinazione == Destinazione.genova)
                        {
                            AderEquitalia4OperatoriGe prod = new AderEquitalia4OperatoriGe();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiSorter;
                            prod.Scarti = item.ScartiSorter;
                            prod.TipoScansione = "Sorter";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriGes.Add(prod);
                        }

                    }


                    foreach (var item in grpCaptiva)
                    {
                        if (destinazione == Destinazione.verona)
                        {
                            AderEquitalia4OperatoriVr prod = new AderEquitalia4OperatoriVr();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiCaptiva;
                            prod.Scarti = 0;
                            prod.TipoScansione = "Captiva";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriVrs.Add(prod);
                        }

                        if (destinazione == Destinazione.genova)
                        {
                            AderEquitalia4OperatoriGe prod = new AderEquitalia4OperatoriGe();
                            prod.Operatore = item.Operatore;
                            prod.TotaleDocumenti = item.DocumentiCaptiva;
                            prod.Scarti = 0;
                            prod.TipoScansione = "Captiva";
                            prod.DataScansione = mailEquitalia.DataRiferimento;
                            prod.IdEvento = mailEquitalia.IDEvento;

                            context.AderEquitalia4OperatoriGes.Add(prod);
                        }

                    }

                    context.SaveChanges();
                }

                mailEquitalia.Esito = true;
            }
            catch (DbUpdateException ex)
            {
                var sqlexception = ex.InnerException?.InnerException as SqlException;
                if (sqlexception != null)
                {
                    if (sqlexception.Errors.OfType<SqlError>().Any(se => se.Number == 2601 || (se.Number == 2627)))
                    {
                        // Duplicate Key Exception
                        mailEquitalia.Esito = false;
                        _logger.LogError(ex.InnerException?.InnerException?.Message?.ToString() ?? "Errore sconosciuto");
                    }
                }
            }
        }



        /// <summary>
        /// Trova il testo cercato nel body della email
        /// </summary>
        /// <param name="bodyText"></param>
        /// <param name="searchString"></param>
        /// <returns></returns>
        private string findBodyText(string bodyText, string searchString)
        {
            string searchedText = "";
            if (!string.IsNullOrEmpty(bodyText) && !string.IsNullOrEmpty(searchString))
            {
                string regexPattern = searchString + @"\s*(\S+)";
                Match regexResults = Regex.Match(bodyText, regexPattern);
                if (regexResults.Success)
                    searchedText = regexResults.Groups[1].ToString();
            }
            return searchedText;
        }



        /// <summary>
        /// Read a csv file and load into datatable
        /// </summary>
        /// <param name="pathFile"></param>
        /// <returns></returns>
        private DataTable LeggiCsv(string pathFile)
        {
            Char quotingCharacter = '\0';  // means none
            Char escapeCharacter = '\0';
            Char commentCharacter = '\0';
            Char delimiter = ';';
            bool hasHeader = true;

            DataTable csvTable = new DataTable();

            using (var reader = new CsvReader(new StreamReader(pathFile), hasHeader, delimiter, quotingCharacter, escapeCharacter, commentCharacter, ValueTrimmingOptions.All))
            {
                int fieldCount = reader.FieldCount;
                string[] headers = reader.GetFieldHeaders();

                foreach (string headerLabel in headers)
                {
                    csvTable.Columns.Add(headerLabel, typeof(String));
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



        /// <summary>
        /// Get totale documenti csv
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private int GetTotaleCsv(DataTable table)
        {
            var sum = 0;
            sum = table.AsEnumerable().Where(r =>
            {
                var numeroDoc = r.Field<string>("Numero Documenti");
                return !string.IsNullOrEmpty(numeroDoc) && int.TryParse(numeroDoc, out int dummy);
            }).Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));
            return sum;
        }


        /// <summary>
        /// Get totale documenti csv
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private TotaleMailCSV GetTotaleCsvScansionati(DataTable table)
        {
            TotaleMailCSV totale = new TotaleMailCSV();

            totale.ScansioneCaptiva = table.AsEnumerable()
                                           .Where(r =>
                                           {
                                               var numeroDoc = r.Field<string>("Numero documenti");
                                               var codiceScatola = r.Field<string>("Codice Scatola");
                                               return !string.IsNullOrEmpty(numeroDoc) &&
                                                      int.TryParse(numeroDoc, out int dummy) &&
                                                      !string.IsNullOrEmpty(codiceScatola) &&
                                                      codiceScatola.Length >= 9 &&
                                                      codiceScatola.Substring(4, 5) != "999X9";
                                           })
                                           .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));


            totale.ScansioneSorter = table.AsEnumerable()
                                          .Where(r =>
                                          {
                                              var numeroDoc = r.Field<string>("Numero documenti");
                                              var codiceScatola = r.Field<string>("Codice Scatola");
                                              return !string.IsNullOrEmpty(numeroDoc) &&
                                                     int.TryParse(numeroDoc, out int dummy) &&
                                                     !string.IsNullOrEmpty(codiceScatola) &&
                                                     codiceScatola.Length >= 10 &&
                                                     (codiceScatola.Substring(4, 6) == "999X91" || codiceScatola.Substring(4, 6) == "999X92") &&
                                                     codiceScatola.Length >= 3 &&
                                                     codiceScatola.Substring(0, 3) == "MN4";
                                          })
                                          .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));


            totale.ScartiScansioneSorter = table.AsEnumerable()
                                                .Where(r =>
                                                {
                                                    var numeroDoc = r.Field<string>("Numero documenti");
                                                    var codiceScatola = r.Field<string>("Codice Scatola");
                                                    return !string.IsNullOrEmpty(numeroDoc) &&
                                                           int.TryParse(numeroDoc, out int dummy) &&
                                                           !string.IsNullOrEmpty(codiceScatola) &&
                                                           codiceScatola.Length >= 10 &&
                                                           (codiceScatola.Substring(4, 6) == "999X91" || codiceScatola.Substring(4, 6) == "999X92") &&
                                                           codiceScatola.Length >= 3 &&
                                                           codiceScatola.Substring(0, 3) != "MN4";
                                                })
                                                .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));


            totale.ScansioneSorterBuste = table.AsEnumerable()
                                               .Where(r =>
                                               {
                                                   var numeroDoc = r.Field<string>("Numero documenti");
                                                   var codiceScatola = r.Field<string>("Codice Scatola");
                                                   return !string.IsNullOrEmpty(numeroDoc) &&
                                                          int.TryParse(numeroDoc, out int dummy) &&
                                                          !string.IsNullOrEmpty(codiceScatola) &&
                                                          codiceScatola.Length >= 10 &&
                                                          codiceScatola.Substring(4, 6) == "999X93" &&
                                                          codiceScatola.Length >= 3 &&
                                                          codiceScatola.Substring(0, 3) == "MN4";
                                               })
                                               .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));


            totale.ScartiScansioneSorterBuste = table.AsEnumerable()
                                                     .Where(r =>
                                                     {
                                                         var numeroDoc = r.Field<string>("Numero documenti");
                                                         var codiceScatola = r.Field<string>("Codice Scatola");
                                                         return !string.IsNullOrEmpty(numeroDoc) &&
                                                                int.TryParse(numeroDoc, out int dummy) &&
                                                                !string.IsNullOrEmpty(codiceScatola) &&
                                                                codiceScatola.Length >= 10 &&
                                                                codiceScatola.Substring(4, 6) == "999X93" &&
                                                                codiceScatola.Length >= 3 &&
                                                                codiceScatola.Substring(0, 3) != "MN4";
                                                     })
                                                     .Sum(r => int.Parse(r.Field<string>("Numero Documenti")?.Trim() ?? "0"));


            return totale;
        }




        /// <summary>
        /// Sposta email in cartella dedicata
        /// </summary>
        /// <param name="email"></param>
        private void SpostaEmail(EmailMessage email)
        {
            if (service == null)
                return;

            // sposta email nella cartella HeraComm
            var viewF = new FolderView(1);
            viewF.Traversal = FolderTraversal.Deep;

            // cartella dedicata per email che arriva in inbox edp.vr
            var filter = new SearchFilter.IsEqualTo(FolderSchema.DisplayName, "EQUITALIA_4");


            var resultsFolder = service.FindFolders(WellKnownFolderName.Root, filter, viewF);
            if ((resultsFolder.TotalCount < 1))
                throw new Exception("Cannot find EQUITALIA_4 folder");
            else
                foreach (Folder f in resultsFolder)
                {
                    if ((f.DisplayName == "EQUITALIA_4"))
                    {
                        email.IsRead = true;
                        email.Update(ConflictResolutionMode.AutoResolve);
                        email.Move(f.Id);
                        _logger.LogInformation("La mail del : " + email.DateTimeReceived + " è stata spostata da Inbox a " + f.DisplayName);
                    }
                }
        }


        /// <summary>
        /// Invia Mail
        /// </summary>
        /// <param name="subjectAnswer">Oggetto della mail di risposta.</param>
        /// <param name="bodyAnswer">Corpo della mail di risposta.</param>
        public void InviaMail(string subjectAnswer, string bodyAnswer)
        {

            try
            {
                //using (var smtpClient = new System.Net.Mail.SmtpClient("gehub1.postel.it"))
                //{

                //    var basicCredential = new NetworkCredential("verona.edp", "200902hope*");
                //    var msg = new System.Net.Mail.MailMessage();
                //    msg.IsBodyHtml = false;
                //    msg.From = new System.Net.Mail.MailAddress("edp.vr@postel.it");
                //    msg.To.Add("diego.lista@postel.it");
                //    //'msg.CC.Add("Edp.Verona@postel.it")
                //    msg.Subject = subjectAnswer;
                //    msg.Body = bodyAnswer;

                //    smtpClient.Credentials = basicCredential;
                //    smtpClient.Port = 587;
                //    smtpClient.Send(msg);
                //}

                ExchangeService service = new Microsoft.Exchange.WebServices.Data.ExchangeService(ExchangeVersion.Exchange2013_SP1)
                {
                    Credentials = new WebCredentials("verona.edp", "200902hope*", "postel.it")
                };

                service.Url = new Uri("https://postaweb.postel.it/ews/exchange.asmx");

                EmailMessage msg = new EmailMessage(service);
                msg.ToRecipients.Add("diego.lista@postel.it");
                //msg.CcRecipients.Add("diego.lista@postel.it");
                msg.Subject = subjectAnswer;
                msg.Body = bodyAnswer;

                msg.Send();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore invio email risposta");
            }


        }


        /// <summary>
        /// Creo il file zip
        /// </summary>
        public void CreaZip()
        {

            try
            {
                var dest = pathFileArchive + @"\" + DateTime.Today.ToString("yyyyMMdd") + @".zip";
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }
                ZipFile.CreateFromDirectory(pathFile, dest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore creazione file zip");
                throw new Exception("Errore creazione file zip : " + ex.Message, ex);
            }

        }

        /// <summary>
        /// Pulisce la cartella di lavoro eliminando tutti i file temporanei.
        /// </summary>
        private void ClearFolder()
        {
            string[] files = Directory.GetFiles(pathFile);
            foreach (string file in files)
            {
                File.Delete(file);

            }
        }


        /// <summary>
        /// Elabora Allegati
        /// </summary>
        public void CaricaAllegato()
        {
            try
            {
                bool exists = System.IO.Directory.Exists(pathFile);
                if (!exists)
                    System.IO.Directory.CreateDirectory(pathFile);

                bool existsArchive = System.IO.Directory.Exists(pathFileArchive);
                if (!existsArchive)
                    System.IO.Directory.CreateDirectory(pathFileArchive);


                GetAttachments();
                CreaZip();
                ClearFolder();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore in CaricaAllegato");
                //inviaMailServiceState("Equitalia3 - servizio automatico - Errore nel caricamento dati produzione", ex.Message);
            }
        }
    }
}
