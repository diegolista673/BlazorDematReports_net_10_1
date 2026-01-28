using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0008621_RDMKT_GARA_ATER")]
    public class Z0008621_RDMKT_GARA_ATER : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0008621_RDMKT_GARA_ATER(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Z0008621_RDMKT_GARA_ATER");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                //Scansione
                //faccio distinct del codice mazzetta perchè può ripetersi ed essere così composta da più documenti, ma in realtà per produzione è un unico fascicolo da conteggiare
                //filtro su nome operatore per il centro di appartenenza perchè il department al momento è sempre Pomezia anche se scansionato a Genova
                if (IDFaseLavorazione == 4)
                {
                    string query = @"select OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, count(distinct(codice_mazzetta)) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                                    from Z0008621_RDMKT_GARA_ATER_UDA_DETTAGLIO
                                    where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan 
                                    group by OP_SCAN,convert(date, DATA_SCAN)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        var table = new DataTable();
                        command = new SqlCommand(query, connection);
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




