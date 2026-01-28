using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0084886_RDMKT_GENERALI_HADID")]
    public class Z0084886_RDMKT_GENERALI_HADID : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0084886_RDMKT_GENERALI_HADID(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }



        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0084886_RDMKT_GENERALI_HADID");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var table = new DataTable();

                //Data Entry
                if (IDFaseLavorazione == 5)
                {


                    string query = @"select OP_INDEX as operatore,CONVERT(date, DATA_INDEX) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID 
                                    where CONVERT(date, DATA_INDEX) >= @startDataDe1 and CONVERT(date, DATA_INDEX) <= @endDataDe1
                                    group by OP_INDEX,CONVERT(date, DATA_INDEX)
                                    union all
                                    select OP_INDEX as operatore,CONVERT(date, DATA_INDEX) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID_ORD 
                                    where CONVERT(date, DATA_INDEX) >= @startDataDe2 and CONVERT(date, DATA_INDEX) <= @endDataDe2
                                    group by OP_INDEX,CONVERT(date, DATA_INDEX)
                                    union all
                                    select OP_INDEX as operatore,CONVERT(date, DATA_INDEX) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID_RACC 
                                    where CONVERT(date, DATA_INDEX) >= @startDataDe3 and CONVERT(date, DATA_INDEX) <= @endDataDe3
                                    group by OP_INDEX,CONVERT(date, DATA_INDEX)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDe1", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe1", endDataDE);
                        command.Parameters.AddWithValue("@startDataDe2", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe2", endDataDE);
                        command.Parameters.AddWithValue("@startDataDe3", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe3", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);
                    }
                }


                //Scansione
                if (IDFaseLavorazione == 4)
                {

                    string query = @"select OP_SCAN as operatore,CONVERT(date, DATA_SCAN ,104) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID 
                                    where CONVERT(date, CAMPO_1 ,104) >= @startDataScan1 and CONVERT(date, CAMPO_1 ,104) <= @endDataScan1 and OP_SCAN is not null
                                    group by OP_SCAN,DATA_SCAN
                                    union all
                                    select OP_SCAN as operatore,SUBSTRING(nome_batch,29,8) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID_ORD
                                    where CONVERT(date, SUBSTRING(nome_batch,29,8)) >= @startDataScan2 and CONVERT(date, SUBSTRING(nome_batch,29,8)) <= @endDataScan2
                                    group by OP_SCAN,DATA_SCAN,nome_batch
                                    union all
                                    select OP_SCAN as operatore,SUBSTRING(nome_batch,30,8) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                                    from Z0084886_RDMKT_GENERALI_HADID_RACC
                                    where CONVERT(date, SUBSTRING(nome_batch,30,8)) >= @startDataScan3 and CONVERT(date, SUBSTRING(nome_batch,30,8)) <= @endDataScan3
                                    group by OP_SCAN,DATA_SCAN,nome_batch
                                    order by DataLavorazione";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan1", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan1", endDataScan);
                        command.Parameters.AddWithValue("@startDataScan2", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan2", endDataScan);
                        command.Parameters.AddWithValue("@startDataScan3", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan3", endDataScan);
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




