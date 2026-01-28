using System.Data;
using System.Data.SqlClient;
using NLog;
using System.Text.RegularExpressions;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute( NomeProceduraProgramma: "Z0092154_RDMKT_ESTINTI_UDA")]
    public class Z0092154_RDMKT_ESTINTI_UDA : BaseLavorazione
    {
        private readonly Logger logger;
        public Z0092154_RDMKT_ESTINTI_UDA(IDbContextFactory<DematReportsContext> repoContext, ILavorazioniConfigManager lavorazioniConfigManager) : base(repoContext, lavorazioniConfigManager)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }


        public override DataTable SetDatiDemat()
        {
            var tableData = new DataTable("Z0092154_RDMKT_ESTINTI_UDA");

            SqlCommand command;
            SqlDataAdapter adapter = new SqlDataAdapter();

            var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
            var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");
            var table = new DataTable();

            //Scansione Documento
            if (IDFaseLavorazione == 4)
            {
                string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti,sum(ISNULL(CAST(NUM_PAG AS int),0))/2 as fogli, sum(ISNULL(CAST(NUM_PAG AS int),0)) as pagine
                                from [RHM_POSTEL].[dbo].[Z0092154_RDMKT_ESTINTI_UDA]
                                where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
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
                    adapter.Fill(tableData);
                }
            }

            return tableData;

        }
    }
}




