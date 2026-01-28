using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;


namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0090407_RDMKT_INWIT_MR_DOC_UDA")]
    public class Z0090407_RDMKT_INWIT_MR_DOC_UDA : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0090407_RDMKT_INWIT_MR_DOC_UDA(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Z0090407_RDMKT_INWIT_MR_DOC_UDA");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDe = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDe = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,num_pag))/2 as Fogli, SUM(convert(int,num_pag)) as Pagine
                                    from Z0090407_RDMKT_INWIT_MR_DOC_UDA
                                    where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and CAMPO_4 ='GENOVA'
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


                //DateEntry
                if (IDFaseLavorazione == 5)
                {
                    string query = @"select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,num_pag))/2 as Fogli, SUM(convert(int,num_pag)) as Pagine
                                    from Z0090407_RDMKT_INWIT_MR_DOC_UDA
                                    where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe and CAMPO_4 ='GENOVA'
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
                        command.Parameters.AddWithValue("@startDataDe", startDataDe);
                        command.Parameters.AddWithValue("@endDataDe", endDataDe);
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




