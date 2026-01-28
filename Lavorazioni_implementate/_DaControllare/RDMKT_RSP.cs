using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("RDMKT_RSP")]
    public class RDMKT_RSP : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public RDMKT_RSP(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("RDMKT_RSP");

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
                    //trova tutte le tabelle RSP non vuote
                    string queryTabelle = @"SELECT distinct(T.name) TableName FROM sys.tables T JOIN sys.sysindexes I ON T.OBJECT_ID = I.ID where t.name LIKE '%_RSP_%_UDA_DETTAGLIO' and i.Rows >0 ";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(queryTabelle, connection);
                        command.CommandTimeout = 0;

                        SqlDataReader reader = command.ExecuteReader();
                        List<string> lstTabelle = new List<string>();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                lstTabelle.Add(reader.GetString(0));
                            }
                        }

                        reader.Close();

                        foreach (string tabName in lstTabelle)
                        {
                            string query = "select OP_INDEX as operatore,CONVERT(date, DATA_INDEX) as DataLavorazione, Count(*) as Documenti, SUM(convert(int,isnull(NUM_PAG,0))/2) AS Fogli, SUM(convert(int,isnull(NUM_PAG,0))) AS Pagine " +
                                           "from " + tabName + ' ' +
                                           "where CONVERT(date, DATA_INDEX) >= @startDataDE and CONVERT(date, DATA_INDEX) <= @endDataDe " +
                                           "group by OP_INDEX,CONVERT(date, DATA_INDEX) ";

                            var cmd = new SqlCommand(query, connection);
                            cmd.CommandTimeout = 0;
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@startDataDe", startDataDE);
                            cmd.Parameters.AddWithValue("@endDataDe", endDataDE);
                            adapter.SelectCommand = cmd;
                            adapter.Fill(table);
                        }
                    }
                }



                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    //trova tutte le tabelle RSP non vuote
                    string queryTabelle = @"SELECT distinct(T.name) TableName FROM sys.tables T JOIN sys.sysindexes I ON T.OBJECT_ID = I.ID where t.name LIKE '%_RSP_%_UDA_DETTAGLIO' and i.Rows >0 ";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(queryTabelle, connection);
                        command.CommandTimeout = 0;

                        SqlDataReader reader = command.ExecuteReader();
                        List<string> lstTabelle = new List<string>();

                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                lstTabelle.Add(reader.GetString(0));
                            }
                        }

                        reader.Close();

                        foreach (string tabName in lstTabelle)
                        {
                            string query = "select OP_SCAN as operatore,CONVERT(date, DATA_SCAN) as DataLavorazione, Count(*) as Documenti, SUM(convert(int,isnull(NUM_PAG,0))/2) AS Fogli, SUM(convert(int,isnull(NUM_PAG,0))) AS Pagine " +
                                           "from " + tabName + ' ' +
                                           "where CONVERT(date, DATA_SCAN) >= @startDataScan and CONVERT(date, DATA_SCAN,104) <= @endDataScan " +
                                           "group by OP_SCAN,CONVERT(date, DATA_SCAN) " +
                                           "order by CONVERT(date, DATA_SCAN)";

                            var cmd = new SqlCommand(query, connection);
                            cmd.CommandTimeout = 0;
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@startDataScan", startDataScan);
                            cmd.Parameters.AddWithValue("@endDataScan", endDataScan);
                            adapter.SelectCommand = cmd;
                            adapter.Fill(table);
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




