using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0013835_FERRERO")]
    public class Z0013835_FERRERO : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0013835_FERRERO(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Z0013835_FERRERO");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                //Data Entry
                if (IDFaseLavorazione == 5)
                {
                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti, Sum(t.Totali) as Fogli, Sum(t.Totali)*2 as Pagine
                                    from (
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'esiti' as Tipologia 
                                        FROM Z0013835_RAC_DATI
                                        WHERE scatola LIKE '%Z0013835_E%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        union all
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
                                        FROM Z0013835_RAC_DATI
                                        WHERE scatola LIKE '%Z0013835_I%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                    ) as t
                                    GROUP By t.Operatore,t.DataLavorazione
                                    order by t.DataLavorazione";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
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




