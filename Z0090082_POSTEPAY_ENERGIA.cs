using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0090082_POSTEPAY_ENERGIA")]
    public class Z0090082_POSTEPAY_ENERGIA : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0090082_POSTEPAY_ENERGIA(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Z0090082_POSTEPAY_ENERGIA");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var table = new DataTable();

                //Scansione contratti + scarti + CONENGY (unione di 3 tabelle e somma dei risultati)
                if (IDFaseLavorazione == 4)
                {
                    string query = @"
                        SELECT 
                            operatore, 
                            DataLavorazione, 
                            SUM(Documenti) AS Documenti, 
                            SUM(Fogli) AS Fogli, 
                            SUM(Pagine) AS Pagine
                        FROM (
                            -- Query 1: Contratti (Z0090082_RDMKT_PPAY_CON_UDA)
                            SELECT 
                                OP_SCAN AS operatore,
                                CONVERT(date, DATA_SCAN) AS DataLavorazione, 
                                Count(*) AS Documenti, 
                                ISNULL((SUM(CONVERT(INT, NUM_PAG)) / 2), 0) AS Fogli, 
                                ISNULL(SUM(CONVERT(INT, NUM_PAG)), 0) AS Pagine
                            FROM Z0090082_RDMKT_PPAY_CON_UDA
                            WHERE STATO = 'Pubblicato' 
                              AND CONVERT(date, DATA_SCAN) >= @startDataScan 
                              AND CONVERT(date, DATA_SCAN) <= @endDataScan
                            GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)

                            UNION ALL

                            -- Query 2: Scarti (Z0090082_RDMKT_PPAY_SCA_UDA)
                            SELECT 
                                OP_SCAN AS operatore,
                                CONVERT(date, DATA_SCAN) AS DataLavorazione, 
                                Count(*) AS Documenti, 
                                ISNULL((SUM(CONVERT(INT, NUM_PAG)) / 2), 0) AS Fogli, 
                                ISNULL(SUM(CONVERT(INT, NUM_PAG)), 0) AS Pagine
                            FROM Z0090082_RDMKT_PPAY_SCA_UDA
                            WHERE STATO = 'Pubblicato' 
                              AND CONVERT(date, DATA_SCAN) >= @startDataScan 
                              AND CONVERT(date, DATA_SCAN) <= @endDataScan
                            GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)

                            UNION ALL

                            -- Query 3: Nuova tabella richiesta (Z0090082_RDMKT_PPAY_CONENGY_UDA)
                            SELECT 
                                OP_SCAN AS operatore,
                                CONVERT(date, DATA_SCAN) AS DataLavorazione, 
                                Count(*) AS Documenti, 
                                ISNULL((SUM(CONVERT(INT, NUM_PAG)) / 2), 0) AS Fogli, 
                                ISNULL(SUM(CONVERT(INT, NUM_PAG)), 0) AS Pagine
                            FROM Z0090082_RDMKT_PPAY_CONENGY_UDA
                            WHERE CONVERT(date, DATA_SCAN) >= @startDataScan 
                                AND CONVERT(date, DATA_SCAN) <= @endDataScan 
                                AND CAMPO_4 = 'GENOVA'
                            GROUP BY OP_SCAN, CONVERT(date, DATA_SCAN)
                        ) AS UnioneTotale
                        GROUP BY operatore, DataLavorazione
                        ORDER BY DataLavorazione, operatore";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);
                    }
                }

                //old query
                ////Scansione contratti + scarti (union di 2 tabelle)
                //if (IDFaseLavorazione == 4)
                //{
                //    string query = @"select OP_SCAN as operatore,CONVERT(date, DATA_SCAN) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                //                    from Z0090082_RDMKT_PPAY_CON_UDA 
                //                    where STATO = 'Pubblicato' and CONVERT(date, DATA_SCAN ) >= @startDataScan and CONVERT(date, DATA_SCAN ) <= @endDataScan
                //                    group by OP_SCAN,DATA_SCAN
                //                    union all
                //                    select OP_SCAN as operatore,CONVERT(date, DATA_SCAN) as DataLavorazione, Count(*) as Documenti, isnull((SUM(convert(int,NUM_PAG))/2),0) AS Fogli, isnull(SUM(convert(int,NUM_PAG)),0) AS Pagine
                //                    from Z0090082_RDMKT_PPAY_SCA_UDA 
                //                    where STATO = 'Pubblicato' and CONVERT(date, DATA_SCAN ) >= @startDataScan and CONVERT(date, DATA_SCAN ) <= @endDataScan
                //                    group by OP_SCAN,DATA_SCAN
                //                    order by CONVERT(date, DATA_SCAN)";


                //    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                //    {
                //        if (connection.State != ConnectionState.Open)
                //        {
                //            connection.Open();
                //        }

                //        command = new SqlCommand(query, connection);
                //        command.CommandTimeout = 0;
                //        command.Parameters.Clear();
                //        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                //        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                //        adapter.SelectCommand = command;
                //        adapter.Fill(table);
                //    }
                //}


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

                        if (operatorName == "auto")
                        {
                            this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine);
                        }
                        else
                        {
                            if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == this.IDCentro))
                            {
                                this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine);
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




