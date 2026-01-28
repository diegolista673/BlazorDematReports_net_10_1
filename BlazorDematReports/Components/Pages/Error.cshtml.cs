using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace BlazorDematReports.Pages
{
    /// <summary>
    /// Modello di pagina per la gestione degli errori.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        /// <summary>
        /// Identificativo della richiesta corrente.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Indica se mostrare l'identificativo della richiesta.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        /// <summary>
        /// Costruttore che accetta un logger per la classe ErrorModel.
        /// </summary>
        /// <param name="logger">Logger per la gestione degli errori.</param>
        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gestisce la richiesta GET e imposta l'identificativo della richiesta.
        /// </summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}