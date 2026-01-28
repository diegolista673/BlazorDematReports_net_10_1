//using LibraryUtility;
//using Microsoft.Data.SqlClient;
//using NLog;
//using System.Data;

//namespace LibraryLavorazioni
//{
//    [ProcessingLavorazioneAttribute("Unicredit")]
//    public class Unicredit : Lavorazione
//    {
//        private readonly Logger logger;
//        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
//        private readonly NormalizzaOperatore _normalizzaOperatore;

//        public Unicredit(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
//        {
//            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
//            _lavorazioniConfigManager = lavorazioniConfigManager;
//            _normalizzaOperatore = normalizzaOperatore;
//        }


//        public override DataTable GetDatiDemat()
//        {

//            switch (this.IDCentro)
//            {
//                //Verona
//                case 1:
//                    LavorazioneImplementataByCentro = true;
//                    FillTableVerona();
//                    break;
//                //Genova
//                case 2:
//                    LavorazioneImplementataByCentro = false;
//                    break;

//                default:
//                    LavorazioneImplementataByCentro = false;
//                    break;
//            }

//            return this.TableData;

//        }


//        private DataTable FillTableVerona()
//        {
//            this.TableData = new DataTable("Unicredit");

//            try
//            {
//                SqlCommand command;
//                SqlDataAdapter adapter = new SqlDataAdapter();

//                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


//                //Scansione
//                if (IDFaseLavorazione == 4)
//                {
//                    string query = @"select Username as Operatore, convert(date,CONVERT(VARCHAR(10), DateStamp)) as DataLavorazione, count(distinct(Barcode_PATCH_B)) as Documenti, count(*)/2 as Fogli, count(*) as Pagine
//                                    from TMP_PAGE_ALL WITH (NOLOCK)
//                                    where CONVERT(date, DateStamp) >= @startDataScan and convert(date, DateStamp) <= @endDataScan and ( Postazione = '1' or Postazione = '2' )
//                                    group by Username, CONVERT(VARCHAR(10), DateStamp)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }
//                        command = new SqlCommand(query, connection);
//                        command.CommandTimeout = 0;
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
//                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
//                        adapter.SelectCommand = command;
//                        adapter.Fill(TableData);
//                    }
//                }

//                //Data Entry UpStream totale pagine
//                if (IDFaseLavorazione == 45)
//                {

//                    string query = @"select CorrUPS_User as Operatore, convert(date, CorrUPS_TimeStamp) as DataLavorazione, COUNT(*) as Pagine
//                                     from TMP_PAGE_ALL WITH (NOLOCK)
//                                     where CorrUPS_User <> '' and convert(date, CorrUPS_TimeStamp) >= @startDataDE and convert(date, CorrUPS_TimeStamp) <= @endDataDE
//                                     group by CorrUPS_User, convert(date, CorrUPS_TimeStamp)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }
//                        command = new SqlCommand(query, connection);
//                        command.CommandTimeout = 0;
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataDE", startDataDE);
//                        command.Parameters.AddWithValue("@endDataDE", endDataDE);
//                        adapter.SelectCommand = command;
//                        adapter.Fill(TableData);
//                    }
//                }

//                //Data Entry DownStream totale pagine
//                if (IDFaseLavorazione == 46)
//                {


//                    string query = @"select CorrDWS_User as Operatore, convert(date, CorrDWS_TimeStamp) as DataLavorazione, COUNT(*) as Pagine
//                                     from TMP_PAGE_ALL WITH (NOLOCK)
//                                     where CorrDWS_User <> '' AND CorrDWS_User <> '1' and convert(date, CorrDWS_TimeStamp) >= @startDataDe and convert(date, CorrDWS_TimeStamp) <= @endDataDe
//                                     group by CorrDWS_User, convert(date, CorrDWS_TimeStamp)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
//                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
//                        adapter.SelectCommand = command;
//                        adapter.Fill(TableData);
//                    }
//                }


//                //DiDiScard totale documenti messi in restore da operatore
//                if (IDFaseLavorazione == 47)
//                {

//                    string query = @"select RTRIM(LTRIM(p.operatore)) as operatore, count(distinct(Barcode_PATCH_B)) as documenti,convert(date, p.dataLavorazione, 103) as dataLavorazione
//                                     from (
//	                                     SELECT Barcode_PATCH_B, SUBSTRING(NOTE, CHARINDEX('=', NOTE)+1, CHARINDEX('TimeStamp',NOTE)- 6) as operatore, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10) as dataLavorazione
//	                                     from TMP_PAGE_ALL WITH (NOLOCK)
//	                                     where NOTE like '%DIDISCARD --->Restore PATCH_B%' and convert(date, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10), 103) >= @startDataDe and convert(date, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10), 103) <= @endDataDe
//	                                     group by note,Barcode_PATCH_B) as p 
//                                     group by p.operatore, p.dataLavorazione";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.CommandTimeout = 0;
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
//                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
//                        adapter.SelectCommand = command;
//                        adapter.Fill(TableData);
//                    }
//                }

//                EsitoLetturaDato = true;
//                return this.TableData;
//            }
//            catch (Exception ex)
//            {
//                EsitoLetturaDato = false;
//                Error = true;
//                base.ErrorMessage = ex.Message;
//                logger.Error(ex.Message);

//                if (this.LavorazioneInRichiestaSingola == true)
//                {
//                    throw new Exception(ex.Message);
//                }
//                else
//                {
//                    return TableData;
//                }
//            }


//        }
//    }
//}




