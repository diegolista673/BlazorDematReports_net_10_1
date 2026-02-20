using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Helpers;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Implementazione di default per la gestione delle lavorazioni.
    /// <para>
    /// Fornisce la logica standard per la lettura dei dati di produzione tramite query SQL associate alle fasi di lavorazione.
    /// Utilizza i servizi di normalizzazione, gestione operatori e configurazione tramite dependency injection.
    /// </para>
    /// </summary>
    [ProcessingLavorazione(NomeProceduraProgramma: "")]
    public class LavorazioneDefault : BaseLavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="LavorazioneDefault"/>.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="lavorazioniConfigManager">Servizio di configurazione lavorazioni.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        public LavorazioneDefault(
            INormalizzatoreOperatori normalizzatoreOperatori,
            IGestoreOperatoriDatiLavorazione gestoreOperatoriDati,
            ILavorazioniConfigManager lavorazioniConfigManager,
            IElaboratoreDatiLavorazione elaboratoreDati
        ) : base(normalizzatoreOperatori, gestoreOperatoriDati, elaboratoreDati)
        {
            _lavorazioniConfigManager = lavorazioniConfigManager;
            ElaboraDatiLavorazione = elaboratoreDati;
            logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
        }

        /// <summary>
        /// Legge i dati di produzione tramite una query SQL associata alla fase di lavorazione.
        /// </summary>
        /// <param name="nomeConnessione">Nome della proprietà di configurazione della connessione.</param>
        /// <param name="queryDaEseguire">Query SQL da eseguire.</param>
        /// <param name="nomeProcedura">Nome della procedura di lavorazione.</param>
        /// <param name="startDate">Data di inizio periodo di ricerca.</param>
        /// <param name="endDate">Data di fine periodo di ricerca.</param>
        /// <returns>
        /// Un <see cref="DataTable"/> contenente i dati di produzione letti dal database.
        /// </returns>
        public virtual async Task<DataTable> SetDatiDematAsync(string nomeConnessione, string queryDaEseguire, string nomeProcedura, DateTime startDate, DateTime endDate)
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: logger);

            var tableData = new DataTable(nomeProcedura);
            string cnxn = _lavorazioniConfigManager!.GetType().GetProperty(nomeConnessione)!.GetValue(_lavorazioniConfigManager)!.ToString()!;

            using (SqlConnection connection = new SqlConnection(cnxn))
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                SqlCommand command = new SqlCommand(queryDaEseguire, connection);
                command.CommandTimeout = 0;
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@startDate", startDate);
                command.Parameters.AddWithValue(@"endDate", endDate);
                SqlDataAdapter adapter = new SqlDataAdapter();
                adapter.SelectCommand = command;
                adapter.Fill(tableData);
            }

            EsitoLetturaDato = true;
            return tableData;
        }

        /// <summary>
        /// Implementazione del metodo astratto della classe base per la lettura dei dati di lavorazione.
        /// Per LavorazioneDefault, restituisce una lista vuota poiché i dati vengono gestiti 
        /// tramite il metodo SetDatiDematAsync che restituisce un DataTable.
        /// </summary>
        /// <returns>Lista vuota di DatiLavorazione.</returns>
        public override Task<List<DatiLavorazione>> SetDatiDematAsync()
        {
            QueryLoggingHelper.LogQueryExecution(nlogLogger: logger);
            return Task.FromResult(new List<DatiLavorazione>());
        }
    }
}
