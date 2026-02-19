using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Helpers; // Aggiornato: usare helper unificato
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per la gestione delle procedure clienti e delle relative operazioni sui dati.
    /// </summary>
    public class ServiceProcedureClienti : ServiceBase<ProcedureCliente>, IServiceProcedureClienti
    {

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie per la gestione delle procedure clienti.
        /// </summary>
        /// <param name="mapper">Servizio per la mappatura tra entit� e DTO.</param>
        /// <param name="configUser">Configurazione dell'utente corrente.</param>
        /// <param name="contextFactory">Factory per la creazione del contesto dati.</param>
        /// <param name="logger">Logger per il tracking delle operazioni.</param>
        public ServiceProcedureClienti(IMapper mapper, ConfigUser configUser, IDbContextFactory<DematReportsContext> contextFactory, ILogger<ServiceProcedureClienti> logger) : base(contextFactory, logger, mapper, configUser)
        {
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            return await context.ProcedureClientes
                .Include(x => x.IdoperatoreNavigation)
                .Include(x => x.IdclienteNavigation).ThenInclude(x => x!.IdCentroLavorazioneNavigation)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteByUserAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
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

            using var context = contextFactory.CreateDbContext();
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
            var LstProcedureClienteDto = mapper.Map<List<ProcedureCliente>, List<ProcedureClienteDto>>(lst);
            LstProcedureClienteDto = LstProcedureClienteDto.OrderBy(x => x.ProceduraCliente).ToList();
            return LstProcedureClienteDto;
        }

        /// <inheritdoc/>
        public async Task<List<ProcedureCliente>> GetProcedureClienteByCentroAsync(int idCentro)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
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

            using var context = contextFactory.CreateDbContext();
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

            var entity = mapper.Map<ProcedureCliente>(procedureClienteDto);
            using var context = contextFactory.CreateDbContext();
            context.ProcedureClientes.Add(entity);
            await context.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateProcedureClienteAsync(ProcedureClienteDto procedureClienteDto)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            using var context = contextFactory.CreateDbContext();
            var proCliente = await context.ProcedureClientes.Where(x => x.IdproceduraCliente.Equals(procedureClienteDto.IdproceduraCliente)).FirstOrDefaultAsync();
            if (proCliente != null)
            {
                proCliente.ProceduraCliente = procedureClienteDto.ProceduraCliente;
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
