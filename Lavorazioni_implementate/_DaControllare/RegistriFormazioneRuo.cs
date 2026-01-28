using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("RegistriFormazioneRuo")]
    public class RegistriFormazioneRuo : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public RegistriFormazioneRuo(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("RegistriFormazioneRuo");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("dd/MM/yyyy");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("dd/MM/yyyy");

                var startDataDE = StartDataLavorazione.ToString("dd/MM/yyyy");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("dd/MM/yyyy");


                //Scansione
                if (IDFaseLavorazione == 4)
                {

                    string query = @"select OPERATORE_SCAN as Operatore,convert(date,CONVERT(VARCHAR(10), DATA_SCAN)) as DataLavorazione, COUNT(*) AS Documenti, SUM(convert(int,NUMERO_PAGINE)) AS pagine, SUM(convert(int,NUMERO_PAGINE))/2 AS fogli 
                                     from REPORT_GIORNALIERI 
                                     where convert(VARCHAR(10),DATA_SCAN)= @startDataScan and CONVERT(VARCHAR(10), DATA_SCAN) <= @startDataScan 
                                     group by OPERATORE_SCAN, CONVERT(VARCHAR(10), DATA_SCAN)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnProduzioneGed))
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

                //Data Entry UpStream totale pagine
                if (IDFaseLavorazione == 45)
                {
                    string query = @"";

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

                //Data Entry DownStream totale pagine
                if (IDFaseLavorazione == 46)
                {


                    string query = @"select CorrDWS_User as Operatore, convert(date, CorrDWS_TimeStamp) as DataLavorazione, COUNT(*) as Pagine
                                     from TMP_PAGE_ALL
                                     where CorrDWS_User <> '' AND CorrDWS_User <> '1' and convert(date, CorrDWS_TimeStamp) >= @startDataDe and convert(date, CorrDWS_TimeStamp) <= @endDataDe
                                     group by CorrDWS_User, convert(date, CorrDWS_TimeStamp)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnUnicredit))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
                    }
                }


                //DiDiScard totale documenti messi in restore da operatore
                if (IDFaseLavorazione == 47)
                {

                    string query = @"select RTRIM(LTRIM(p.operatore)) as operatore, count(*) as documenti,convert(date, p.dataLavorazione, 103) as dataLavorazione
                                     from (
                                        SELECT Barcode_PATCH_B ,SUBSTRING(NOTE, CHARINDEX('=', NOTE)+1, CHARINDEX('Time',NOTE)- 6) as operatore, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10) as dataLavorazione
                                        from TMP_PAGE_ALL
                                        where NOTE like '%TimeStamp%Restore PATCH_B%' and convert(date, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10), 103) >=@startDataDe and convert(date, SUBSTRING(NOTE, CHARINDEX('TimeStamp = ', NOTE ) + 12, 10), 103) <= @endDataDe
                                        group by note,Barcode_PATCH_B) as p
                                     group by p.operatore, p.dataLavorazione";


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




