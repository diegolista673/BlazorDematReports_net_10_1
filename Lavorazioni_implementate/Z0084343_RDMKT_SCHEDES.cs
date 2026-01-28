using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0084343_RDMKT_SCHEDES")]
    public class Z0084343_RDMKT_SCHEDES : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0084343_RDMKT_SCHEDES(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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

            this.TableData = new DataTable("Z0084343_RDMKT_SCHEDES");

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


                    string query = @"select OP_SCAN  as Operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti
                                     from Z0084343_RDMKT_SCHEDES_UDA_dettaglio
                                     where department = 'GENOVA' and convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                                     group by OP_SCAN,convert(date, DATA_SCAN)
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
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
                    }
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select OP_INDEX  as Operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti
                                    from Z0084343_RDMKT_SCHEDES_UDA_dettaglio
                                    where department = 'GENOVA' and convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe
                                    group by OP_INDEX,convert(date, DATA_INDEX)";


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




