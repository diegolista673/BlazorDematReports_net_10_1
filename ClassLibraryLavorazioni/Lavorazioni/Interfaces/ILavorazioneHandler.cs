using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility.Models;

namespace LibraryLavorazioni.Lavorazioni.Interfaces
{
    /// <summary>
    /// Contratto per l'esecuzione delle lavorazioni.
    /// </summary>
    public interface ILavorazioneHandler
    {
        /// <summary>
        /// Codice identificativo univoco della lavorazione.
        /// </summary>
        string LavorazioneCode { get; }

        /// <summary>
        /// Esegue la lavorazione specificata.
        /// </summary>
        Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default);

        /// <summary>
        /// Ottiene il codice servizio opzionale (es. per handler mail).
        /// Default: null (nessun codice servizio).
        /// </summary>
        string? GetServiceCode() => null;

        /// <summary>
        /// Ottiene i metadata dell'handler.
        /// Default: metadata vuoto.
        /// </summary>
        HandlerMetadata GetMetadata() => new();
    }

    /// <summary>
    /// Metadata associati a un handler di lavorazione.
    /// Usato per esporre informazioni aggiuntive senza modificare la firma principale.
    /// </summary>
    public record HandlerMetadata
    {
        /// <summary>
        /// Codice servizio opzionale (es. HERA16, ADER4 per handler mail).
        /// </summary>
        public string? ServiceCode { get; init; }

        /// <summary>
        /// Indica se l'handler richiede un servizio email.
        /// </summary>
        public bool RequiresEmailService { get; init; }

        /// <summary>
        /// Categoria dell'handler per UI/discovery.
        /// </summary>
        public string? Category { get; init; }

        /// <summary>
        /// Proprietà aggiuntive custom.
        /// </summary>
        public Dictionary<string, string>? AdditionalProperties { get; init; }
    }
}