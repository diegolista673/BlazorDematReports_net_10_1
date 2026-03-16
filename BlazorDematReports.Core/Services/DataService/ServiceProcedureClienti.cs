using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Application.Mapping;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle procedure clienti.
    /// </summary>
    public class ServiceProcedureClienti : ServiceBase<ProcedureCliente>, IServiceProcedureClienti
    {
        private readonly ClientiMapper _mapper;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie.
        /// </summary>
        /// <param name="mapper">Mapper Mapperly per ProcedureCliente ↔ DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceProcedureClienti(ClientiMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceProcedureClienti> logger) : base(contextFactory, logger, configUser)
        {
            _mapper = mapper;
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.ProcedureClientes
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            if (configUser.IsAdminRole)
            {
                return await context.ProcedureClientes
                    .Include(x => x.IdoperatoreNavigation)
                    .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                    .ToListAsync();
            }
            else
            {
                return await context.ProcedureClientes
                    .Include(x => x.IdoperatoreNavigation)
                    .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                    .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                    .ToListAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureClienteDto>> GetProcedureClienteDtoByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            List<ProcedureCliente> lst;
            if (configUser.IsAdminRole)
            {
                lst = await context.ProcedureClientes
                    .Include(x => x.IdoperatoreNavigation)
                    .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                    .ToListAsync();
            }
            else
            {
                lst = await context.ProcedureClientes
                    .Include(x => x.IdoperatoreNavigation)
                    .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                    .Where(x => x.Idcentro == configUser.IdCentroOrigine)
                    .ToListAsync();
            }
            var LstProcedureClienteDto = lst.Select(_mapper.ProceduraClienteToDto).OrderBy(x => x.ProceduraCliente).ToList();
            return LstProcedureClienteDto;
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteByCentroAsync(int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.ProcedureClientes
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                .Where(x => x.Idcentro == idCentro)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteProceduraClienteAsync(int idProceduraCliente)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var entity = await context.ProcedureClientes.FindAsync(idProceduraCliente);
            if (entity != null)
            {
                context.ProcedureClientes.Remove(entity);
                await context.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task AddProceduraClienteAsync(ProcedureClienteDto procedureClienteDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            var entity = _mapper.DtoToProceduraCliente(procedureClienteDto);
            await using var context = await contextFactory.CreateDbContextAsync();
            context.ProcedureClientes.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateProcedureClienteAsync(ProcedureClienteDto procedureClienteDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            await using var context = await contextFactory.CreateDbContextAsync();
            var proCliente = await context.ProcedureClientes.Where(x => x.IdproceduraCliente.Equals(procedureClienteDto.IdproceduraCliente)).FirstOrDefaultAsync();
            if (proCliente != null)
            {
                proCliente.ProceduraCliente = procedureClienteDto.ProceduraCliente!;
                proCliente.Idcliente = procedureClienteDto.Idcliente;
                proCliente.Idcentro = (int)procedureClienteDto.Idcentro!;
                proCliente.Commessa = procedureClienteDto.Commessa;
                proCliente.DataInserimento = procedureClienteDto.DataInserimento;
                proCliente.Idoperatore = procedureClienteDto.Idoperatore;
                proCliente.DescrizioneProcedura = procedureClienteDto.DescrizioneProcedura;
                await context.SaveChangesAsync();
            }
        }
    }
}
