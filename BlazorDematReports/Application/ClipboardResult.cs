namespace BlazorDematReports.Application;

/// <summary>
/// Risultato dell'operazione di copia nella clipboard tramite JSInterop.
/// </summary>
internal sealed record ClipboardResult(
    bool Success,
    string? Method,
    string? Error,
    string? Details);
