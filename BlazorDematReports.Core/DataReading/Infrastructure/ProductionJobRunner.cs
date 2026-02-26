using BlazorDematReports.Core.Constants;
using BlazorDematReports.Core.DataReading.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Enums;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.DataReading.Infrastructure
{
    /// <summary>
    /// Entry point statico per l'esecuzione dei job di produzione Hangfire.
    /// Gestisce il ciclo di vita completo: caricamento task, esecuzione pipeline
    /// Acquire → Elaborate → Persist, aggiornamento stato e audit log.
    /// Ogni job viene eseguito in un proprio scope DI per garantire isolamento delle risorse.
    /// </summary>
    public static class ProductionJobRunner
    {
        private static IServiceScopeFactory? _scopeFactory;

        /// <summary>
        /// Inizializza il runner con la factory DI.
        /// Deve essere chiamato una sola volta all'avvio dell'applicazione (in <c>InitializeApp</c>).
        /// </summary>
        /// <param name="factory">Factory per la creazione di scope DI per ogni job Hangfire.</param>
        /// <exception cref="ArgumentNullException">Se <paramref name="factory"/> è null.</exception>
        /// <exception cref="InvalidOperationException">Se già inizializzato.</exception>
        public static void Initialize(IServiceScopeFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory);

            if (_scopeFactory is not null)
                throw new InvalidOperationException("ProductionJobRunner e gia stato inizializzato.");

            _scopeFactory = factory;
        }

        /// <summary>Entry point Hangfire: esegue il task con date calcolate da GiorniPrecedenti.</summary>
        public static async Task RunAsync(int idTaskDaEseguire, CancellationToken cancellationToken = default)
            => await RunAsync(idTaskDaEseguire, startDate: null, endDate: null, cancellationToken);

        /// <summary>
        /// Entry point per esecuzione manuale con range date custom.
        /// Se <paramref name="startDate"/>/<paramref name="endDate"/> sono null, usa GiorniPrecedenti del task.
        /// </summary>
        public static async Task RunAsync(
            int idTaskDaEseguire,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken cancellationToken = default)
        {
            if (_scopeFactory is null)
                throw new InvalidOperationException(
                    "ProductionJobRunner non e stato inizializzato. Chiamare Initialize() all'avvio.");

            using var scope = _scopeFactory.CreateScope();
            var db     = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                             .CreateLogger("ProductionJobRunner");

            var entity = await db.TaskDaEseguires
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdProceduraLavorazioneNavigation)
                .Include(x => x.IdLavorazioneFaseDateReadingNavigation)!.ThenInclude(f => f.IdFaseLavorazioneNavigation)
                .Include(x => x.IdConfigurazioneDatabaseNavigation)
                .FirstOrDefaultAsync(x => x.IdTaskDaEseguire == idTaskDaEseguire, cancellationToken);

            if (entity is null)
            {
                logger.LogWarning("Task {TaskId} non trovato", idTaskDaEseguire);
                return;
            }

            if (!entity.Enabled)
            {
                logger.LogInformation("Task {TaskId} disabilitato, esecuzione saltata", idTaskDaEseguire);
                return;
            }

            try
            {
                await ExecuteProductionTaskAsync(scope, entity, startDate, endDate, cancellationToken);
                MarkSuccess(entity);
                logger.LogInformation("Task {TaskId} completato", idTaskDaEseguire);
            }
            catch (Exception ex)
            {
                MarkFailure(entity, ex);
                logger.LogError(ex, "Errore task {TaskId}", idTaskDaEseguire);
                throw;
            }
            finally
            {
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        #region Execution Pipeline

        /// <summary>
        /// Pipeline unificata: Acquire → Elaborate → Persist.
        /// Comune per TipoFonte.SQL e TipoFonte.HandlerIntegrato.
        /// </summary>
        private static async Task ExecuteProductionTaskAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var db          = scope.ServiceProvider.GetRequiredService<DematReportsContext>();
            var elaboratore = scope.ServiceProvider.GetRequiredService<IElaboratoreDatiLavorazione>();
            var logger      = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                  .CreateLogger("ProductionJobRunner");
            var fase = entity.IdLavorazioneFaseDateReadingNavigation;

            var config = await db.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                    .ThenInclude(fc => fc.IdCentroNavigation)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == entity.IdConfigurazioneDatabase, ct)
                ?? throw new InvalidOperationException(
                    $"Configurazione {entity.IdConfigurazioneDatabase} non trovata per task {entity.IdTaskDaEseguire}");

            // Il centro e ricavato dalla procedura: TaskDaEseguire → LavorazioniFasiDataReading
            //   → ProcedureLavorazioni.Idcentro. La navigation e gia caricata in RunAsync.
            var idCentroDelTask = fase.IdProceduraLavorazioneNavigation?.Idcentro;

            var mapping = config.ConfigurazioneFaseCentros
                .FirstOrDefault(fc =>
                    fc.IdFaseLavorazione      == fase.IdFaseLavorazione      &&
                    fc.IdProceduraLavorazione == fase.IdProceduraLavorazione &&
                    (idCentroDelTask == null   || fc.IdCentro == idCentroDelTask) &&
                    fc.FlagAttiva             == true)
                ?? throw new InvalidOperationException(
                    $"Nessun mapping attivo per fase {fase.IdFaseLavorazione} / procedura {fase.IdProceduraLavorazione} " +
                    $"/ centro {idCentroDelTask} nella configurazione {config.IdConfigurazione}. " +
                    "Verificare i mapping Fase/Centro in /admin/fonti-dati.");

            var datiLavorazione = await AcquireDatiLavorazioneAsync(
                scope, entity, config, mapping, startDate, endDate, ct);

            if (datiLavorazione.Count == 0)
            {
                logger.LogInformation("Task {TaskId}: nessun dato acquisito per il periodo configurato",
                    entity.IdTaskDaEseguire);
                return;
            }

            var datiElaborati = await elaboratore.ElaboraDatiLavorazioneAsync(
                datiLavorazione,
                mapping.IdCentro,
                fase.IdProceduraLavorazione,
                fase.IdFaseLavorazione,
                ct);

            int saved = await PersistProduzioneSistemaAsync(db, datiElaborati, ct);

            await LogTaskExecutionAsync(db, entity, fase, datiElaborati, startDate, endDate, saved, ct);

            logger.LogInformation(
                "Task {TaskId}: {Saved} record salvati in ProduzioneSistema su {Total} elaborati",
                entity.IdTaskDaEseguire, saved, datiElaborati.Count);
        }

        /// <summary>
        /// Strategy selector: delega l'acquisizione dati al path corretto in base a TipoFonte.
        /// </summary>
        private static Task<List<DatiLavorazione>> AcquireDatiLavorazioneAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            return config.TipoFonte switch
            {
                TipoFonteData.SQL              => AcquireFromSqlAsync(scope, entity, config, mapping, startDate, endDate, ct),
                TipoFonteData.HandlerIntegrato => AcquireFromHandlerAsync(scope, entity, config, mapping, startDate, endDate, ct),
                _ => throw new InvalidOperationException(
                    $"TipoFonte '{config.TipoFonte}' non supportato per task {entity.IdTaskDaEseguire}")
            };
        }

        /// <summary>
        /// Strategy SQL: esegue la query configurata nel mapping, converte i risultati in DatiLavorazione.
        /// AppartieneAlCentro e determinato dal QueryService con euristica a 3 livelli.
        /// </summary>
        private static async Task<List<DatiLavorazione>> AcquireFromSqlAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var queryService      = scope.ServiceProvider.GetRequiredService<IQueryService>();
            var lavorazioniConfig = scope.ServiceProvider.GetRequiredService<ILavorazioniConfigManager>();
            var logger            = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                        .CreateLogger("ProductionJobRunner");

            var query = mapping.TestoQueryTask;
            if (string.IsNullOrWhiteSpace(query))
                throw new InvalidOperationException(
                    $"Nessuna query configurata per task {entity.IdTaskDaEseguire}. " +
                    "Configurare TestoQueryTask nel mapping Fase/Centro.");

            var connectionString = lavorazioniConfig.GetConnectionString(config.ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    $"Connection string '{config.ConnectionStringName}' non trovata in appsettings.");

            var effectiveStart = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEnd   = endDate   ?? DateTime.Now;

            // Nome del centro (es. 'GENOVA') — usato dal QueryService per l'euristica WHERE
            var nomeCentroAtteso = mapping.IdCentroNavigation?.Centro;

            var results = await queryService.ExecuteProductionQueryAsync(
                connectionString, query, effectiveStart, effectiveEnd,
                mapping.IdCentro, nomeCentroAtteso, ct);

            if (results.Count == 0)
            {
                logger.LogInformation(
                    "Task {TaskId}: nessun dato dalla query SQL per periodo {Start:d}-{End:d}",
                    entity.IdTaskDaEseguire, effectiveStart, effectiveEnd);
                return [];
            }

            return results.Select(r => new DatiLavorazione
            {
                Operatore                     = r.Operatore,
                DataLavorazione               = r.DataLavorazione,
                Documenti                     = r.Documenti,
                Fogli                         = r.Fogli,
                Pagine                        = r.Pagine,
                AppartieneAlCentroSelezionato = r.AppartieneAlCentro
            }).ToList();
        }

        /// <summary>
        /// Strategy Handler: esegue l'handler integrato custom tramite UnifiedHandlerService.
        /// </summary>
        private static async Task<List<DatiLavorazione>> AcquireFromHandlerAsync(
            IServiceScope scope,
            TaskDaEseguire entity,
            ConfigurazioneFontiDati config,
            ConfigurazioneFaseCentro mapping,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken ct)
        {
            var unifiedService = scope.ServiceProvider.GetRequiredService<IUnifiedHandlerService>();
            var logger         = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                     .CreateLogger("ProductionJobRunner");
            var fase = entity.IdLavorazioneFaseDateReadingNavigation;

            var handlerCode = config.HandlerClassName;
            if (string.IsNullOrWhiteSpace(handlerCode))
                throw new InvalidOperationException(
                    $"HandlerClassName mancante nella configurazione {config.IdConfigurazione}");

            var effectiveStart = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEnd   = endDate   ?? DateTime.Now;

            var context = new UnifiedExecutionContext
            {
                IDProceduraLavorazione = fase.IdProceduraLavorazione,
                HandlerCode            = handlerCode,
                Parameters = new Dictionary<string, object>
                {
                    { "IDFaseLavorazione",        fase.IdFaseLavorazione },
                    { "IDCentro",                 mapping.IdCentro },
                    { "NomeProcedura",            fase.IdProceduraLavorazioneNavigation?.NomeProceduraProgramma ?? "UNKNOWN" },
                    { "StartDataLavorazione",     effectiveStart },
                    { "EndDataLavorazione",       effectiveEnd },
                    { "TaskId",                   entity.IdTaskDaEseguire },
                    { "IdConfigurazioneDatabase", entity.IdConfigurazioneDatabase ?? 0 }
                }
            };

            var result = await unifiedService.ExecuteHandlerAsync(handlerCode, context, ct);

            if (result is not List<DatiLavorazione> datiLavorazione)
            {
                logger.LogWarning("Handler {Code} ha restituito tipo inatteso: {Type}",
                    handlerCode, result?.GetType().Name ?? "null");
                return [];
            }

            if (datiLavorazione.Count == 0)
                logger.LogInformation("Handler {Code}: nessun dato restituito", handlerCode);

            return datiLavorazione;
        }

        #endregion

        #region Persistence & Audit

        /// <summary>
        /// Persiste i dati elaborati in ProduzioneSistema con strategia delete-then-reinsert.
        /// Elimina i record auto-inseriti nel range date, reinserisce quelli nuovi.
        /// I record con FlagInserimentoManuale=true non vengono mai toccati.
        /// Salta i record con IdOperatore=0 (FK non valida).
        /// </summary>
        /// <returns>Numero di record inseriti.</returns>
        private static async Task<int> PersistProduzioneSistemaAsync(
            DematReportsContext db,
            List<DatiElaborati> datiElaborati,
            CancellationToken ct)
        {
            if (datiElaborati.Count == 0) return 0;

            var dates  = datiElaborati.Select(d => d.DataLavorazione.Date).Distinct().ToList();
            var idProc = datiElaborati[0].IdProceduraLavorazione;
            var idFase = datiElaborati[0].IdFaseLavorazione;

            // 1. Rimuove i record auto-inseriti nel range (dati stale)
            var toDelete = await db.ProduzioneSistemas
                .Where(p => p.IdProceduraLavorazione == idProc &&
                            p.IdFaseLavorazione      == idFase &&
                            dates.Contains(p.DataLavorazione.Date) &&
                            p.FlagInserimentoAuto    == true &&
                            p.FlagInserimentoManuale != true)
                .ToListAsync(ct);

            if (toDelete.Count > 0)
                db.ProduzioneSistemas.RemoveRange(toDelete);

            // 2. Recupera chiavi manuali per evitare duplicati
            var manualKeys = await db.ProduzioneSistemas
                .Where(p => p.IdProceduraLavorazione == idProc &&
                            p.IdFaseLavorazione      == idFase &&
                            dates.Contains(p.DataLavorazione.Date) &&
                            p.FlagInserimentoManuale == true)
                .Select(p => new { p.IdOperatore, Data = p.DataLavorazione.Date })
                .ToListAsync(ct);

            var manualSet = manualKeys.Select(k => (k.IdOperatore, k.Data)).ToHashSet();

            // 3. Inserisce i nuovi record
            var now      = DateTime.Now;
            int inserted = 0;

            foreach (var dato in datiElaborati)
            {
                if (dato.IdOperatore == 0)
                    continue;

                if (manualSet.Contains((dato.IdOperatore, dato.DataLavorazione.Date)))
                    continue;

                db.ProduzioneSistemas.Add(new ProduzioneSistema
                {
                    IdOperatore              = dato.IdOperatore,
                    Operatore                = dato.Operatore,
                    OperatoreNonRiconosciuto = dato.OperatoreNonRiconosciuto,
                    IdProceduraLavorazione   = dato.IdProceduraLavorazione,
                    IdFaseLavorazione        = dato.IdFaseLavorazione,
                    IdCentro                 = dato.IdCentro,
                    DataLavorazione          = dato.DataLavorazione.Date,
                    DataAggiornamento        = now,
                    Documenti                = dato.Documenti,
                    Fogli                    = dato.Fogli,
                    Pagine                   = dato.Pagine,
                    FlagInserimentoAuto      = true,
                    FlagInserimentoManuale   = false
                });
                inserted++;
            }

            await db.SaveChangesAsync(ct);
            return inserted;
        }

        /// <summary>
        /// Registra l'esecuzione del task nella tabella di audit TaskDataReadingAggiornamento.
        /// </summary>
        private static async Task LogTaskExecutionAsync(
            DematReportsContext db,
            TaskDaEseguire entity,
            LavorazioniFasiDataReading fase,
            List<DatiElaborati> datiElaborati,
            DateTime? startDate,
            DateTime? endDate,
            int recordsSaved,
            CancellationToken ct)
        {
            var effectiveStart = startDate ?? DateTime.Now.AddDays(-(entity.GiorniPrecedenti ?? TaskConfigurationDefaults.DefaultGiorniPrecedenti));
            var effectiveEnd   = endDate   ?? DateTime.Now;

            db.TaskDataReadingAggiornamentos.Add(new TaskDataReadingAggiornamento
            {
                Lavorazione           = fase.IdProceduraLavorazioneNavigation?.NomeProcedura ?? "UNKNOWN",
                IdLavorazione         = fase.IdProceduraLavorazione,
                FaseLavorazione       = fase.IdFaseLavorazioneNavigation?.FaseLavorazione    ?? "UNKNOWN",
                IdFase                = fase.IdFaseLavorazione,
                DataInizioLavorazione = effectiveStart.Date,
                DataFineLavorazione   = effectiveEnd.Date,
                DataAggiornamento     = DateTime.Now,
                Risultati             = recordsSaved,
                EsitoLetturaDato      = true,
                DescrizioneEsito      = $"Task {entity.IdTaskDaEseguire}: {recordsSaved} record salvati su {datiElaborati.Count} elaborati"
            });

            await db.SaveChangesAsync(ct);
        }

        #endregion

        #region State Helpers

        /// <summary>Marca il task come completato con successo.</summary>
        private static void MarkSuccess(TaskDaEseguire entity)
        {
            entity.LastRunUtc = DateTime.UtcNow;
            entity.LastError  = null;
            entity.Stato      = "COMPLETED";
            entity.DataStato  = DateTime.Now;
        }

        /// <summary>Marca il task come fallito, salvando il messaggio di errore.</summary>
        private static void MarkFailure(TaskDaEseguire entity, Exception ex)
        {
            entity.LastRunUtc = DateTime.UtcNow;
            entity.LastError  = ex.Message;
            entity.Stato      = "ERROR";
            entity.DataStato  = DateTime.Now;
        }

        #endregion
    }
}
