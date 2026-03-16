namespace BlazorDematReports.Core.Services.Email;

/// <summary>
/// Contratto per l'elaborazione batch di email con allegati CSV.
/// Astrae il trasporto sottostante (Exchange EWS, cartella locale) per permettere
/// testing senza accesso al server di posta aziendale.
/// </summary>
public interface IEmailBatchProcessor
{
    /// <summary>
    /// Elabora le email/file disponibili e restituisce il risultato del batch.
    /// </summary>
    /// <param name="ct">Token di cancellazione.</param>
    Task<BatchEmailProcessingResult> ProcessEmailsAsync(CancellationToken ct = default);
}
