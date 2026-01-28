using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0017181_POSTEMOBILE")]
    public class Z0017181_POSTEMOBILE : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0017181_POSTEMOBILE(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Postemobile");
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

                    string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(CAMPO_3) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                     from Z0017181_RDMKT_PM_GECT_UDA
                                     where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by DataLavorazione";



                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();

                            using (var cmd = new SqlCommand(query, connection))
                            {

                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("@startDataScan", startDataScan);
                                cmd.Parameters.AddWithValue("@endDataScan", endDataScan);
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

                    string query = @"select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(distinct(codice_mazzetta)) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                     from Z0017181_RDMKT_PM_GECT_UDA_DETTAGLIO
                                     where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
                                     group by OP_INDEX,convert(date, DATA_INDEX)
                                     order by DataLavorazione";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();

                            using (var cmd = new SqlCommand(query, connection))
                            {

                                cmd.Parameters.Clear();
                                cmd.Parameters.AddWithValue("@startDataDe", startDataScan);
                                cmd.Parameters.AddWithValue("@endDataDe", endDataScan);
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




