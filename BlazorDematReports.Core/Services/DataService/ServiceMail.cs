using AutoMapper;
using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Interfaces.IDataService;
using Entities.Enums;
using Entities.Helpers;
using Entities.Models.DbApplication;
using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService
{
    /// <summary>
    /// Servizio per l'invio di email tramite FluentEmail e gestione configurazioni servizi mail.
    /// </summary>
    public class ServiceMail : IServiceMail
    {
        private readonly IMapper mapper;
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceMail> logger;
        private readonly IFluentEmail fluentEmail;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dell'invio email e configurazioni mail.
        /// </summary>
        /// <param name="mapper">Mapper per conversioni tra entità e DTO.</param>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="fluentEmail">Servizio FluentEmail per invio email.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceMail(
            IMapper mapper,
            ConfigUser configUser,
            IDbContextFactory<DematReportsContext> contextFactory,
            IFluentEmail fluentEmail,
            ILogger<ServiceMail> logger)
        {
            this.mapper = mapper;
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.fluentEmail = fluentEmail;
            this.logger = logger;
        }

        /// <summary>
        /// Conta il numero totale di servizi mail configurati (configurazioni EmailCSV attive).
        /// </summary>
        /// <returns>Numero di configurazioni EmailCSV attive.</returns>
        public async Task<int> GetMailServicesCountAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                var count = await MailConfigurationQuery(context)
                    .CountAsync();

                return count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel conteggio dei servizi mail");
                return 0;
            }
        }

        /// <summary>
        /// Conta i servizi mail associati a una specifica procedura di lavorazione.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <returns>Numero di configurazioni EmailCSV attive per la procedura specificata.</returns>
        public async Task<int> GetMailServicesCountByProceduraAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                var count = await MailConfigurationQuery(context)
                    .Where(c => c.ConfigurazioneFaseCentros.Any(fc =>
                           fc.FlagAttiva == true &&
                           fc.IdProceduraLavorazione == idProceduraLavorazione))
                    .CountAsync();

                return count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel conteggio dei servizi mail per procedura {IdProcedura}", idProceduraLavorazione);
                return 0;
            }
        }

        /// <summary>
        /// Restituisce tutte le configurazioni EmailCSV attive.
        /// </summary>
        /// <returns>Lista di configurazioni fonti dati di tipo EmailCSV.</returns>
        public async Task<List<ConfigurazioneFontiDati>> GetMailServicesAsync()
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                return await MailConfigurationQuery(context)
                    .Include(c => c.ConfigurazioneFaseCentros.Where(fc => fc.FlagAttiva == true))
                        .ThenInclude(fc => fc.IdProceduraLavorazioneNavigation.NomeProcedura)
                    .Include(c => c.ConfigurazioneFaseCentros.Where(fc => fc.FlagAttiva))
                        .ThenInclude(fc => fc.IdFaseLavorazioneNavigation.FaseLavorazione)
                    .OrderBy(c => c.HandlerClassName)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel recupero dei servizi mail");
                return new List<ConfigurazioneFontiDati>();
            }
        }

        /// <summary>
        /// Restituisce le configurazioni EmailCSV per una specifica procedura.
        /// </summary>
        /// <param name="idProceduraLavorazione">ID della procedura di lavorazione.</param>
        /// <returns>Lista di configurazioni EmailCSV per la procedura specificata.</returns>
        public async Task<List<ConfigurazioneFontiDati>> GetMailServicesByProceduraAsync(int idProceduraLavorazione)
        {
            QueryLoggingHelper.LogQueryExecution(logger);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                return await MailConfigurationQuery(context)
                    .Where(c => c.ConfigurazioneFaseCentros.Any(fc =>
                        fc.FlagAttiva == true && fc.IdProceduraLavorazione == idProceduraLavorazione))
                    .Include(c => c.ConfigurazioneFaseCentros.Where(fc =>
                        fc.FlagAttiva == true && fc.IdProceduraLavorazione == idProceduraLavorazione))
                        .ThenInclude(fc => fc.IdFaseLavorazioneNavigation.FaseLavorazione)
                    .OrderBy(c => c.HandlerClassName)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Errore nel recupero dei servizi mail per procedura {IdProcedura}", idProceduraLavorazione);
                return new List<ConfigurazioneFontiDati>();
            }
        }

        private static IQueryable<ConfigurazioneFontiDati> MailConfigurationQuery(DematReportsContext context)
            => context.ConfigurazioneFontiDatis.Where(c => c.TipoFonte == TipoFonteData.HandlerIntegrato // ✅ Type-safe!
                && !string.IsNullOrWhiteSpace(c.HandlerClassName)
                && (c.HandlerClassName == "Hera16EwsHandler" || c.HandlerClassName == "Ader4Handler"));

        /// <summary>
        /// Invia una email semplice.
        /// </summary>
        /// <param name="to">Indirizzo email destinatario.</param>
        /// <param name="subject">Oggetto dell'email.</param>
        /// <param name="body">Corpo dell'email.</param>
        /// <returns>True se l'invio è riuscito, false altrimenti.</returns>
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                logger.LogInformation("Invio email a {To} con oggetto: {Subject}", to, subject);

                var response = await fluentEmail
                    .To(to)
                    .Subject(subject)
                    .Body(body)
                    .SendAsync();

                if (response.Successful)
                {
                    logger.LogInformation("Email inviata con successo a {To}", to);
                    return true;
                }
                else
                {
                    logger.LogWarning("Errore nell'invio email a {To}: {Errors}", to, string.Join(", ", response.ErrorMessages));
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Eccezione durante l'invio email a {To}", to);
                return false;
            }
        }

        /// <summary>
        /// Invia una email con mittente, destinatario, oggetto e corpo specificati.
        /// </summary>
        /// <param name="from">Indirizzo email mittente.</param>
        /// <param name="to">Indirizzo email destinatario.</param>
        /// <param name="toName">Nome del destinatario.</param>
        /// <param name="subject">Oggetto dell'email.</param>
        /// <param name="body">Corpo dell'email.</param>
        /// <returns>True se l'invio è riuscito, false altrimenti.</returns>
        public async Task<bool> SendEmailAsync(string from, string to, string toName, string subject, string body)
        {
            try
            {
                logger.LogInformation("Invio email da {From} a {To} con oggetto: {Subject}", from, to, subject);

                var response = await fluentEmail
                    .SetFrom(from)
                    .To(to, toName)
                    .Subject(subject)
                    .Body(body)
                    .SendAsync();

                if (response.Successful)
                {
                    logger.LogInformation("Email inviata con successo da {From} a {To}", from, to);
                    return true;
                }
                else
                {
                    logger.LogWarning("Errore nell'invio email a {To}: {Errors}", to, string.Join(", ", response.ErrorMessages));
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Eccezione durante l'invio email a {To}", to);
                return false;
            }
        }
    }
}
