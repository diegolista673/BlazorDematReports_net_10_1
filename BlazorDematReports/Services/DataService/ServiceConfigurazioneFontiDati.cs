using AutoMapper;
using BlazorDematReports.Application;
using BlazorDematReports.Dto;
using BlazorDematReports.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using MudBlazor;


namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle procedure di lavorazione e delle relative operazioni sui dati.
    public class ServiceConfigurazioneFontiDati : ServiceBase<ConfigurazioneFontiDati>, IServiceConfigurazioneFontiDati
    {
        
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceConfigurazioneFontiDati> logger;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione delle procedure di lavorazione.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceConfigurazioneFontiDati(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceConfigurazioneFontiDati> logger)
            : base(contextFactory)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.logger = logger;
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
                TipoFonte = c.TipoFonte,
                FlagAttiva = c.FlagAttiva,
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
                    .Select(fc => ExtractCronFromJson(fc.ParametriExtra))
                    .ToList(),

                MappingDettaglio = c.ConfigurazioneFaseCentros
                    .Where(fc => fc.FlagAttiva == true)
                    .Select(fc => new MappingDettaglioDto
                    {
                        NomeProcedura = fc.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "N/A",
                        NomeFase = fc.IdFaseLavorazioneNavigation?.FaseLavorazione ?? "N/A",
                        NomeCentro = fc.IdCentroNavigation?.Centro ?? "N/A",
                        Cron = ExtractCronFromJson(fc.ParametriExtra),
                        ParametriExtra = fc.ParametriExtra
                    })
                    .ToList()
            }).ToList();

            return _configurazioni;
        }

        /// <summary>
        /// Estrae il cron dal JSON ParametriExtra, se presente.
        /// </summary>
        private static string ExtractCronFromJson(string? parametriExtra)
        {
            if (string.IsNullOrWhiteSpace(parametriExtra))
                return "0 5 * * *"; // Default
    
            try
            {
                var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametriExtra);
                if (json != null && json.TryGetValue("cron", out var cronValue))
                {
                    return cronValue?.ToString() ?? "0 5 * * *";
                }
            }
            catch
            {
                // JSON malformato
            }
    
            return "0 5 * * *";
        }

        /// <summary>
        /// Aggiorna FlagDataReading = true nella tabella LavorazioniFasiDataReading per le fasi con cron validi.
        /// </summary>
        private async Task UpdateFlagDataReadingForMappingsAsync(DematReportsContext context, List<ConfigurazioneFaseCentro> mappings)
        {
            foreach (var mapping in mappings.Where(m => m.FlagAttiva))
            {
                // Estrai il cron dal mapping
                var cron = ExtractCronFromJson(mapping.ParametriExtra);
                
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
                // Rimuovi mapping e task associati solo se presenti
                if (entity.ConfigurazioneFaseCentros != null && entity.ConfigurazioneFaseCentros.Count > 0)
                    context.ConfigurazioneFaseCentros.RemoveRange(entity.ConfigurazioneFaseCentros);

                if (entity.TaskDaEseguires != null && entity.TaskDaEseguires.Count > 0)
                    context.TaskDaEseguires.RemoveRange(entity.TaskDaEseguires);

                context.ConfigurazioneFontiDatis.Remove(entity);
                await context.SaveChangesAsync();

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

                var incomingIds = incoming.Where(m => m.IdFaseCentro > 0).Select(m => m.IdFaseCentro).ToHashSet();
                context.ConfigurazioneFaseCentros.RemoveRange(existing.Where(e => !incomingIds.Contains(e.IdFaseCentro)));
                foreach (var m in incoming)
                {
                    if (m.IdFaseCentro > 0)
                    {
                        var exist = existing.FirstOrDefault(e => e.IdFaseCentro == m.IdFaseCentro);
                        if (exist != null)
                        {
                            exist.IdFaseLavorazione = m.IdFaseLavorazione;
                            exist.IdCentro = m.IdCentro;
                            exist.TestoQueryOverride = m.TestoQueryOverride;
                            exist.ParametriExtra = m.ParametriExtra;
                            exist.MappingColonne = m.MappingColonne;
                            exist.FlagAttiva = m.FlagAttiva;
                        }
                        else
                        {
                            m.IdFaseCentro = 0;
                            m.IdConfigurazione = config.IdConfigurazione;
                            m.FlagAttiva = true;
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
                                existingDup.TestoQueryOverride = m.TestoQueryOverride;
                                existingDup.ParametriExtra = m.ParametriExtra;
                                existingDup.MappingColonne = m.MappingColonne;
                                existingDup.FlagAttiva = true;
                            }
                        }
                        else
                        {
                            m.IdFaseCentro = 0;
                            m.IdConfigurazione = config.IdConfigurazione;
                            m.FlagAttiva = true;
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

            configurazioneFontiDati.CreatoIl = DateTime.Now;
            configurazioneFontiDati.CreatoDa = user ?? string.Empty;
            configurazioneFontiDati.FlagAttiva = true;

            await using var context = await contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

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
                        mapping.ParametriExtra ??= string.Empty;
                        context.ConfigurazioneFaseCentros.Add(mapping);
                }

                await context.SaveChangesAsync();

                // **NUOVA LOGICA: Aggiorna FlagDataReading = true per le fasi con cron validi**
                await UpdateFlagDataReadingForMappingsAsync(context, mappingFasi);
            }

            await transaction.CommitAsync();

        }
    }
}
