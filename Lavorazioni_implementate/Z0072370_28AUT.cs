using Entities.Models.DbApplication;
using LibraryLavorazioni.Utility;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Data;


namespace LibraryLavorazioni.Lavorazioni
{

    [ProcessingLavorazioneAttribute(NomeProceduraProgramma: "Z0072370_28AUT")]
    public class Z0072370_28AUT : BaseLavorazione
    {
        private readonly Logger logger;


        public Z0072370_28AUT(IDbContextFactory<DematReportsContext> repoContext, ILavorazioniConfigManager lavorazioniConfigManager) : base(repoContext, lavorazioniConfigManager)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }


        public override DataTable SetDatiDemat()
        {
            var tableData = new DataTable("Z0072370_28AUT");

            SqlCommand command;
            SqlDataAdapter adapter = new SqlDataAdapter();

            var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
            var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

            var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
            var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

            //Scansione
            if (IDFaseLavorazione == 4)
            {

                string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,num_pag)) as Pagine
                                 from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                                 where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan and department = 'GENOVA'
                                 group by OP_SCAN,convert(date, DATA_SCAN)";

                using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnDematReports))
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
                    adapter.Fill(tableData);
                }
            }


            //Data Entry
            if (IDFaseLavorazione == 5)
            {
                string query = @"select OP_INDEX as operatore, convert(date, DATA_INDEX) as DataLavorazione, COUNT(*) as Documenti, SUM(convert(int,NUM_PAG))/2 AS Fogli, SUM(convert(int,NUM_PAG)) as Pagine
                                 from Z0072370_RDMKT_28AUT_GE_UDA_DETTAGLIO
                                 where convert(date, DATA_INDEX) >= @startDataDe and convert(date, DATA_INDEX) <= @endDataDe and department = 'GENOVA'
                                 group by OP_INDEX, convert(date, DATA_INDEX)";

                using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnDematReports))
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
                    adapter.Fill(tableData);
                }
            }

            return tableData;


        }


    }
}




