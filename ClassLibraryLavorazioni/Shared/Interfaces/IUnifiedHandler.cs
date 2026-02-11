using LibraryLavorazioni.Shared.Models;

namespace LibraryLavorazioni.Shared.Interfaces
{
    /// <summary>
    /// Interfaccia unificata per tutti gli handler del sistema.
    /// Supporta sia lavorazioni classiche che importazioni mail.
    /// </summary>
    public interface IUnifiedHandler
    {
        /// <summary>
        /// Codice identificativo univoco dell'handler.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// Tipo di handler per distinguere tra lavorazioni e mail.
        /// </summary>
        HandlerType Type { get; }

        /// <summary>
        /// Descrizione leggibile dell'handler.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Esegue l'handler con il contesto unificato.
        /// </summary>
        /// <param name="context">Contesto di esecuzione unificato.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Risultato dell'esecuzione.</returns>
        Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default);
    }

    /// <summary>
    /// Tipi di handler supportati dal sistema unificato.
    /// </summary>
    public enum HandlerType
    {
        /// <summary>
        /// Handler per lavorazioni (elaborazione dati di produzione, inclusi servizi mail).
        /// </summary>
        Lavorazione
    }
}