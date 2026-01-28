namespace BlazorDematReports.Helpers
{
    /// <summary>
    /// Interfaccia per la gestione della clipboard tramite Blazor e JavaScript interop.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Copia il testo specificato negli appunti del browser.
        /// </summary>
        /// <param name="text">Testo da copiare negli appunti.</param>
        Task CopyToClipboard(string? text);
    }
}
