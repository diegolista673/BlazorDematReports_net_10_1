using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Equitalia_3")]
    public class Equitalia_3 : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public Equitalia_3(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore
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
            this.TableData = new DataTable("Equitalia_3");

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

                    string query = @"SELECT Operatore, convert(date, data_scansione) as DataLavorazione,TOTALE_DOC as Documenti, TOTALE_DOC as Fogli, (TOTALE_DOC *2 ) as Pagine
                                     FROM EQUITALIA_3_OPERATORI 
                                     WHERE  convert(date, data_scansione) >= @startDataScan and convert(date, data_scansione) <= @endDataScan ";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnEquitaliaVR))
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




