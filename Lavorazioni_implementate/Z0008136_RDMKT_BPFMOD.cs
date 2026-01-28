using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0008136_RDMKT_BPFMOD")]
    public class Z0008136_RDMKT_BPFMOD : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0008136_RDMKT_BPFMOD(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
            this.TableData = new DataTable("Z0008136_RDMKT_BPFMOD");

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


                    string query = @"select OP_INDEX as Operatore, convert(date, DATA_INDEX) as DataLavorazione,
                                     SUM(CASE WHEN STATO_DOC = 'Indicizzato' or STATO_DOC = 'Pubblicato' THEN 1 ELSE 0 END) AS documenti,
                                     SUM(CASE WHEN STATO_DOC = 'Indicizzato' or STATO_DOC = 'Pubblicato' THEN 1 ELSE 0 END) AS Fogli,
                                     SUM(CASE WHEN STATO_DOC = 'Indicizzato' or STATO_DOC = 'Pubblicato' THEN 1 ELSE 0 END) * 2 AS Pagine,
                                     SUM(CASE WHEN STATO_DOC = 'Da Scartare' or STATO_DOC = 'Scartato' or STATO_DOC = 'Sospeso' THEN 1 ELSE 0 END) AS Scarti
                                     from Z0008136_RDMKT_BPFMOD_UDA_DETTAGLIO
                                     where convert(date, DATA_INDEX) >= @startDataDE and convert(date, DATA_INDEX) <= @endDataDE
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
                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(TableData);
                    }
                }

                EsitoLetturaDato = true;
                return TableData;
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




