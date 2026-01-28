using FluentEmail.Core;

namespace BlazorDematReports.Services.DataService
{
    /// <summary>
    /// Servizio per l'invio di email tramite FluentEmail.
    /// </summary>
    public class EmailService
    {
        private IFluentEmail _fluentEmail;

        /// <summary>
        /// Costruttore che accetta un'istanza di IFluentEmail.
        /// </summary>
        /// <param name="fluentEmail">Istanza di IFluentEmail per la composizione e l'invio delle email.</param>
        public EmailService(IFluentEmail fluentEmail)
        {
            _fluentEmail = fluentEmail;
        }

        /// <summary>
        /// Invia una email semplice a un destinatario predefinito.
        /// </summary>
        public async Task Send()
        {
            await _fluentEmail.To("diego.lista@postel.it")
            .Body("The body").SendAsync();
        }

        /// <summary>
        /// Invia una email con mittente, destinatario, oggetto e corpo specificati.
        /// </summary>
        public async Task Send2()
        {
            var email = await Email
                .From("diego.lista@postel.it")
                .To("diegolista673@hotmail.com", "bob")
                .Subject("hows it going bob")
                .Body("yo bob, long time no see!")
                .SendAsync();
        }
    }
}
