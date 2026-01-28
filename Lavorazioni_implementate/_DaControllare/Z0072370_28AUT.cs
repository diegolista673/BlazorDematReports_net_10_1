using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;


namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0072370_28AUT")]
    public class Z0072370_28AUT : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0072370_28AUT(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore
)
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
            this.TableData = new DataTable("Z0072370_28AUT");

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
                    string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,num_pag)) as Pagine
                                    from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                                    where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                                    group by OP_SCAN,convert(date, DATA_SCAN)"; 


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
                        adapter.Fill(this.TableData);
                    }
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,NUM_PAG))/2 AS Fogli, SUM(convert(int,NUM_PAG)) as Pagine
                                     from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                                     where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
                                     group by OP_INDEX, convert(date, DATA_INDEX)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
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




