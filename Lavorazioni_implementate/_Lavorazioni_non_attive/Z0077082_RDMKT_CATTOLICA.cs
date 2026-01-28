using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0077082_RDMKT_CATTOLICA")]
    public class Z0077082_RDMKT_CATTOLICA : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0077082_RDMKT_CATTOLICA(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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


        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0077082_RDMKT_CATTOLICA");
            DataTable table = new DataTable();

            try
            {

                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    table.Clear();

                    //old
                    //string query = @"select OP_SCAN  as Operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti, 
                    //                 isnull(SUM(convert(int,NUM_PAGINE))/2,0) AS Fogli, isnull(SUM(convert(int,NUM_PAGINE)),0) AS pagine
                    //                 from Z0077082_RDMKT_CATTOLICA
                    //                 where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                    //                 group by OP_SCAN,convert(date, DATA_SCAN)";


                    string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                     from Z0077082_RDMKT_CATTOLICA_UDA
                                     where convert(date, DATA_SCAN) >= @startDataScan1 and convert(date, DATA_SCAN) <= @endDataScan1
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     union all
                                     select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                     from Z0077082_RDMKT_CATTOLICA_CART_UDA_DETTAGLIO
                                     where convert(date, DATA_SCAN) >= @startDataScan2 and convert(date, DATA_SCAN) <= @endDataScan2
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by DataLavorazione";



                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                            using (var cmd = new SqlCommand(query, connection))
                            {
                                cmd.CommandTimeout = 0;
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("@startDataScan1", startDataScan);
                                cmd.Parameters.AddWithValue("@endDataScan1", endDataScan);
                                cmd.Parameters.AddWithValue("@startDataScan2", startDataScan);
                                cmd.Parameters.AddWithValue("@endDataScan2", endDataScan);
                                adapter.SelectCommand = cmd;
                                adapter.Fill(table);
                            }
                        }
                    }
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {
                    table.Clear();

                    // old table
                    //cmdRHM.CommandText = "select OP_INDEX as operatore, COUNT(*) as tot_doc " &
                    //"from Z0018682_RDMKT_CATTOLICA " &
                    //"where convert(date,data_index)>= ? and convert(date,data_index)<= ? " &
                    //"group by OP_INDEX"

                    //old
                    //string query = @"select OP_INDEX as Operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,NUM_PAGINE))/2,0) AS Fogli, isnull(SUM(convert(int,NUM_PAGINE)),0) AS pagine
                    //                 from Z0077082_RDMKT_CATTOLICA
                    //                 where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
                    //                 group by OP_INDEX, convert(date, DATA_INDEX)";



                    string query = @"select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                    from Z0077082_RDMKT_CATTOLICA_UDA
                                    where convert(date, DATA_INDEX) >= @startDataDe1 and convert(date, DATA_INDEX) <= @endDataDe1
                                    group by OP_INDEX,convert(date, DATA_INDEX)
                                    union all
                                    select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                    from Z0077082_RDMKT_CATTOLICA_CART_UDA_DETTAGLIO
                                    where convert(date, DATA_INDEX) >= @startDataDe2 and convert(date, DATA_INDEX) <= @endDataDe2
                                    group by OP_INDEX,convert(date, DATA_INDEX)
                                    order by DataLavorazione";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                            using (var cmd = new SqlCommand(query, connection))
                            {
                                cmd.CommandTimeout = 0;
                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("@startDataDe1", startDataDE);
                                cmd.Parameters.AddWithValue("@endDataDe1", endDataDE);
                                cmd.Parameters.AddWithValue("@startDataDe2", startDataDE);
                                cmd.Parameters.AddWithValue("@endDataDe2", endDataDE);
                                adapter.SelectCommand = cmd;
                                adapter.Fill(table);
                            }
                        }

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




