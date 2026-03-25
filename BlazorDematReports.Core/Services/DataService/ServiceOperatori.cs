using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Application.Mapping;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione degli operatori nel sistema.
    /// </summary>
    public class ServiceOperatori : ServiceBase<Operatori>, IServiceOperatori
    {
        private readonly OperatoriMapper _mapper;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ServiceOperatori"/>.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per la conversione tra entità e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceOperatori(OperatoriMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceOperatori> logger)
            : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Restituisce tutti gli operatori del sistema con le relative informazioni del centro di lavoro.
        /// </summary>
        /// <returns>Lista di tutti gli operatori con dati del centro inclusi.</returns>
        public async Task<List<Operatori>> GetOperatoriAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Operatoris.Include(x => x.IdcentroNavigation).ToListAsync();
        }

        /// <summary>
        /// Restituisce gli operatori filtrati in base al ruolo dell'utente corrente e al centro di appartenenza.
        /// </summary>
        /// <returns>Lista degli operatori accessibili all'utente corrente.</returns>
        public async Task<List<Operatori>> GetOperatoriByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            if (configUser.IsAdminRole)
            {
                return await context.Operatoris.Include(x => x.IdcentroNavigation)
                                      .Where(x => x.FlagOperatoreAttivo == true)
                                      .OrderBy(x => x.Operatore)
                                      .ToListAsync();
            }
            else
            {
                return await context.Operatoris.Include(x => x.IdcentroNavigation)
                                      .Where(x => x.Idcentro == configUser.IdCentroOrigine && x.Azienda.ToLower() == "postel" && x.FlagOperatoreAttivo == true)
                                      .OrderBy(x => x.Operatore)
                                      .ToListAsync();
            }
        }

        /// <summary>
        /// Restituisce tutti gli operatori appartenenti al centro di lavoro specificato.
        /// </summary>
        /// <param name="iDcentro">ID del centro di lavoro per il filtro.</param>
        /// <returns>Lista degli operatori del centro specificato.</returns>
        public async Task<List<Operatori>> GetOperatoriByIdCentroAsync(int iDcentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Operatoris.Where(c => c.Idcentro.Equals(iDcentro)).Include(x => x.IdcentroNavigation).ToListAsync();
        }

        /// <summary>
        /// Restituisce un operatore specifico identificato dall'ID con i dati del centro inclusi.
        /// </summary>
        /// <param name="idOperatore">ID dell'operatore da cercare.</param>
        /// <returns>L'operatore specificato o null se non trovato.</returns>
        public async Task<Operatori?> GetOperatoriByIdAsync(int idOperatore)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Operatoris.Where(x => x.Idoperatore.Equals(idOperatore)).Include(x => x.IdcentroNavigation).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Aggiunge un nuovo operatore al sistema con hash della password e associazione ai centri visibili.
        /// </summary>
        /// <param name="oper">DTO dell'operatore da aggiungere con i centri visibili.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddOperatoreAsync(OperatoriDto oper)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            Operatori operatore = _mapper.DtoToOperatore(oper);
            operatore.Operatore = operatore.Operatore.ToLower();
            var pwd = operatore.Password;
            var passwordHasher = new PasswordHasher<string>();
            var hash = passwordHasher.HashPassword(string.Empty, pwd ?? string.Empty);
            operatore.Password = hash;
            List<CentriVisibili> lst = oper.CentriVisibiliDto?.Select(_mapper.DtoToCentroVisibile).ToList() ?? [];
            operatore.CentriVisibilis = lst;
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Operatoris.Add(operatore);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Aggiunge un nuovo operatore al sistema utilizzando l'entit� diretta con hash della password.
        /// </summary>
        /// <param name="oper">Entit� operatore da aggiungere.</param>
        /// <returns>Task asincrono per l'operazione di inserimento.</returns>
        public async Task AddOperatoreAsync(Operatori oper)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            Operatori operatore = oper;
            operatore.Operatore = operatore.Operatore.ToLower();
            var pwd = operatore.Password;
            var passwordHasher = new PasswordHasher<string>();
            var hash = passwordHasher.HashPassword(operatore.Operatore, pwd ?? string.Empty);
            operatore.Password = hash;
            await using var context = await contextFactory.CreateDbContextAsync();
            context.Operatoris.Add(operatore);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina un operatore dal sistema utilizzando l'ID specificato.
        /// </summary>
        /// <param name="idOperatore">ID dell'operatore da eliminare.</param>
        /// <returns>Task asincrono per l'operazione di eliminazione.</returns>
        public async Task DeleteOperatoreAsync(int idOperatore)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.Operatoris.FindAsync(idOperatore);
            if (entity != null)
            {
                context.Operatoris.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Aggiorna un operatore esistente con i nuovi dati e rigenera le associazioni ai centri visibili.
        /// </summary>
        /// <param name="oper">DTO dell'operatore con i dati aggiornati.</param>
        /// <returns>Task asincrono per l'operazione di aggiornamento.</returns>
        public async Task UpdateOperatoreAsync(OperatoriDto oper)
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var operatoreOriginal = await context.Operatoris.Where(x => x.Idoperatore.Equals(oper.Idoperatore)).Include(x => x.IdcentroNavigation).Include(p => p.CentriVisibilis).FirstOrDefaultAsync();
            if (operatoreOriginal != null)
            {
                operatoreOriginal.Operatore = oper.Operatore!;
                operatoreOriginal.Azienda = oper.Azienda!;
                operatoreOriginal.Idcentro = (int)oper.Idcentro!;
                operatoreOriginal.FlagOperatoreAttivo = oper.FlagOperatoreAttivo!;
                operatoreOriginal.IdRuolo = (int)oper.IdRuolo!;
                List<CentriVisibili> lstDaAggiornare = oper.CentriVisibiliDto?.Select(_mapper.DtoToCentroVisibile).ToList() ?? [];
                context.CentriVisibilis.RemoveRange(operatoreOriginal.CentriVisibilis);
                context.CentriVisibilis.AddRange(lstDaAggiornare);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Restituisce tutti gli operatori come DTO con proiezione diretta per ottimizzare le performance.
        /// </summary>
        /// <returns>Lista di DTO degli operatori filtrati per l'utente corrente.</returns>
        public async Task<List<OperatoriDto>> GetOperatoriDtoAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger: logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            if (configUser.IsAdminRole)
            {
                var entities = await context.Operatoris
                              .Include(x => x.IdcentroNavigation)
                              .Include(x => x.CentriVisibilis)
                                  .ThenInclude(cv => cv.IdCentroNavigation)
                              .AsNoTracking()
                              .OrderBy(x => x.Operatore)
                              .ToListAsync();
                return entities.Select(o => _mapper.OperatoreToDto(o)).ToList();
            }
            else
            {
                var entities = await context.Operatoris
                              .Include(x => x.IdcentroNavigation)
                              .Include(x => x.CentriVisibilis)
                                  .ThenInclude(cv => cv.IdCentroNavigation)
                              .AsNoTracking()
                              .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                              .OrderBy(x => x.Operatore)
                              .ToListAsync();
                return entities.Select(o => _mapper.OperatoreToDto(o)).ToList();
            }
        }
    }
}
