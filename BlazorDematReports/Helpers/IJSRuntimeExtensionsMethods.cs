using Microsoft.JSInterop;

namespace BlazorDematReports.Helpers
{
    /// <summary>
    /// Metodi di estensione per IJSRuntime per l'inizializzazione di funzionalità JavaScript personalizzate.
    /// </summary>
    public static class IJSRuntimeExtensionsMethods
    {
        /// <summary>
        /// Inizializza un timer di inattività lato client tramite JavaScript.
        /// </summary>
        /// <typeparam name="T">Tipo dell'oggetto .NET da referenziare.</typeparam>
        /// <param name="js">Istanza di IJSRuntime.</param>
        /// <param name="dotNetObjectReference">Riferimento all'oggetto .NET per callback JS.</param>
        public static async ValueTask InitializeInactivityTimer<T>(this IJSRuntime js, DotNetObjectReference<T> dotNetObjectReference) where T : class
        {
            await js.InvokeVoidAsync("initializeInactivityTimer", dotNetObjectReference);
        }
    }
}
