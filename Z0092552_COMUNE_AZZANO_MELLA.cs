using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0092552_COMUNE_AZZANO_MELLA")]
    public class Z0092552_COMUNE_AZZANO_MELLA  : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0092552_COMUNE_AZZANO_MELLA (ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }

        public override DataTable GetDatiDemat()
        {

            switch (this.IDCentro)
            {
                //Verona
                case 1:
                    LavorazioneImplementataByCentro = false;
                    break;
                //Genova
                case 2:
                    LavorazioneImplementataByCentro = true;
                    FillTableGenova();
                    break;

                default:
                    LavorazioneImplementataByCentro = false;
                    break;
            }

            return this.TableData;

        }


        private DataTable FillTableGenova()
        {
            this.TableData = new DataTable("Z0092552_COMUNE_AZZANO_MELLA");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var table = new DataTable();

                //scansione che prevede la sola fase di scansione (essendo presenti Documenti e Tavole,
                //serve reperire solo il dato relativo ai documenti inserito filtro nel CAMPO_U1).
                if (IDFaseLavorazione == 4)
                {
                    string query = @"SELECT OP_SCAN as operatore,convert(date, DATA_SCAN) as DataLavorazione,COUNT(*) as Documenti,sum(ISNULL(CAST(NUM_PAG AS int),0))/2 as fogli, sum(ISNULL(CAST(NUM_PAG AS int),0)) as pagine
                                    from Z0092552_RDMKT_PE_AZZANO_MELLA_PE_UDA  
                                    where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and CAMPO_U1 ='DOCUMENTO' and CAMPO_4 = 'GENOVA'
                                    GROUP BY OP_SCAN,convert(date, DATA_SCAN)
                                    order by convert(date, DATA_SCAN)";


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




