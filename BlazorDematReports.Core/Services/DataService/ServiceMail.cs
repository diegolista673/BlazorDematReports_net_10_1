using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
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
        private readonly ConfigUser configUser;
        private readonly IDbContextFactory<DematReportsContext> contextFactory;
        private readonly ILogger<ServiceMail> logger;
        private readonly IFluentEmail fluentEmail;

        /// <summary>
        /// Inizializza una nuova istanza del servizio per la gestione dell'invio email e configurazioni mail.
        /// </summary>
        /// <param name="configUser">Configurazione utente per controllo autorizzazioni.</param>
        /// <param name="contextFactory">Factory per la creazione di contesti database.</param>
        /// <param name="fluentEmail">Servizio FluentEmail per invio email.</param>
        /// <param name="logger">Logger per registrare operazioni e errori.</param>
        public ServiceMail(
            ConfigUser configUser,
            IDbContextFactory<DematReportsContext> contextFactory,
            IFluentEmail fluentEmail,
            ILogger<ServiceMail> logger)
        {
            this.configUser = configUser;
            this.contextFactory = contextFactory;
            this.fluentEmail = fluentEmail;
            this.logger = logger;
        }



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
