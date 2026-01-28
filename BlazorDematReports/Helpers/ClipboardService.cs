using Microsoft.JSInterop;

namespace BlazorDematReports.Helpers
{
    /// <summary>
    /// Servizio per la gestione della clipboard tramite interoperabilità JavaScript in Blazor.
    /// </summary>
    public class ClipboardService : IClipboardService
    {
        private readonly IJSRuntime _jsInterop;

        /// <summary>
        /// Costruttore che accetta l'istanza di IJSRuntime per l'interoperabilità JS.
        /// </summary>
        /// <param name="jsInterop">Istanza di IJSRuntime.</param>
        public ClipboardService(IJSRuntime jsInterop)
        {
            _jsInterop = jsInterop;
        }

        /// <summary>
        /// Copia il testo specificato negli appunti del browser tramite JavaScript.
        /// </summary>
        /// <param name="text">Testo da copiare negli appunti.</param>
        public async Task CopyToClipboard(string? text)
        {
            await _jsInterop.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
