using LibraryLavorazioni.Shared.Models;

namespace LibraryLavorazioni.Shared.Interfaces
{
    /// <summary>
    /// Interfaccia per il registry unificato degli handler.
    /// </summary>
    public interface IUnifiedHandlerRegistry
    {
        /// <summary>
        /// Restituisce l'handler corrispondente al codice specificato.
        /// </summary>
        /// <param name="code">Codice dell'handler.</param>
        /// <returns>Handler unificato.</returns>
        /// <exception cref="InvalidOperationException">Lanciata se non esiste un handler per il codice specificato.</exception>
        IUnifiedHandler Get(string code);

        /// <summary>
        /// Esegue direttamente un handler tramite il suo codice.
        /// </summary>
        /// <param name="code">Codice dell'handler da eseguire.</param>
        /// <param name="context">Contesto di esecuzione unificato.</param>
        /// <param name="ct">Token di cancellazione.</param>
        /// <returns>Risultato dell'esecuzione.</returns>
        Task<object> ExecuteAsync(string code, UnifiedExecutionContext context, CancellationToken ct = default);

        /// <summary>
        /// Collezione read-only di tutti i codici disponibili.
        /// </summary>
        IReadOnlyCollection<string> Codes { get; }

        /// <summary>
        /// Restituisce gli handler filtrati per tipo.
        /// </summary>
        /// <param name="type">Tipo di handler da filtrare.</param>
        /// <returns>Collezione di handler del tipo specificato.</returns>
        IReadOnlyCollection<IUnifiedHandler> GetHandlersByType(HandlerType type);

        /// <summary>
        /// Tenta di ottenere un handler senza lanciare eccezioni.
        /// </summary>
        /// <param name="code">Codice dell'handler.</param>
        /// <param name="handler">Handler trovato (se esiste).</param>
        /// <returns>True se l'handler è stato trovato.</returns>
        bool TryGetHandler(string code, out IUnifiedHandler? handler);
    }
}