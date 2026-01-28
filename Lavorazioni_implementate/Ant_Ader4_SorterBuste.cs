using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("ANT_ADER4_SORTER_BUSTE")]
    public class Ant_Ader4_SorterBuste : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public Ant_Ader4_SorterBuste(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore
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
            this.TableData = new DataTable("ANT_ADER4_SORTER_BUSTE");

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

                    string query = @"SELECT Username as Operatore,convert(date, dateTime_acquisizione) as DataLavorazione,count(coduniF) as Documenti, count(coduniF) as Fogli, (count(coduniF) *2 ) as Pagine 
                                     FROM [GesimCheck_Local_Produzione].[dbo].[Tab_Lavorato] where convert(date,dateTime_acquisizione) >= @startDataScan and convert(date,dateTime_acquisizione) <= @endDataScan
                                     group by Username,CONVERT(date, dateTime_acquisizione)";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnAder4SorterVips))
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
                ErrorMessage = ex.Message;
                logger.Error(ex.Message);

                if (this.LavorazioneInRichiestaSingola == true)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    return this.TableData;
                }
            }


        }

  
    }
}




