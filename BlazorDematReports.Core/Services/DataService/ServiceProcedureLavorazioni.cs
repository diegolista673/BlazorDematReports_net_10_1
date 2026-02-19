using AutoMapper;
using AutoMapper.QueryableExtensions;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Interfaces.IDataService;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle procedure di lavorazione e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceProcedureLavorazioni : ServiceBase<ProcedureLavorazioni>, IServiceProcedureLavorazioni
    {


        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione delle procedure di lavorazione.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceProcedureLavorazioni(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceProcedureLavorazioni> logger)
            : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <summary>
        /// Elimina una procedura di lavorazione specificata dall'ID.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteProceduraLavorazioneAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var entity = await context.ProcedureLavorazionis.FindAsync(idProceduraLavorazione).ConfigureAwait(false);
            if (entity != null)
            {
                context.ProcedureLavorazionis.Remove(entity);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Aggiunge una nuova procedura di lavorazione al sistema e restituisce l'ID generato.
        /// </summary>
        /// <param name="procedureLavorazioniDto">DTO della procedura di lavorazione da aggiungere.</param>
        /// <returns>ID della nuova procedura di lavorazione inserita.</returns>
        public async Task<int> AddProceduraLavorazioneAsync(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = mapper.Map<ProcedureLavorazioniDto, ProcedureLavorazioni>(procedureLavorazioniDto);
            using var context = contextFactory.CreateDbContext();
            context.ProcedureLavorazionis.Add(entity);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return entity.IdproceduraLavorazione;
        }

        /// <summary>
        /// Restituisce una procedura di lavorazione specifica con fasi e query associate.
        /// </summary>
        /// <param name="IdproceduraLavorazione">ID della procedura di lavorazione da cercare.</param>
        /// <returns>La procedura di lavorazione specificata con tutte le relazioni incluse o null se non trovata.</returns>
        public async Task<ProcedureLavorazioni?> GetProceduraLavorazioneByIdAsync(int IdproceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.LavorazioniFasiDataReadings)
                .Include(x => x.QueryProcedureLavorazionis)
                .FirstOrDefaultAsync(c => c.IdproceduraLavorazione.Equals(IdproceduraLavorazione));
        }

        /// <summary>
        /// Restituisce tutte le procedure di lavorazione appartenenti al centro specificato.
        /// </summary>
        /// <param name="iDcentro">ID del centro di lavorazione per il filtro.</param>
        /// <returns>Lista delle procedure di lavorazione del centro specificato con fasi incluse.</returns>
        public async Task<List<ProcedureLavorazioni>> GetProceduraLavorazioneByIdCentroAsync(int iDcentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.LavorazioniFasiDataReadings)
                .AsNoTracking()
                .Where(c => c.Idcentro.Equals(iDcentro))
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce tutte le procedure di lavorazione con tutte le relazioni incluse per amministratori.
        /// </summary>
        /// <returns>Lista completa delle procedure di lavorazione con operatori, reparti, formati e relazioni client-centro.</returns>
        public async Task<List<ProcedureLavorazioni>> GetProcedureLavorazioniAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdrepartiNavigation)
                .Include(x => x.IdformatoDatiProduzioneNavigation)
                .Include(x => x.QueryProcedureLavorazionis)
                .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires)
                .Include(x => x.IdproceduraClienteNavigation).ThenInclude(p => p!.IdclienteNavigation).ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                .OrderBy(x => x.NomeProcedura)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce le procedure di lavorazione filtrate in base al ruolo dell'utente corrente.
        /// </summary>
        /// <returns>Lista delle procedure di lavorazione accessibili all'utente corrente con tutte le relazioni.</returns>
        public async Task<List<ProcedureLavorazioni>> GetProcedureLavorazioniByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            if (configUser.IsAdminRole)
            {
                return await GetProcedureLavorazioniAsync();
            }
            else
            {
                return await context.ProcedureLavorazionis
                    .Include(x => x.IdoperatoreNavigation)
                    .Include(x => x.IdrepartiNavigation)
                    .Include(x => x.IdformatoDatiProduzioneNavigation)
                    .Include(x => x.QueryProcedureLavorazionis)
                    .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires).ThenInclude(x => x.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazioneNavigation)
                    .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires)
                    .Include(x => x.IdproceduraClienteNavigation).ThenInclude(p => p!.IdclienteNavigation).ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                    .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                    .OrderBy(x => x.NomeProcedura)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Restituisce le procedure di lavorazione semplificate per visualizzazione in tabelle.
        /// </summary>
        /// <returns>Lista delle procedure di lavorazione filtrate per l'utente corrente senza relazioni complesse.</returns>
        public async Task<List<ProcedureLavorazioni>> GetTableProcedureLavorazioniByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();

            var query = context.ProcedureLavorazionis
                .AsNoTracking();


            if (!configUser.IsAdminRole)
            {
                query = query.Where(x => x.Idcentro == configUser.IdCentroOrigine);
            }

            return await query.OrderBy(x => x.NomeProcedura).ToListAsync();
        }

        /// <summary>
        /// Restituisce le procedure di lavorazione come DTO con proiezione ottimizzata per performance.
        /// </summary>
        /// <returns>Lista di DTO delle procedure di lavorazione filtrate per l'utente corrente.</returns>
        public async Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            if (configUser.IsAdminRole)
            {
                return await context.ProcedureLavorazionis
                    .OrderBy(x => x.NomeProcedura)
                    .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                    .ToListAsync();
            }
            else
            {
                return await context.ProcedureLavorazionis
                    .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                    .OrderBy(x => x.NomeProcedura)
                    .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Restituisce le procedure di lavorazione con fasi associate come DTO.
        /// </summary>
        /// <returns>Lista di DTO delle procedure con operatori e fasi di lavorazione incluse.</returns>
        public async Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniFasiDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.LavorazioniFasiDataReadings)
                .AsNoTracking()
                .OrderBy(x => x.NomeProcedura)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce le procedure di lavorazione di un centro come DTO con tutte le relazioni complesse.
        /// </summary>
        /// <param name="idCentro">ID del centro di lavorazione per il filtro.</param>
        /// <returns>Lista di DTO delle procedure del centro con operatori, reparti, formati, query, fasi e task inclusi.</returns>
        public async Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniDtoByIDCentroAsync(int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.LavorazioniFasiDataReadings)
                .AsNoTracking()
                .Where(x => x.Idcentro == idCentro)
                .OrderBy(x => x.NomeProcedura)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce una singola procedura di lavorazione completa con task e fasi come DTO.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione da cercare.</param>
        /// <returns>DTO della procedura di lavorazione con tutte le relazioni complesse incluse.</returns>
        public async Task<ProcedureLavorazioniDto?> GetSingleProceduraLavorazioneDtoByIDAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();

            var query = context.ProcedureLavorazionis
                            .Include(x => x.LavorazioniFasiDataReadings)
                                .ThenInclude(x => x.TaskDaEseguires)
                            .Include(x => x.LavorazioniFasiDataReadings)
                                .ThenInclude(x => x.IdFaseLavorazioneNavigation)
                            .Include(x => x.QueryProcedureLavorazionis)
                            .Include(x => x.IdoperatoreNavigation)
                            .Include(x => x.IdrepartiNavigation)
                            .Include(x => x.IdformatoDatiProduzioneNavigation)
                            .Include(x => x.IdproceduraClienteNavigation)
                                .ThenInclude(p => p!.IdclienteNavigation)
                                .ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                            .AsNoTracking()
                            .Where(x => x.IdproceduraLavorazione == idProceduraLavorazione);

            return await query
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Aggiorna una procedura di lavorazione esistente con gestione complessa di fasi e task in transazione.
        /// </summary>
        /// <param name="procedureLavorazioniDto">DTO della procedura con tutti i dati aggiornati incluse fasi e task.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento transazionale.</returns>
        public async Task UpdateProceduraLavorazioneAndFasiDataReadingASync(ProcedureLavorazioniDto procedureLavorazioniDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var lavorazioneOriginal = await context.ProcedureLavorazionis
                    .Where(c => c.IdproceduraLavorazione == procedureLavorazioniDto.IdproceduraLavorazione)
                    .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires)
                    .Include(x => x.QueryProcedureLavorazionis)
                    .FirstOrDefaultAsync();

                if (lavorazioneOriginal == null)
                {
                    throw new InvalidOperationException($"ProceduraLavorazione con ID {procedureLavorazioniDto.IdproceduraLavorazione} non trovata");
                }

                // VALIDAZIONE VINCOLI FOREIGN KEY PRIMA DELL'AGGIORNAMENTO
                await ValidateForeignKeysAsync(context, procedureLavorazioniDto);

                // GESTIONE DELLE FASI E TASK
                if (procedureLavorazioniDto.LavorazioniFasiDataReadingsDto != null && procedureLavorazioniDto.LavorazioniFasiDataReadingsDto.Any())
                {
                    // Caso 1: Ci sono fasi nel DTO da gestire
                    var fasiList = procedureLavorazioniDto.LavorazioniFasiDataReadingsDto.ToList();
                    await ValidateFasiAndTasksAsync(context, fasiList);
                    await ManageFasiDataReadingsAsync(context, lavorazioneOriginal, fasiList);
                }
                else
                {
                    // Caso 2: LavorazioniFasiDataReadingsDto � null o vuoto - rimuovi tutte le fasi esistenti
                    await ClearAllFasiDataReadingsAsync(context, lavorazioneOriginal);
                }

                // AGGIORNA LE PROPRIET� PRINCIPALI
                UpdateMainProperties(lavorazioneOriginal, procedureLavorazioniDto);

                // Salvataggio finale
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException($"Errore durante l'aggiornamento della procedura di lavorazione: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Valida tutti i vincoli Foreign Key
        /// </summary>
        private async Task ValidateForeignKeysAsync(DematReportsContext context, ProcedureLavorazioniDto dto)
        {
            // Valida IdproceduraCliente
            if (dto.IdproceduraCliente > 0)
            {
                var exists = await context.ProcedureClientes
                    .AnyAsync(pc => pc.IdproceduraCliente == dto.IdproceduraCliente);

                if (!exists)
                {
                    throw new InvalidOperationException($"ProceduraCliente con ID {dto.IdproceduraCliente} non esiste");
                }
            }

            // Valida Idoperatore se presente
            if (dto.Idoperatore.HasValue && dto.Idoperatore.Value > 0)
            {
                var exists = await context.Operatoris
                    .AnyAsync(o => o.Idoperatore == dto.Idoperatore.Value);

                if (!exists)
                {
                    throw new InvalidOperationException($"Operatore con ID {dto.Idoperatore} non esiste");
                }
            }

            // Valida IdformatoDatiProduzione
            if (dto.IdformatoDatiProduzione > 0)
            {
                var exists = await context.FormatoDatis
                    .AnyAsync(f => f.IdformatoDati == dto.IdformatoDatiProduzione);

                if (!exists)
                {
                    throw new InvalidOperationException($"FormatoDati con ID {dto.IdformatoDatiProduzione} non esiste");
                }
            }

            // Valida Idreparti
            if (dto.Idreparti > 0)
            {
                var exists = await context.RepartiProduziones
                    .AnyAsync(r => r.IdReparti == dto.Idreparti);

                if (!exists)
                {
                    throw new InvalidOperationException($"RepartiProduzione con ID {dto.Idreparti} non esiste");
                }
            }
        }


        /// <summary>
        /// Valida le fasi e i task
        /// </summary>
        private async Task ValidateFasiAndTasksAsync(DematReportsContext context, List<LavorazioniFasiDataReadingDto> fasiDto)
        {
            foreach (var faseDto in fasiDto)
            {
                // **CORREZIONE: Salta la validazione per la fase speciale procedura (ID = 0)**
                // La fase con ID 0 � una fase virtuale per task a livello procedura e non esiste nella tabella FasiLavorazione
                if (faseDto.IdFaseLavorazione != 0)
                {
                    // Valida fase solo se non � la fase procedura speciale
                    var faseExists = await context.FasiLavoraziones
                        .AnyAsync(f => f.IdFaseLavorazione == faseDto.IdFaseLavorazione);

                    if (!faseExists)
                    {
                        throw new InvalidOperationException($"FaseLavorazione con ID {faseDto.IdFaseLavorazione} non esiste");
                    }
                }
                else
                {
                    // Log per la fase procedura speciale
                    logger.LogDebug("Validazione saltata per fase procedura speciale (ID=0) - fase virtuale per servizi mail");
                }

                // **CORREZIONE: Validazione rigorosa dei task per prevenire violazioni FK**
                if (faseDto.TaskDaEseguireDto != null)
                {
                    foreach (var taskDto in faseDto.TaskDaEseguireDto.Where(t => t.IdTask > 0))
                    {
                        // **VALIDAZIONE: Controlla che IdConfigurazioneDatabase sia valido**
                        if (taskDto.IdConfigurazioneDatabase.HasValue)
                        {
                            var configExists = await context.ConfigurazioneFontiDatis
                                .AnyAsync(c => c.IdConfigurazione == taskDto.IdConfigurazioneDatabase.Value);

                            if (!configExists)
                            {
                                logger.LogError("Task con IdConfigurazioneDatabase non valido: TaskId={TaskId}, IdConfig={IdConfig}", 
                                    taskDto.IdTaskDaEseguire, taskDto.IdConfigurazioneDatabase.Value);
                                throw new InvalidOperationException(
                                    $"Configurazione con ID {taskDto.IdConfigurazioneDatabase.Value} non esiste. " +
                                    "Usa /admin/fonti-dati per creare configurazioni valide.");
                            }
                        }
                    }
                }
            }
        }





        /// <summary>
        /// Gestisce l'aggiornamento delle fasi
        /// </summary>
        private async Task ManageFasiDataReadingsAsync(DematReportsContext context,
            ProcedureLavorazioni lavorazione,
            List<LavorazioniFasiDataReadingDto> fasiDto)
        {
            var existingFasi = lavorazione.LavorazioniFasiDataReadings.ToList();
            var dtoFasiIds = fasiDto
                .Where(dto => dto.IdlavorazioneFaseDateReading > 0)
                .Select(dto => dto.IdlavorazioneFaseDateReading)
                .ToList();

            // Rimuovi fasi non pi� presenti
            foreach (var existingFase in existingFasi)
            {
                if (!dtoFasiIds.Contains(existingFase.IdlavorazioneFaseDateReading))
                {
                    // Rimuovi prima i task
                    var tasksToRemove = existingFase.TaskDaEseguires.ToList();
                    foreach (var task in tasksToRemove)
                    {
                        context.TaskDaEseguires.Remove(task);
                    }

                    // Poi rimuovi la fase
                    context.LavorazioniFasiDataReadings.Remove(existingFase);
                }
            }

            // Aggiorna o aggiungi fasi
            foreach (var faseDto in fasiDto)
            {
                if (faseDto.IdlavorazioneFaseDateReading > 0)
                {
                    // Aggiorna fase esistente
                    var existingFase = existingFasi
                        .FirstOrDefault(f => f.IdlavorazioneFaseDateReading == faseDto.IdlavorazioneFaseDateReading);

                    if (existingFase != null)
                    {
                        existingFase.IdFaseLavorazione = faseDto.IdFaseLavorazione;
                        existingFase.FlagDataReading = faseDto.FlagDataReading;
                        existingFase.FlagGraficoDocumenti = faseDto.FlagGraficoDocumenti;

                        // Gestisci task per fase esistente
                        if (faseDto.TaskDaEseguireDto != null)
                        {
                            await UpdateTaskDaEseguireAsync(context, existingFase, faseDto.TaskDaEseguireDto);
                        }
                    }
                }
                else
                {
                    // Aggiungi nuova fase
                    var newFase = new LavorazioniFasiDataReading
                    {
                        IdProceduraLavorazione = lavorazione.IdproceduraLavorazione,
                        IdFaseLavorazione = faseDto.IdFaseLavorazione,
                        FlagDataReading = faseDto.FlagDataReading,
                        FlagGraficoDocumenti = faseDto.FlagGraficoDocumenti
                    };

                    context.LavorazioniFasiDataReadings.Add(newFase);

                    // Aggiungi task per la nuova fase
                    if (faseDto.TaskDaEseguireDto != null)
                    {
                        foreach (var taskDto in faseDto.TaskDaEseguireDto)
                        {
                            // **VALIDAZIONE: Controlla IdConfigurazioneDatabase prima di creare nuovo task**
                            if (taskDto.IdConfigurazioneDatabase.HasValue)
                            {
                                var configExists = await context.ConfigurazioneFontiDatis
                                    .AnyAsync(c => c.IdConfigurazione == taskDto.IdConfigurazioneDatabase.Value);

                                if (!configExists)
                                {
                                    logger.LogError("Tentativo di creare task con IdConfigurazioneDatabase non valido: IdConfig={IdConfig}", 
                                        taskDto.IdConfigurazioneDatabase.Value);
                                    throw new InvalidOperationException(
                                        $"Configurazione con ID {taskDto.IdConfigurazioneDatabase.Value} non esiste");
                                }
                            }

                            var correctedTaskDto = new TaskDaEseguireDto
                            {
                                IdTask = taskDto.IdTask,
                                TaskName = taskDto.TaskName,
                                Descrizione = taskDto.Descrizione,
                                TipoTask = taskDto.TipoTask,
                                TimeTask = taskDto.TimeTask,
                                GiorniPrecedenti = taskDto.GiorniPrecedenti,
                                Stato = taskDto.Stato,
                                DataStato = taskDto.DataStato,
                                Enabled = taskDto.Enabled,
                                CronExpression = taskDto.CronExpression,
                                LastRunUtc = taskDto.LastRunUtc,
                                LastError = taskDto.LastError,
                                ConsecutiveFailures = taskDto.ConsecutiveFailures,
                                IdTaskHangFire = taskDto.IdTaskHangFire,
                                IdConfigurazioneDatabase = taskDto.IdConfigurazioneDatabase
                            };

                            var newTask = mapper.Map<TaskDaEseguire>(correctedTaskDto);
                            
                            // **CORREZIONE: Assicurati che IdTaskHangFire non sia null per task nuovi**
                            if (string.IsNullOrWhiteSpace(newTask.IdTaskHangFire))
                            {
                                newTask.IdTaskHangFire = correctedTaskDto.IdTaskHangFire ?? $"temp-{Guid.NewGuid():N}";
                                logger.LogDebug("Assegnato ID Hangfire temporaneo per nuovo task in nuova fase: {TempId}", newTask.IdTaskHangFire);
                            }
                            
                            newFase.TaskDaEseguires.Add(newTask);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pulisce tutte le fasi di una lavorazione esistente
        /// </summary>
        private async Task ClearAllFasiDataReadingsAsync(DematReportsContext context, ProcedureLavorazioni lavorazione)
        {
            var existingFasi = lavorazione.LavorazioniFasiDataReadings.ToList();

            if (existingFasi.Any())
            {
                // Rimuovi prima tutti i task associati
                foreach (var fase in existingFasi)
                {
                    var tasksToRemove = fase.TaskDaEseguires.ToList();
                    foreach (var task in tasksToRemove)
                    {
                        context.TaskDaEseguires.Remove(task);
                    }
                }

                // Poi rimuovi tutte le fasi
                foreach (var fase in existingFasi)
                {
                    context.LavorazioniFasiDataReadings.Remove(fase);
                }
            }
        }

        /// <summary>
        /// Aggiorna le propriet� principali dell'entit�
        /// </summary>
        private void UpdateMainProperties(ProcedureLavorazioni lavorazione, ProcedureLavorazioniDto dto)
        {
            lavorazione.NomeProcedura = dto.NomeProcedura!;
            lavorazione.IdformatoDatiProduzione = dto.IdformatoDatiProduzione;
            lavorazione.Idoperatore = dto.Idoperatore;
            lavorazione.IdproceduraCliente = dto.IdproceduraCliente;
            lavorazione.Idreparti = dto.Idreparti;
            lavorazione.Note = dto.Note;
            lavorazione.LogoBase64 = dto.LogoBase64;
        }

        /// <summary>
        /// Aggiorna i TaskDaEseguire per una fase esistente
        /// </summary>
        private async Task UpdateTaskDaEseguireAsync(DematReportsContext context,
            LavorazioniFasiDataReading existingFase,
            List<TaskDaEseguireDto> taskDtoList)
        {
            var existingTasks = existingFase.TaskDaEseguires.ToList();
            var dtoTaskIds = taskDtoList
                .Where(dto => dto.IdTaskDaEseguire > 0)
                .Select(dto => dto.IdTaskDaEseguire)
                .ToList();

            // Rimuovi task non pi� presenti
            foreach (var existingTask in existingTasks)
            {
                if (!dtoTaskIds.Contains(existingTask.IdTaskDaEseguire))
                {
                    context.TaskDaEseguires.Remove(existingTask);
                }
            }

            // Aggiorna o aggiungi task
            foreach (var taskDto in taskDtoList)
            {
                if (taskDto.IdTaskDaEseguire > 0)
                {
                    // Aggiorna task esistente
                    var existingTask = existingTasks
                        .FirstOrDefault(t => t.IdTaskDaEseguire == taskDto.IdTaskDaEseguire);

                    if (existingTask != null)
                    {
                        // **VALIDAZIONE: Controlla IdConfigurazioneDatabase prima dell'aggiornamento**
                        if (taskDto.IdConfigurazioneDatabase.HasValue)
                        {
                            var configExists = await context.ConfigurazioneFontiDatis
                                .AnyAsync(c => c.IdConfigurazione == taskDto.IdConfigurazioneDatabase.Value);

                            if (!configExists)
                            {
                                logger.LogError("Tentativo di aggiornare task con IdConfigurazioneDatabase non valido: TaskId={TaskId}, IdConfig={IdConfig}", 
                                    taskDto.IdTaskDaEseguire, taskDto.IdConfigurazioneDatabase.Value);
                                throw new InvalidOperationException(
                                    $"Configurazione con ID {taskDto.IdConfigurazioneDatabase.Value} non esiste");
                            }
                        }

                        // Mapping propriet�
                        existingTask.IdTaskHangFire = taskDto.IdTaskHangFire ?? string.Empty;
                        existingTask.GiorniPrecedenti = taskDto.GiorniPrecedenti;
                        existingTask.Stato = taskDto.Stato ?? "Attivo";
                        existingTask.DataStato = taskDto.DataStato != DateTime.MinValue ? taskDto.DataStato : DateTime.Now;
                        existingTask.Enabled = taskDto.Enabled;
                        existingTask.CronExpression = taskDto.CronExpression;
                        existingTask.LastRunUtc = taskDto.LastRunUtc;
                        existingTask.LastError = taskDto.LastError;
                        existingTask.ConsecutiveFailures = taskDto.ConsecutiveFailures;
                        existingTask.IdConfigurazioneDatabase = taskDto.IdConfigurazioneDatabase;

                    }
                }
                else
                {
                    // Aggiungi nuovo task
                    var newTask = mapper.Map<TaskDaEseguire>(taskDto);
                    
                    // **CORREZIONE: Assicurati che IdTaskHangFire non sia null per task nuovi**
                    if (string.IsNullOrWhiteSpace(newTask.IdTaskHangFire))
                    {
                        newTask.IdTaskHangFire = taskDto.IdTaskHangFire ?? $"temp-{Guid.NewGuid():N}";
                        logger.LogDebug("Assegnato ID Hangfire temporaneo per nuovo task: {TempId}", newTask.IdTaskHangFire);
                    }
                    
                    // set propriet� aggiuntive non coperte dal mapping (in caso profilo non aggiornato)
                    newTask.Enabled = taskDto.Enabled;
                    newTask.IdConfigurazioneDatabase = taskDto.IdConfigurazioneDatabase;
                    newTask.CronExpression = taskDto.CronExpression;
                    newTask.LastRunUtc = taskDto.LastRunUtc;
                    newTask.LastError = taskDto.LastError;
                    newTask.ConsecutiveFailures = taskDto.ConsecutiveFailures;
                    newTask.IdLavorazioneFaseDateReading = existingFase.IdlavorazioneFaseDateReading;
                    existingFase.TaskDaEseguires.Add(newTask);
                }
            }
        }

        /// <summary>
        /// Restituisce una singola procedura di lavorazione identificata per nome con fasi e task associati come DTO.
        /// </summary>
        /// <param name="nomeProcedura">Nome della procedura di lavorazione da cercare.</param>
        /// <returns>DTO della procedura di lavorazione con fasi e task inclusi o null se non trovata.</returns>
        public async Task<ProcedureLavorazioniDto?> GetSingleProceduraLavorazioneDtoAsync(string nomeProcedura)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.LavorazioniFasiDataReadings!).ThenInclude(x => x.TaskDaEseguires)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.NomeProcedura == nomeProcedura);
        }

        /// <summary>
        /// Restituisce tutte le procedure di lavorazione come DTO con tutte le relazioni per uso amministrativo.
        /// </summary>
        /// <returns>Lista completa di DTO delle procedure con operatori, reparti, formati, query, fasi e task inclusi.</returns>
        public async Task<List<ProcedureLavorazioniDto>> GetProcedureLavorazioniDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdrepartiNavigation)
                .Include(x => x.IdformatoDatiProduzioneNavigation)
                .Include(x => x.QueryProcedureLavorazionis)
                .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires)
                .Include(x => x.IdproceduraClienteNavigation).ThenInclude(p => p!.IdclienteNavigation).ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                .OrderBy(x => x.NomeProcedura)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }

        /// <summary>
        /// Restituisce una singola procedura di lavorazione come DTO con tutte le relazioni.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione da cercare.</param>
        /// <returns>DTO della procedura di lavorazione con tutte le relazioni incluse o null se non trovata.</returns>
        public async Task<ProcedureLavorazioniDto?> GetProceduraLavorazioneDtoAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation!)
                .Include(x => x.IdrepartiNavigation!)
                .Include(x => x.IdformatoDatiProduzioneNavigation!)
                .Include(x => x.QueryProcedureLavorazionis!)
                .Include(x => x.LavorazioniFasiDataReadings!).ThenInclude(x => x.TaskDaEseguires)
                .Include(x => x.IdproceduraClienteNavigation!).ThenInclude(p => p.IdclienteNavigation).ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(x => x.IdproceduraLavorazione == idProceduraLavorazione);
        }


        /// <summary>
        /// Restituisce le procedure di lavorazione di un centro come DTO con tutte le relazioni complesse (versione alternativa).
        /// </summary>
        /// <param name="idCentro">ID del centro di lavorazione per il filtro.</param>
        /// <returns>Lista di DTO delle procedure del centro con operatori, reparti, formati, query, fasi e task inclusi.</returns>
        public async Task<List<ProcedureLavorazioniDto>> GetProcedureLavorazioniDtoByCentroAsync(int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureLavorazionis
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdrepartiNavigation)
                .Include(x => x.IdformatoDatiProduzioneNavigation)
                .Include(x => x.QueryProcedureLavorazionis)
                .Include(x => x.LavorazioniFasiDataReadings).ThenInclude(x => x.TaskDaEseguires)
                .Include(x => x.IdproceduraClienteNavigation).ThenInclude(p => p!.IdclienteNavigation).ThenInclude(p => p!.IdCentroLavorazioneNavigation)
                .AsNoTracking()
                .Where(x => x.Idcentro == idCentro)
                .OrderBy(x => x.NomeProcedura)
                .ProjectTo<ProcedureLavorazioniDto>(mapper.ConfigurationProvider)
                .ToListAsync();
        }


        /// <summary>
        /// Restituisce tutte le procedure di lavorazione con proiezione personalizzata per migliori performance.
        /// </summary>
        /// <returns>Lista di DTO con campi selezionati e navigazione ottimizzata filtrata per l'utente corrente.</returns>
        public async Task<List<ProcedureLavorazioniDto>?> GetAllProcedureLavorazioniDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            if (configUser.IsAdminRole)
            {
                return await context.ProcedureLavorazionis
                    .OrderBy(x => x.NomeProcedura)
                    .Select(x => new ProcedureLavorazioniDto
                    {
                        IdproceduraLavorazione = x.IdproceduraLavorazione,
                        NomeProcedura = x.NomeProcedura,
                        FormatoDatiProduzione = x.IdformatoDatiProduzioneNavigation != null ? x.IdformatoDatiProduzioneNavigation.FormatoDatiProduzione : null,
                        Reparto = x.IdrepartiNavigation != null ? x.IdrepartiNavigation.Reparti : null,
                        Centro = x.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro,
                        Idcentro = x.Idcentro,
                        Idreparti = x.Idreparti
                    })
                    .ToListAsync();
            }
            else
            {
                return await context.ProcedureLavorazionis
                    .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                    .OrderBy(x => x.NomeProcedura)
                    .Select(x => new ProcedureLavorazioniDto
                    {
                        IdproceduraLavorazione = x.IdproceduraLavorazione,
                        NomeProcedura = x.NomeProcedura,
                        FormatoDatiProduzione = x.IdformatoDatiProduzioneNavigation != null ? x.IdformatoDatiProduzioneNavigation.FormatoDatiProduzione : null,
                        Reparto = x.IdrepartiNavigation != null ? x.IdrepartiNavigation.Reparti : null,
                        Centro = x.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro
                    })
                    .ToListAsync();
            }
        }
    }
}
