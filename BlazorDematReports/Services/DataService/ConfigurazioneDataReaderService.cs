using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione dei dati di configurazione fonte dati (lettura).
    /// </summary>
    public interface IConfigurazioneDataReaderService
    {
        /// <summary>
        /// Carica le fasi per una procedura specifica.
        /// </summary>
        Task<List<FasiLavorazione>> GetFasiByProceduraAsync(int idProcedura);

        /// <summary>
        /// Carica la configurazione per editing.
        /// </summary>
        Task<ConfigurazioneFontiDati?> GetConfigurazioneFontiDatiAsync(int idConfigurazione);

        /// <summary>
        /// Ottiene la lista di connection strings da configurazione.
        /// </summary>
        List<string> GetConnectionStrings(IConfiguration configuration);

        /// <summary>
        /// Carica i centri lavorazione associati alle fasi di una procedura.
        /// </summary>
        Task<List<CentriLavorazione>> GetCentriByProceduraAsync(int idProcedura);
    }

    /// <summary>
    /// Implementazione del servizio per la gestione dei dati di configurazione fonte dati (lettura).
    /// </summary>
    public class ConfigurazioneDataReaderService : IConfigurazioneDataReaderService
    {
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ConfigurazioneDataReaderService> logger;

        public ConfigurazioneDataReaderService(IDbContextFactory<DematReportsContext> contextFactory, ILogger<ConfigurazioneDataReaderService> logger)
        {
            this.contextFactory = contextFactory;
            this.logger = logger;
        }

        public async Task<List<FasiLavorazione>> GetFasiByProceduraAsync(int idProcedura)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                
                var fasi = await context.LavorazioniFasiDataReadings
                    .Where(l => l.IdProceduraLavorazione == idProcedura)
                    .Select(l => l.IdFaseLavorazioneNavigation)
                    .Where(f => f != null)
                    .OrderBy(f => f!.FaseLavorazione)
                    .ToListAsync();

                return fasi.Where(f => f != null).ToList()!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel caricamento fasi per procedura {IdProcedura}", idProcedura);
                return new List<FasiLavorazione>();
            }
        }

        public async Task<ConfigurazioneFontiDati?> GetConfigurazioneFontiDatiAsync(int idConfigurazione)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                
                return await context.ConfigurazioneFontiDatis
                    .Include(c => c.ConfigurazioneFaseCentros)
                    .FirstOrDefaultAsync(c => c.IdConfigurazione == idConfigurazione);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel caricamento configurazione {IdConfigurazione}", idConfigurazione);
                return null;
            }
        }

        public List<string> GetConnectionStrings(IConfiguration configuration)
        {
            try
            {
                return configuration.GetSection("ConnectionStrings")
                    .GetChildren()
                    .Select(c => c.Key)
                    .Where(key => !key.StartsWith("_"))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel caricamento connection strings");
                return new List<string>();
            }
        }

        public async Task<List<CentriLavorazione>> GetCentriByProceduraAsync(int idProcedura)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                
                // Otteniamo il centro dalla procedura
                var procedura = await context.ProcedureLavorazionis
                    .Where(p => p.IdproceduraLavorazione == idProcedura)
                    .Select(p => p.Idcentro)
                    .FirstOrDefaultAsync();

                if (procedura > 0)
                {
                    var centro = await context.CentriLavoraziones
                        .FirstOrDefaultAsync(c => c.Idcentro == procedura);
                    
                    if (centro != null)
                        return new List<CentriLavorazione> { centro };
                }

                return new List<CentriLavorazione>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel caricamento centri per procedura {IdProcedura}", idProcedura);
                return new List<CentriLavorazione>();
            }
        }
    }
}
