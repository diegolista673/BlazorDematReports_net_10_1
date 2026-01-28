using LibraryLavorazioni.Utility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni.Lavorazioni
{
    //    [ProcessingLavorazioneAttribute("ADER_EQUITALIA_4_SORTER_BUSTE")]
    //    public class Ader_Equitalia_4_SorterBuste : Lavorazione
    //    {
    //        private readonly Logger logger;
    //        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
    //        private readonly NormalizzaOperatore _normalizzaOperatore;


    //        public Ader_Equitalia_4_SorterBuste(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore
    //)
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
    //                    LavorazioneImplementataByCentro = true;
    //                    FillTableGenova();
    //                    break;

    //                default:
    //                    LavorazioneImplementataByCentro = false;
    //                    break;
    //            }


    //            return this.TableData;

    //        }

    //        private DataTable FillTableVerona()
    //        {
    //            this.TableData = new DataTable("ADER_EQUITALIA_4_SORTER_BUSTE");

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

    //                    string query = @"SELECT Operatore, convert(date, DataScansione) as DataLavorazione,TotaleDocumenti as Documenti, TotaleDocumenti as Fogli, (TotaleDocumenti *2 ) as Pagine, Scarti
    //                                     FROM Ader_Equitalia4_Operatori_VR
    //                                     WHERE convert(date, DataScansione) >= @startDataScan and convert(date, DataScansione) <= @endDataScan and TipoScansione ='Sorter_Buste' ";

    //                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnProduzioneGed))
    //                    {
    //                        if (connection.State != ConnectionState.Open)
    //                        {
    //                            connection.Open();
    //                        }
    //                        command = new SqlCommand(query, connection);
    //                        command.Parameters.Clear();
    //                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
    //                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
    //                        adapter.SelectCommand = command;
    //                        adapter.Fill(this.TableData);
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
    //                    return this.TableData;
    //                }
    //            }


    //        }

    //        private DataTable FillTableGenova()
    //        {
    //            this.TableData = new DataTable("ADER_EQUITALIA_4_SORTER_BUSTE");

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

    //                    string query = @"SELECT Operatore, convert(date, DataScansione) as DataLavorazione,TotaleDocumenti as Documenti, TotaleDocumenti as Fogli, (TotaleDocumenti *2 ) as Pagine, Scarti
    //                                     FROM Ader_Equitalia4_Operatori_GE 
    //                                     WHERE  convert(date, DataScansione) >= @startDataScan and convert(date, DataScansione) <= @endDataScan and TipoScansione ='Sorter_Buste' ";

    //                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnProduzioneGed))
    //                    {
    //                        if (connection.State != ConnectionState.Open)
    //                        {
    //                            connection.Open();
    //                        }
    //                        command = new SqlCommand(query, connection);
    //                        command.Parameters.Clear();
    //                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
    //                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
    //                        adapter.SelectCommand = command;
    //                        adapter.Fill(this.TableData);
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
    //                    return this.TableData;
    //                }
    //            }


    //        }
    //    }
}




