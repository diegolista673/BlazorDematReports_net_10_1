using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0073216_RDMKT_MBPA")]
    public class Z0073216_RDMKT_MBPA : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public Z0073216_RDMKT_MBPA(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            this.LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }

        //old
        //private DataTable FillTableVerona()
        //{
        //    this.TableData = new DataTable("Z0073216_RDMKT_MBPA");

        //    try
        //    {
        //        SqlCommand command;
        //        SqlDataAdapter adapter = new SqlDataAdapter();

        //        var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
        //        var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

        //        var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
        //        var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

        //        //Scansione
        //        if (IDFaseLavorazione == 4)
        //        {

        //            string query = @"select OP_SCAN  as Operatore, convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,NUM_PAGINE))/2 AS Fogli,SUM(convert(int,NUM_PAGINE)) AS Pagine
        //                             from Z0073216_rdmkt_mbpa
        //                             where convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @startDataScan and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) <= @endDataScan
        //                             group by OP_SCAN, CONVERT(VARCHAR(10), DATA_SCAN)
        //                             order by convert(date,CONVERT(VARCHAR(10), DATA_SCAN))";

        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }
        //                command = new SqlCommand(query, connection);
        //                command.Parameters.Clear();
        //                command.Parameters.AddWithValue("@startDataScan", startDataScan);
        //                command.Parameters.AddWithValue("@endDataScan", endDataScan);
        //                adapter.SelectCommand = command;
        //                adapter.Fill(this.TableData);
        //            }
        //        }


        //        //Data Entry
        //        if (IDFaseLavorazione == 5)
        //        {


        //            string query = @"select OP_INDEX as Operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti
        //                             from Z0073216_rdmkt_mbpa
        //                             where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
        //                             group by OP_INDEX, convert(date, DATA_INDEX)";


        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }

        //                command = new SqlCommand(query, connection);
        //                command.Parameters.Clear();
        //                command.Parameters.AddWithValue("@startDataDe", startDataDE);
        //                command.Parameters.AddWithValue("@endDataDe", endDataDE);
        //                adapter.SelectCommand = command;
        //                adapter.Fill(this.TableData);
        //            }
        //        }

        //        EsitoLetturaDato = true;
        //        return this.TableData;
        //    }
        //    catch (Exception ex)
        //    {
        //        EsitoLetturaDato = false;
        //        Error = true;
        //        base.ErrorMessage = ex.Message;
        //        logger.Error(ex.Message);

        //        if (this.LavorazioneInRichiestaSingola == true)
        //        {
        //            throw new Exception(ex.Message);
        //        }
        //        else
        //        {
        //            return TableData;
        //        }
        //    }


        //}


        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0073216_RDMKT_MBPA");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select OP_INDEX as Operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti,Isnull(SUM(convert(int,NUM_PAGINE))/2,0) as Fogli, Isnull(SUM(convert(int,NUM_PAGINE)),0) as Pagine
                                     from Z0073216_rdmkt_mbpa
                                     where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
                                     group by OP_INDEX, convert(date, DATA_INDEX)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        var table = new DataTable();
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);

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

                    }
                }

                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    string query = @"select OP_SCAN  as Operatore, convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) as DataLavorazione, COUNT(*) as Documenti, Isnull(SUM(convert(int,NUM_PAGINE))/2,0) as Fogli, Isnull(SUM(convert(int,NUM_PAGINE)),0) as Pagine
                                     from Z0073216_rdmkt_mbpa
                                     where convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @startDataScan and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) <= @endDataScan
                                     group by OP_SCAN, CONVERT(VARCHAR(10), DATA_SCAN)
                                     order by convert(date,CONVERT(VARCHAR(10), DATA_SCAN))";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        var table = new DataTable();
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);

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
                    return TableData;
                }
            }
        }

    }
}




