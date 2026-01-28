using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0084886_RDMKT_GENERALI_WELION")]
    public class Z0084886_RDMKT_GENERALI_WELION : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0084886_RDMKT_GENERALI_WELION(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
                    LavorazioneImplementataByCentro = true;
                    FillTableVerona();
                    break;
                //Genova
                case 2:
                    LavorazioneImplementataByCentro = false;
                    break;
                default:
                    LavorazioneImplementataByCentro = false;
                    break;
            }

            return this.TableData;

        }



        private DataTable FillTableVerona()
        {
            this.TableData = new DataTable("Z0084886_RDMKT_GENERALI_WELION");

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

                    //old tabella
                    //string query = @"select OP_SCAN as Operatore,convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) as DataLavorazione, count(*) as Documenti, SUM(convert(int,NUM_PAG))/2 AS Fogli, SUM(convert(int,NUM_PAG)) as Pagine
                    //                 from Z0084886_RDMKT_GENERALI_AGENZIE 
                    //                 where substring(bancale,1,6)='WELBAN' and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @startDataScan and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @endDataScan
                    //                 group by OP_SCAN,CONVERT(VARCHAR(10), DATA_SCAN)
                    //                 order by CONVERT(VARCHAR(10), DATA_SCAN)";



                    string query = @"select OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, count(*) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                                     from [Z0084886_RDMKT_GEN_WELION_UDA_DETTAGLIO]
                                     where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and CAMPO_DE_10 ='WE_C02'
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by convert(date, DATA_SCAN)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
                    }
                }


                //DataEntry
                if (IDFaseLavorazione == 5)
                {

                    //old tabella
                    //string query = @"select OP_SCAN as Operatore,convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) as DataLavorazione, count(*) as Documenti, SUM(convert(int,NUM_PAG))/2 AS Fogli, SUM(convert(int,NUM_PAG)) as Pagine
                    //                 from Z0084886_RDMKT_GENERALI_AGENZIE 
                    //                 where substring(bancale,1,6)='WELBAN' and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @startDataScan and convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) >= @endDataScan
                    //                 group by OP_SCAN,CONVERT(VARCHAR(10), DATA_SCAN)
                    //                 order by CONVERT(VARCHAR(10), DATA_SCAN)";



                    string query = @"select OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, count(*) as Documenti, SUM(convert(int, NUM_PAG)) / 2 AS Fogli, SUM(convert(int, NUM_PAG)) as Pagine
                                     from [Z0084886_RDMKT_GEN_WELION_UDA_DETTAGLIO]
                                     where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and CAMPO_DE_10 ='WE_C02'
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by convert(date, DATA_SCAN)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
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




