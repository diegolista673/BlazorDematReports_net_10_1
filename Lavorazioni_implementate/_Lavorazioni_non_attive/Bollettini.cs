using LibraryUtility;
using LumenWorks.Framework.IO.Csv;
using NLog;
using System.Data;
using System.Globalization;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Bollettini")]
    public class Bollettini : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Bollettini(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            switch (this.IDCentro)
            {
                //Verona
                case 1:
                    LavorazioneImplementataByCentro = true;
                    FillTableVerona();
                    break;
                //Genova
                case 2:
                    LavorazioneImplementataByCentro = true;
                    FillTableGenova();
                    break;
                //Melzo
                case 4:
                    LavorazioneImplementataByCentro = true;
                    FillTableMelzo();
                    break;

                default:
                    LavorazioneImplementataByCentro = false;
                    break;
            }

            return this.TableData;

        }


        //valori raggruppati tramite lavorazione
        private DataTable FillTableVerona()
        {
            // le tre lavorazioni seguenti non hanno dati recuperabili
            //BOLLETTINI_IMU
            //BOLLETTINI_TASI
            //BOLLETTINI_TARES

            DateTime dataFiltered = StartDataLavorazione;
            DateTime dataFolder = StartDataLavorazione.AddDays(1);
            DataTable tableDoc = new DataTable();

            this.TableData = new DataTable("Bollettini_Verona");

            string lavorazioneBol = "";

            //la domenica non è presente il file csv
            if (dataFolder.DayOfWeek == DayOfWeek.Sunday)
            {
                //return tableIns;
                dataFolder = dataFolder.AddDays(1);
            }


            switch (this.NomeProcedura)
            {

                case "BP_IDEA_BOLLETTINI":
                    lavorazioneBol = "std";
                    break;
                case "BP_IDEA_MARCHE":
                    lavorazioneBol = "marche";
                    break;
                case "BP_IDEA_ASL":
                    lavorazioneBol = "asl bn/ce";
                    break;
                case "BP_IDEA_INAIL":
                    lavorazioneBol = "inail";
                    break;
                case "BP_IDEA_ASTI_COSAP":
                    lavorazioneBol = "asti";
                    break;
                case "BP_IDEA_PUGLIA":
                    lavorazioneBol = "puglia";
                    break;
                case "BP_IDEA_SICILIA":
                    lavorazioneBol = "sicilia";
                    break;
                case "BP_IDEA_MOLISE":
                    lavorazioneBol = "molise";
                    break;
                case "BP_IDEA_CAMPANIA":
                    lavorazioneBol = "campania";
                    break;
                case "BP_IDEA_AUTO":
                    lavorazioneBol = "tasse auto";
                    break;
                case "BOLLETTINI_ICI":
                    lavorazioneBol = "ici";
                    break;
                case "BOLLETTINI_VIOLAZIONI":
                    lavorazioneBol = "violazioni";
                    break;
                case "BOLLETTINI_PUBBLICITA":
                    lavorazioneBol = "pubblicità";
                    break;
                case "BOLLETTINI_TOSAP_TARSUG":
                    lavorazioneBol = "tosap-tarsug";
                    break;

                default:
                    lavorazioneBol = "";
                    break;
            }


            try
            {
                if ((NomeProcedura == "BOLLETTINI_IMU") || (NomeProcedura == "BOLLETTINI_TASI") || (NomeProcedura == "BOLLETTINI_TARES"))
                {
                    return this.TableData;
                }

                var tableCsv = GetTableCsv(dataFolder);

                var dtRows = tableCsv.AsEnumerable().Where(myRow => myRow.Field<string>("Lavorazione").ToLower() == lavorazioneBol &&
                                                                    myRow.Field<string>("Data Lavorazione") == dataFiltered.ToString("yyyyMMdd"));


                if (dtRows.Any())
                {
                    var dtFiltered = dtRows.CopyToDataTable();

                    if (dtFiltered != null)
                    {
                        if (dtFiltered.Rows.Count > 0)
                        {
                            //Crea la Tabella parziale
                            tableDoc.Columns.Add("Operatore", typeof(string));
                            tableDoc.Columns.Add("DataLavorazione", typeof(DateTime));
                            tableDoc.Columns.Add("Documenti", typeof(int));

                            foreach (DataRow row in dtFiltered.Rows)
                            {
                                DateTime dataValue = DateTime.ParseExact(row["Data Lavorazione"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                                tableDoc.Rows.Add(row["Operatore"].ToString(), dataValue, row["Qta"]);
                            }
                        }
                    }


                    var newSort = from row in tableDoc.AsEnumerable()
                                  group row by new
                                  {
                                      Operatore = row.Field<string>("Operatore"),
                                      DataLavorazione = row.Field<DateTime>("DataLavorazione")
                                  } into grp
                                  select new
                                  {
                                      Operatore = grp.Key.Operatore,
                                      DataLavorazione = grp.Key.DataLavorazione,
                                      Documenti = grp.Sum(r => r.Field<int>("Documenti")),
                                      Fogli = grp.Sum(r => r.Field<int>("Documenti")),
                                      Pagine = grp.Sum(r => r.Field<int>("Documenti")) * 2
                                  };

                    //Crea la Tabella finale
                    this.TableData.Columns.Add("Operatore", typeof(string));
                    this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    this.TableData.Columns.Add("Documenti", typeof(int));
                    this.TableData.Columns.Add("Fogli", typeof(int));
                    this.TableData.Columns.Add("Pagine", typeof(int));


                    foreach (var row in newSort)
                    {
                        var operatorName = row.Operatore.Replace(" ", ".").ToLower();
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, StartDataLavorazione.Date, row.Documenti);
                        }
                    }
                }

                EsitoLetturaDato = true;
                return this.TableData;
            }
            catch (Exception ex)
            {
                EsitoLetturaDato = false;
                Error = true;
                base.ErrorMessage = ex.Message;
                logger.Error(ex.Message);

                if (this.LavorazioneInRichiestaSingola == true)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    return this.TableData;
                }
            }


        }


        //valori raggruppati tutti nella lavorazione per Genova ArrichimentoBollettini senza distinzione di procedura
        private DataTable FillTableGenova()
        {
            DateTime dataFiltered = StartDataLavorazione;
            DateTime dataFolder = StartDataLavorazione.AddDays(1);
            DataTable tableDoc = new DataTable();
            this.TableData = new DataTable("Bollettini_Genova");

            try
            {

                //la domenica non è presente il file csv
                if (dataFolder.DayOfWeek == DayOfWeek.Sunday)
                {
                    //return tableIns;
                    dataFolder = dataFolder.AddDays(1);
                }


                var tableCsv = GetTableCsv(dataFolder);

                var dtRows = tableCsv.AsEnumerable().Where(myRow => myRow.Field<string>("Data Lavorazione") == dataFiltered.ToString("yyyyMMdd"));


                if (dtRows.Any())
                {
                    var dtFiltered = dtRows.CopyToDataTable();

                    if (dtFiltered.Rows.Count > 0)
                    {
                        //Crea la Tabella parziale
                        tableDoc.Columns.Add("Operatore", typeof(string));
                        tableDoc.Columns.Add("DataLavorazione", typeof(DateTime));
                        tableDoc.Columns.Add("Documenti", typeof(int));

                        foreach (DataRow row in dtFiltered.Rows)
                        {
                            DateTime dataValue = DateTime.ParseExact(row["Data Lavorazione"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                            tableDoc.Rows.Add(row["Operatore"].ToString(), dataValue, row["Qta"]);
                        }
                    }


                    var newSort = from row in tableDoc.AsEnumerable()
                                  group row by new
                                  {
                                      Operatore = row.Field<string>("Operatore"),
                                      DataLavorazione = row.Field<DateTime>("DataLavorazione")
                                  } into grp
                                  select new
                                  {
                                      Operatore = grp.Key.Operatore,
                                      DataLavorazione = grp.Key.DataLavorazione,
                                      Documenti = grp.Sum(r => r.Field<int>("Documenti")),
                                      Fogli = grp.Sum(r => r.Field<int>("Documenti")),
                                      Pagine = grp.Sum(r => r.Field<int>("Documenti")) * 2
                                  };

                    //Crea la Tabella parziale
                    TableData.Columns.Add("Operatore", typeof(string));
                    TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    TableData.Columns.Add("Documenti", typeof(int));
                    TableData.Columns.Add("Fogli", typeof(int));
                    TableData.Columns.Add("Paging", typeof(int));


                    foreach (var row in newSort)
                    {
                        var operatorName = row.Operatore.Replace(" ", ".").ToLower();
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, StartDataLavorazione.Date, row.Documenti);
                        }
                    }


                }

                EsitoLetturaDato = true;
                return TableData;
            }
            catch (Exception ex)
            {
                EsitoLetturaDato = false;
                Error = true;
                base.ErrorMessage = ex.Message;
                logger.Error(ex.Message);

                if (this.LavorazioneInRichiestaSingola == true)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    return TableData;
                }
            }


        }


        //valori raggruppati tutti nella lavorazione per Melzo ArrichimentoBollettini senza distinzione di procedura
        private DataTable FillTableMelzo()
        {
            DateTime dataFiltered = StartDataLavorazione;
            DateTime dataFolder = StartDataLavorazione.AddDays(1);
            DataTable tableDoc = new DataTable();
            this.TableData = new DataTable("Bollettini_Melzo");

            try
            {

                //la domenica non è presente il file csv
                if (dataFolder.DayOfWeek == DayOfWeek.Sunday)
                {
                    //return tableIns;
                    dataFolder = dataFolder.AddDays(1);
                }


                var tableCsv = GetTableCsv(dataFolder);

                var dtRows = tableCsv.AsEnumerable().Where(myRow => myRow.Field<string>("Data Lavorazione") == dataFiltered.ToString("yyyyMMdd"));


                if (dtRows.Any())
                {
                    var dtFiltered = dtRows.CopyToDataTable();

                    if (dtFiltered.Rows.Count > 0)
                    {
                        //Crea la Tabella parziale
                        tableDoc.Columns.Add("Operatore", typeof(string));
                        tableDoc.Columns.Add("DataLavorazione", typeof(DateTime));
                        tableDoc.Columns.Add("Documenti", typeof(int));

                        foreach (DataRow row in dtFiltered.Rows)
                        {
                            DateTime dataValue = DateTime.ParseExact(row["Data Lavorazione"].ToString(), "yyyyMMdd", CultureInfo.InvariantCulture);
                            tableDoc.Rows.Add(row["Operatore"].ToString(), dataValue, row["Qta"]);
                        }
                    }


                    var newSort = from row in tableDoc.AsEnumerable()
                                  group row by new
                                  {
                                      Operatore = row.Field<string>("Operatore"),
                                      DataLavorazione = row.Field<DateTime>("DataLavorazione")
                                  } into grp
                                  select new
                                  {
                                      Operatore = grp.Key.Operatore,
                                      DataLavorazione = grp.Key.DataLavorazione,
                                      Documenti = grp.Sum(r => r.Field<int>("Documenti")),
                                      Fogli = grp.Sum(r => r.Field<int>("Documenti")),
                                      Pagine = grp.Sum(r => r.Field<int>("Documenti")) * 2
                                  };

                    //Crea la Tabella parziale
                    TableData.Columns.Add("Operatore", typeof(string));
                    TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    TableData.Columns.Add("Documenti", typeof(int));
                    TableData.Columns.Add("Fogli", typeof(int));
                    TableData.Columns.Add("Paging", typeof(int));


                    foreach (var row in newSort)
                    {
                        var operatorName = row.Operatore.Replace(" ", ".").ToLower();
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, StartDataLavorazione.Date, row.Documenti);
                        }
                    }


                }

                EsitoLetturaDato = true;
                return TableData;
            }
            catch (Exception ex)
            {
                EsitoLetturaDato = false;
                Error = true;
                base.ErrorMessage = ex.Message;
                logger.Error(ex.Message);

                if (this.LavorazioneInRichiestaSingola == true)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    return TableData;
                }
            }


        }


        //Get datatable from csv bollettini la folder che li contiene è uguale alla data di lavorazione + 1 giorno
        public override DataTable GetTableCsv(DateTime dataFolder)
        {
            DataTable tableData = new DataTable();


            var path = _lavorazioniConfigManager.PathFileBollettini + "\\" + dataFolder.ToString("yyyyMMdd") + "\\Rese_Operatori.csv";

            Char quotingCharacter = '\0';  // means none
            Char escapeCharacter = '\0';
            Char commentCharacter = '\0';
            Char delimiter = ';';
            bool hasHeader = true;


            if (!File.Exists(path))
            {
                throw new Exception("Bollettini - Impossibile trovare il file : " + path);
            }


            using (var reader = new CsvReader(new StreamReader(path), hasHeader, delimiter, quotingCharacter, escapeCharacter, commentCharacter, ValueTrimmingOptions.All))
            {

                int fieldCount = reader.FieldCount;

                string[] headers = reader.GetFieldHeaders();
                //while (reader.ReadNextRecord())
                //{
                //    for (int i = 0; i < fieldCount; i++)
                //        Console.Write(string.Format("{0} = {1};",
                //                      headers[i], reader[i]));
                //    Console.WriteLine();
                //}

                foreach (string headerLabel in headers)
                {
                    tableData.Columns.Add(headerLabel, typeof(String));
                }

                while (reader.ReadNextRecord())
                {
                    DataRow newRow = tableData.NewRow();

                    for (int i = 0; i <= fieldCount - 1; i++)
                    {
                        newRow[i] = reader[i];
                    }

                    tableData.Rows.Add(newRow);
                }

                return tableData;
            }


        }






    }
}




