using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using DataReading.Infrastructure;
using Entities.Converters;
using Entities.Enums;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using MudBlazor;



namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Service per la gestione delle configurazioni fonti dati, mapping fasi/centro e task associati.
    /// Implementa logica di business e orchestrazione Hangfire.
    /// </summary>
    public class ServiceConfigurazioneFontiDati : ServiceBase<ConfigurazioneFontiDati>, IServiceConfigurazioneFontiDati
    {
        
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceConfigurazioneFontiDati> logger;
        private readonly IProductionJobScheduler productionScheduler;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione delle procedure di lavorazione.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        /// <param name="productionScheduler">Scheduler per la gestione dei job Hangfire.</param>
        public ServiceConfigurazioneFontiDati(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceConfigurazioneFontiDati> logger, IProductionJobScheduler productionScheduler)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
            this.productionScheduler = productionScheduler;
        }

        /// <inheritdoc/>
        public async Task<List<ConfigurazioneRiepilogoDto>> GetConfigurazioneFontiDatiDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();

            var configs = await context.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                    .ThenInclude(fc => fc.IdFaseLavorazioneNavigation)
                .Include(c => c.ConfigurazioneFaseCentros)
                    .ThenInclude(fc => fc.IdProceduraLavorazioneNavigation)
                .Include(c => c.ConfigurazioneFaseCentros)
                    .ThenInclude(fc => fc.IdCentroNavigation)
                .Include(c => c.TaskDaEseguires)
                .OrderBy(c => c.IdConfigurazione)
                .ToListAsync();

            var _configurazioni = configs.Select(c => new ConfigurazioneRiepilogoDto
            {
                IdConfigurazione = c.IdConfigurazione,
                CodiceConfigurazione = c.CodiceConfigurazione,
                Descrizione = c.DescrizioneConfigurazione!,
                TipoFonte = c.TipoFonte.ToString(),
                CreatoIl = (DateTime)c.CreatoIl,
                NumeroFasi = c.ConfigurazioneFaseCentros.Count(fc => fc.FlagAttiva == true),
                TaskAttivi = c.TaskDaEseguires.Count(t => t.Enabled),

                // Nuovi campi dettaglio
                FasiDettaglio = c.ConfigurazioneFaseCentros
                    .Where(fc => fc.FlagAttiva == true)
                    .Select(fc => fc.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "N/A")
                    .ToList(),

                CronExpressions = c.ConfigurazioneFaseCentros
                    .Where(fc => fc.FlagAttiva == true)
                    .Select(fc => fc.CronExpression ?? "0 5 * * *")
                    .ToList(),

                MappingDettaglio = c.ConfigurazioneFaseCentros
                    .Where(fc => fc.FlagAttiva == true)
                    .Select(fc => new MappingDettaglioDto
                    {
                        NomeProcedura = fc.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "N/A",
                        NomeFase = fc.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "N/A",
                        NomeCentro = fc.IdCentroNavigation?.Centro ?? "N/A",
                        Cron = fc.CronExpression ?? "0 5 * * *",
                        ParametriExtra = null
                    })
                    .ToList()
            }).ToList();

            return _configurazioni;
        }



        /// <summary>
        /// Aggiorna FlagDataReading = true nella tabella LavorazioniFasiDataReading per le fasi con cron validi.
        /// </summary>
        /// <param name="context">Contesto dati EF.</param>
        /// <param name="mappings">Lista mapping da aggiornare.</param>
        private async Task UpdateFlagDataReadingForMappingsAsync(DematReportsContext context, List<ConfigurazioneFaseCentro> mappings)
        {
            foreach (var mapping in mappings.Where(m => m.FlagAttiva))
            {
                // Usa il CRON dal campo dedicato
                var cron = mapping.CronExpression;
                
                // Se il cron è valido (non è solo il default), aggiorna FlagDataReading
                if (!string.IsNullOrWhiteSpace(cron) && cron != "0 5 * * *")
                {
                    // Cerca la fase corrispondente in LavorazioniFasiDataReading
                    var faseDataReading = await context.LavorazioniFasiDataReadings
                        .FirstOrDefaultAsync(l => 
                            l.IdProceduraLavorazione == mapping.IdProceduraLavorazione && 
                            l.IdFaseLavorazione == mapping.IdFaseLavorazione);

                    if (faseDataReading != null)
                    {
                        // Aggiorna FlagDataReading = true
                        faseDataReading.FlagDataReading = true;
                        logger.LogInformation(
                            "Impostato FlagDataReading = true per Procedura {IdProc} Fase {IdFase}",
                            mapping.IdProceduraLavorazione, 
                            mapping.IdFaseLavorazione);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Fase non trovata in LavorazioniFasiDataReading: Procedura {IdProc} Fase {IdFase}",
                            mapping.IdProceduraLavorazione, 
                            mapping.IdFaseLavorazione);
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteConfigurazioneFontiDatiAsync(int idConf)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .Include(c => c.TaskDaEseguires)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == idConf);
            if (entity != null)
            {
                // FASE 1: Rimuovi i job da Hangfire PRIMA di eliminarli dal database
                if (entity.TaskDaEseguires != null && entity.TaskDaEseguires.Count > 0)
                {
                    foreach (var task in entity.TaskDaEseguires)
                    {
                        if (!string.IsNullOrWhiteSpace(task.IdTaskHangFire))
                        {
                            try
                            {
                                await productionScheduler.RemoveByKeyAsync(task.IdTaskHangFire);
                                logger.LogInformation("Rimosso job Hangfire: {HangfireKey} per task {TaskId}", 
                                    task.IdTaskHangFire, task.IdTaskDaEseguire);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Errore rimozione job Hangfire {HangfireKey}", task.IdTaskHangFire);
                            }
                        }
                    }
                }

                // FASE 2: Rimuovi le entità dal database
                if (entity.ConfigurazioneFaseCentros != null && entity.ConfigurazioneFaseCentros.Count > 0)
                    context.ConfigurazioneFaseCentros.RemoveRange(entity.ConfigurazioneFaseCentros);

                if (entity.TaskDaEseguires != null && entity.TaskDaEseguires.Count > 0)
                    context.TaskDaEseguires.RemoveRange(entity.TaskDaEseguires);

                context.ConfigurazioneFontiDatis.Remove(entity);
                await context.SaveChangesAsync();

                logger.LogInformation("Configurazione {IdConfigurazione} eliminata definitivamente con {NumTask} task e {NumMapping} mapping", 
                    idConf, entity.TaskDaEseguires?.Count ?? 0, entity.ConfigurazioneFaseCentros?.Count ?? 0);
            }

        }

        /// <inheritdoc/>
        public async Task UpdateConfigurazioneFontiDatiAsync(ConfigurazioneFontiDati config, List<ConfigurazioneFaseCentro> mappings, string user)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            await using var tx = await context.Database.BeginTransactionAsync();
            try
            {
                var dbConfig = await context.ConfigurazioneFontiDatis
                    .Include(c => c.ConfigurazioneFaseCentros)
                    .FirstOrDefaultAsync(c => c.IdConfigurazione == config.IdConfigurazione);

                if (dbConfig == null) throw new InvalidOperationException($"Config {config.IdConfigurazione} non trovata");

                context.Entry(dbConfig).CurrentValues.SetValues(config);
                var incoming = mappings ?? new();
                var existing = dbConfig.ConfigurazioneFaseCentros ?? new List<ConfigurazioneFaseCentro>();

                await EnsureUniqueLavorazioneAsync(context, incoming, config.IdConfigurazione);

                var incomingIds = incoming.Where(m => m.IdFaseCentro > 0).Select(m => m.IdFaseCentro).ToHashSet();
                var mappingsToRemove = existing.Where(e => !incomingIds.Contains(e.IdFaseCentro)).ToList();
                
                // FASE 1: Trova e rimuovi i task associati ai mapping che verranno eliminati
                if (mappingsToRemove.Any())
                {
                    foreach (var mappingToRemove in mappingsToRemove)
                    {
                        // STEP 1: Trova prima il LavorazioniFasiDataReading corrispondente al mapping
                        var lavorazioneFase = await context.LavorazioniFasiDataReadings
                            .FirstOrDefaultAsync(lf => 
                                lf.IdProceduraLavorazione == mappingToRemove.IdProceduraLavorazione &&
                                lf.IdFaseLavorazione == mappingToRemove.IdFaseLavorazione);
                        
                        if (lavorazioneFase == null)
                        {
                            logger.LogWarning("LavorazioneFase non trovata per mapping Proc={IdProc} Fase={IdFase}", 
                                mappingToRemove.IdProceduraLavorazione, mappingToRemove.IdFaseLavorazione);
                            continue;
                        }
                        
                        // STEP 2: Trova tutti i task associati a questo LavorazioneFase E a questa configurazione
                        var tasksToRemove = await context.TaskDaEseguires
                            .Where(t => t.IdConfigurazioneDatabase == config.IdConfigurazione &&
                                       t.IdLavorazioneFaseDateReading == lavorazioneFase.IdlavorazioneFaseDateReading)
                            .ToListAsync();
                        
                        if (!tasksToRemove.Any())
                        {
                            logger.LogDebug("Nessun task trovato per mapping Proc={IdProc} Fase={IdFase}", 
                                mappingToRemove.IdProceduraLavorazione, mappingToRemove.IdFaseLavorazione);
                            continue;
                        }
                        
                        // STEP 3: Rimuovi prima i job da Hangfire
                        foreach (var task in tasksToRemove)
                        {
                            if (!string.IsNullOrWhiteSpace(task.IdTaskHangFire))
                            {
                                try
                                {
                                    await productionScheduler.RemoveByKeyAsync(task.IdTaskHangFire);
                                    logger.LogInformation("Rimosso job Hangfire {HangfireKey} per task {TaskId} (Proc={IdProc}, Fase={IdFase})",
                                        task.IdTaskHangFire, task.IdTaskDaEseguire, 
                                        mappingToRemove.IdProceduraLavorazione, mappingToRemove.IdFaseLavorazione);
                                }
                                catch (Exception ex)
                                {
                                    logger.LogWarning(ex, "Errore rimozione job Hangfire {HangfireKey}", task.IdTaskHangFire);
                                }
                            }
                        }
                        // STEP 4: Rimuovi i task dal database
                        context.TaskDaEseguires.RemoveRange(tasksToRemove);
                        logger.LogInformation("Rimossi {Count} task dal DB per mapping Proc={IdProc} Fase={IdFase}",
                            tasksToRemove.Count, mappingToRemove.IdProceduraLavorazione, mappingToRemove.IdFaseLavorazione);
                    }
                }
                
                // FASE 2: Rimuovi i mapping
                context.ConfigurazioneFaseCentros.RemoveRange(mappingsToRemove);
                foreach (var m in incoming)
                {
                    if (m.IdFaseCentro > 0)
                    {
                        var exist = existing.FirstOrDefault(e => e.IdFaseCentro == m.IdFaseCentro);
                        if (exist != null)
                        {
                            exist.IdFaseLavorazione = m.IdFaseLavorazione;
                            exist.IdCentro = m.IdCentro;
                            exist.TestoQueryTask = m.TestoQueryTask;
                            exist.CronExpression = m.CronExpression;
                            exist.TipoTask = m.TipoTask;
                            exist.HandlerClassName = m.HandlerClassName;
                            exist.MappingColonne = m.MappingColonne;
                            exist.FlagAttiva = m.FlagAttiva;
                            exist.GiorniPrecedenti = m.GiorniPrecedenti; // Aggiorna GiorniPrecedenti
                        }
                        else
                        {
                            m.IdFaseCentro = 0;
                            m.IdConfigurazione = config.IdConfigurazione;
                            m.FlagAttiva = true;
                            m.TipoTask ??= config.TipoFonte; // TipoFonte è già una stringa nel DB
                            m.CronExpression ??= "0 5 * * *";
                            if (m.GiorniPrecedenti is null or <= 0)
                                m.GiorniPrecedenti = 10;
                            context.ConfigurazioneFaseCentros.Add(m);
                        }
                    }
                    else
                    {
                        var duplicate = await context.ConfigurazioneFaseCentros.AnyAsync(x => x.IdConfigurazione == config.IdConfigurazione && x.IdFaseLavorazione == m.IdFaseLavorazione && x.IdCentro == m.IdCentro);
                        if (duplicate)
                        {
                            var existingDup = await context.ConfigurazioneFaseCentros.FirstOrDefaultAsync(x => x.IdConfigurazione == config.IdConfigurazione && x.IdFaseLavorazione == m.IdFaseLavorazione && x.IdCentro == m.IdCentro);
                            if (existingDup != null)
                            {
                                existingDup.TestoQueryTask = m.TestoQueryTask;
                                existingDup.CronExpression = m.CronExpression;
                                existingDup.TipoTask = m.TipoTask;
                                existingDup.HandlerClassName = m.HandlerClassName;
                                existingDup.MappingColonne = m.MappingColonne;
                                existingDup.FlagAttiva = true;
                                existingDup.GiorniPrecedenti = m.GiorniPrecedenti; // Aggiorna GiorniPrecedenti
                            }
                        }
                        else
                        {
                            m.IdFaseCentro = 0;
                            m.IdConfigurazione = config.IdConfigurazione;
                            m.FlagAttiva = true;

                            // Assicura che i campi obbligatori siano valorizzati
                            m.TipoTask ??= config.TipoFonte; // TipoFonte è già una stringa nel DB
                            m.CronExpression ??= "0 5 * * *";
                            if (m.GiorniPrecedenti is null or <= 0)
                                m.GiorniPrecedenti = 10;
                            context.ConfigurazioneFaseCentros.Add(m);
                        }
                    }
                }
                dbConfig.ModificatoIl = DateTime.Now;
                dbConfig.ModificatoDa = user ?? string.Empty;
                await context.SaveChangesAsync();

                // **NUOVA LOGICA: Aggiorna FlagDataReading = true per le fasi con cron validi**
                await UpdateFlagDataReadingForMappingsAsync(context, incoming);

                await tx.CommitAsync();
                
                // ✅ CLEANUP FINALE: Rimuove job Hangfire orfani dopo il commit
                // Questo è un ulteriore livello di sicurezza che confronta Hangfire con il DB
                try
                {
                    var cleanupCount = await productionScheduler.CleanupOrphansAsync();
                    if (cleanupCount > 0)
                    {
                        logger.LogInformation("Cleanup post-update: {Count} job Hangfire processati", cleanupCount);
                    }
                }
                catch (Exception cleanupEx)
                {
                    logger.LogWarning(cleanupEx, "Errore durante cleanup Hangfire post-update");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore aggiornamento configurazione fonti dati");
                try { await tx.RollbackAsync(); } catch { }
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task AddConfigurazioneFontiDatiAsync(ConfigurazioneFontiDati configurazioneFontiDati, List<ConfigurazioneFaseCentro> mappingFasi, string user)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            if (configurazioneFontiDati == null)
                throw new ArgumentNullException(nameof(configurazioneFontiDati));

            await using var context = await contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // Verifica se CodiceConfigurazione esiste già
                var codiceEsistente = await context.ConfigurazioneFontiDatis
                    .AnyAsync(c => c.CodiceConfigurazione == configurazioneFontiDati.CodiceConfigurazione);

                if (codiceEsistente)
                {
                    // Genera codice univoco con timestamp
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    var baseCodice = configurazioneFontiDati.CodiceConfigurazione.Split('_')[0];
                    configurazioneFontiDati.CodiceConfigurazione = $"{baseCodice}_{timestamp}";
                    
                    logger.LogWarning("CodiceConfigurazione duplicato rilevato, generato nuovo codice: {NewCode}", 
                        configurazioneFontiDati.CodiceConfigurazione);
                }

                if (mappingFasi != null && mappingFasi.Any())
                {
                    await EnsureUniqueLavorazioneAsync(context, mappingFasi);
                }

                configurazioneFontiDati.CreatoIl = DateTime.Now;
                configurazioneFontiDati.CreatoDa = user ?? string.Empty;

                // Insert configuration first to obtain IdConfigurazione
                context.ConfigurazioneFontiDatis.Add(configurazioneFontiDati);
                await context.SaveChangesAsync();

                if (mappingFasi != null && mappingFasi.Any())
                {
                    foreach (var mapping in mappingFasi)
                    {
                        // Ensure EF treats mapping as new insert
                        mapping.IdFaseCentro = 0;
                        mapping.IdConfigurazione = configurazioneFontiDati.IdConfigurazione;
                        mapping.FlagAttiva = true;
                        
                        // Assicura che GiorniPrecedenti sia valorizzato (default 10 se 0)
                        if (mapping.GiorniPrecedenti is null or <= 0)
                            mapping.GiorniPrecedenti = 10;
                        
                        context.ConfigurazioneFaseCentros.Add(mapping);
                    }

                    await context.SaveChangesAsync();

                    // **NUOVA LOGICA: Aggiorna FlagDataReading = true per le fasi con cron validi**
                    await UpdateFlagDataReadingForMappingsAsync(context, mappingFasi);
                }

                await transaction.CommitAsync();
                
                logger.LogInformation("Configurazione {IdConf} creata con codice {Codice} e {NumMapping} mapping", 
                    configurazioneFontiDati.IdConfigurazione, configurazioneFontiDati.CodiceConfigurazione, mappingFasi?.Count ?? 0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore durante creazione ConfigurazioneFontiDati");
                try { await transaction.RollbackAsync(); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Verifica che non ci siano duplicati di lavorazione tra le configurazioni.
        /// </summary>
        /// <param name="context">Contesto dati EF.</param>
        /// <param name="mappingFasi">Lista mapping da verificare.</param>
        /// <param name="currentConfigId">ID configurazione corrente (opzionale).</param>
        private static async Task EnsureUniqueLavorazioneAsync(DematReportsContext context, IEnumerable<ConfigurazioneFaseCentro> mappingFasi, int? currentConfigId = null)
        {
            var procedures = mappingFasi
                .Where(m => m.IdProceduraLavorazione > 0)
                .Select(m => m.IdProceduraLavorazione)
                .Distinct()
                .ToList();

            if (!procedures.Any())
                return;

            var query = context.ConfigurazioneFaseCentros
                .Where(fc => procedures.Contains(fc.IdProceduraLavorazione));

            if (currentConfigId.HasValue)
                query = query.Where(fc => fc.IdConfigurazione != currentConfigId.Value);

            var conflict = await query
                .Include(fc => fc.IdConfigurazioneNavigation)
                .FirstOrDefaultAsync();

            if (conflict != null)
            {
                var configCode = conflict.IdConfigurazioneNavigation?.CodiceConfigurazione ?? conflict.IdConfigurazione.ToString();
                throw new InvalidOperationException($"La procedura {conflict.IdProceduraLavorazione} è già utilizzata dalla configurazione {configCode}.");
            }
        }
    }
}
