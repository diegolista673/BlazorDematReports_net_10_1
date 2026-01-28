using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0084886_RDMKT_GENERALI_FASCHIM")]
    public class Z0084886_RDMKT_GENERALI_FASCHIM : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0084886_RDMKT_GENERALI_FASCHIM(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        //public override DataTable GetDatiDemat()
        //{

        //    switch (this.IDCentro)
        //    {
        //        //Verona
        //        case 1:
        //            LavorazioneImplementataByCentro = true;
        //            FillTableVerona();
        //            break;
        //        //Genova
        //        case 2:
        //            LavorazioneImplementataByCentro = false;                    
        //            break;
        //        default:
        //            LavorazioneImplementataByCentro = false;
        //            break;
        //    }

        //    return this.TableData;

        //}

        public override DataTable GetDatiDemat()
        {
            this.LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }



        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0084886_RDMKT_GENERALI_FASCHIM");
            DataTable table = new DataTable();


            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                //Scansione
                if (IDFaseLavorazione == 4)
                {
                     table.Clear();
                    //vecchia tabella
                    //string query = @"select OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, count(*) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                    //                 from [Z0084886_RDMKT_GEN_WELION_UDA_DETTAGLIO]
                    //                 where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and CAMPO_DE_10 ='WE_C03'
                    //                 group by OP_SCAN,convert(date, DATA_SCAN)
                    //                 order by convert(date, DATA_SCAN)";


                    //nuova tabella dal 10/10/2022
                    string query = @"select OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, count(*) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                                     from Z0088415_RDMKT_FASCHIM_UDA_DETTAGLIO
                                     where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by convert(date, DATA_SCAN)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);
                    }
                }


                //DataEntry
                if (IDFaseLavorazione == 5)
                {
                    table.Clear();

                    string query = @"select OP_SCAN as Operatore,convert(date, DATA_INDEX) as DataLavorazione, count(*) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                                     from [Z0084886_RDMKT_GEN_WELION_UDA_DETTAGLIO]
                                     where convert(date, DATA_INDEX) >= @startDataDE and convert(date, DATA_INDEX) <= @endDataDE and CAMPO_DE_10 ='WE_C03'
                                     group by OP_SCAN,convert(date, DATA_INDEX)
                                     order by convert(date, DATA_INDEX)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDE", startDataDE);
                        command.Parameters.AddWithValue("@endDataDE", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);
                    }
                }


                if (table.Rows.Count > 0)
                {
                    //group by utente
                    var newGrouped = from row in table.AsEnumerable()
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
                                         Fogli = grp.Sum(r => r.Field<int>("Fogli")),
                                         Pagine = grp.Sum(r => r.Field<int>("Pagine"))
                                     };

                    //Crea la Tabella finale
                    this.TableData.Columns.Add("Operatore", typeof(string));
                    this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    this.TableData.Columns.Add("Documenti", typeof(int));
                    this.TableData.Columns.Add("Fogli", typeof(int));
                    this.TableData.Columns.Add("Pagine", typeof(int));


                    foreach (var row in newGrouped)
                    {
                        //Since it will catch runs of any kind of whitespace(e.g.tabs, newlines, etc.) and replace them with a single space.
                        string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim().ToLower();
                        var operatorName = oper.Replace(" ", ".").ToLower();
                        operatorName = oper.Replace(@"postel\", "");
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == this.IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine);
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
                ErrorMessage = ex.Message;
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
    }
}




