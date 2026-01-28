using Microsoft.JSInterop;

namespace BlazorDematReports.Helpers
{
    /// <summary>
    /// Classe helper per operazioni sui file.
    /// Fornisce metodi per salvare file utilizzando JavaScript tramite IJSRuntime.
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Salva un file sul client utilizzando JavaScript.
        /// </summary>
        /// <param name="js">Istanza di IJSRuntime per l'interazione con JavaScript.</param>
        /// <param name="filename">Nome del file da salvare.</param>
        /// <param name="data">Dati del file in formato byte array.</param>
        /// <returns>Un ValueTask che rappresenta l'operazione asincrona.</returns>
        public static ValueTask<object> SaveAs(this IJSRuntime js, string filename, byte[] data)
            => js.InvokeAsync<object>(
                "saveAsFile",
                filename,
                Convert.ToBase64String(data));
    }
}
