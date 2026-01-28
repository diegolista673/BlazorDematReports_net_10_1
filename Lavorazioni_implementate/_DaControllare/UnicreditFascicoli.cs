using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;


namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("UnicreditFascicoli")]
    public class UnicreditFascicoli : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public UnicreditFascicoli(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("UnicreditFascicoli");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                if (IDFaseLavorazione == 4)
                {

                    string query = @"select Username as Operatore, convert(date,CONVERT(VARCHAR(10), DateStamp)) as DataLavorazione, count(distinct(Barcode_PATCH_B)) as Documenti, count(*)/2 as Fogli, count(*) as Pagine 
                                    from TMP_PAGE_ALL_FASCICOLI WITH (NOLOCK)
                                    where CONVERT(date, DateStamp) >= @startDataScan and convert(date, DateStamp) <= @endDataScan and ( Postazione = '1' or Postazione = '2' ) 
                                    group by Username, CONVERT(VARCHAR(10), DateStamp)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
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

                ////Data Entry UpStream totale pagine
                if (IDFaseLavorazione == 45)
                {
                    string query = @"select CorrUPS_User  as Operatore, convert(date, CorrUPS_TimeStamp) as DataLavorazione, COUNT(*) as Pagine
                                     from TMP_PAGE_ALL_FASCICOLI WITH (NOLOCK)
                                     where CorrUPS_User <> '' and convert(date, CorrUPS_TimeStamp) >= @startDataDE and convert(date, CorrUPS_TimeStamp) <= @endDataDE
                                     group by CorrUPS_User,convert(date, CorrUPS_TimeStamp)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDE", startDataDE);
                        command.Parameters.AddWithValue("@endDataDE", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
                    }
                }


                ////Data Entry DownStream totale pagine
                if (IDFaseLavorazione == 46)
                {


                    string query = @"select CorrDWS_User as Operatore, convert(date, CorrDWS_TimeStamp) as DataLavorazione, COUNT(*) as Pagine
                                     from TMP_PAGE_ALL_FASCICOLI WITH (NOLOCK)
                                     where CorrDWS_User <> '' AND CorrDWS_User <> '1' and convert(date, CorrDWS_TimeStamp) >= @startDataDe and convert(date, CorrDWS_TimeStamp) <= @endDataDe
                                     group by CorrDWS_User, convert(date, CorrDWS_TimeStamp)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
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




