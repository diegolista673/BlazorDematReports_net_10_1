using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;


namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0075454_RDMKT_UIBM")]
    public class Z0075454_RDMKT_UIBM : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0075454_RDMKT_UIBM(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore
)
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
            this.TableData = new DataTable("Z0075454_RDMKT_UIBM");

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

                    string query = @"select OP_SCAN as operatore, convert(date, DATA_PUBBL) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,num_pag))/2 as fogli, SUM(convert(int,num_pag)) as Pagine
                                     from Z0075454_RDMKT_UIBM_UDA
                                     where convert(date, DATA_PUBBL) >= @startDataScan and convert(date, DATA_PUBBL) <= @endDataScan
                                     group by OP_SCAN,convert(date, DATA_PUBBL)";


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




