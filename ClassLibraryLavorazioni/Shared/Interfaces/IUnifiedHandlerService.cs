using LibraryLavorazioni.Shared.Models;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryLavorazioni.Shared.Interfaces
{
    /// <summary>
    /// Interfaccia per il servizio unificato di gestione degli handler.
    /// </summary>
    public interface IUnifiedHandlerService
    {
        /// <summary>
        /// Esegue un handler utilizzando il codice identificativo.
        /// </summary>
        /// <param name="handlerCode">Codice identificativo dell'handler da eseguire.</param>
        /// <param name="context">Contesto di esecuzione unificato.</param>
        /// <param name="ct">Token di cancellazione per gestire l'interruzione dell'operazione.</param>
        /// <returns>Risultato dell'esecuzione dell'handler.</returns>
        Task<object> ExecuteHandlerAsync(string handlerCode, UnifiedExecutionContext context, CancellationToken ct = default);

        /// <summary>
        /// Restituisce la collezione di tutti gli handler disponibili nel sistema.
        /// </summary>
        /// <returns>Collezione read-only dei codici degli handler disponibili.</returns>
        Task<IReadOnlyCollection<string>> GetAvailableHandlersAsync();

        /// <summary>
        /// Restituisce gli handler filtrati per tipo.
        /// </summary>
        /// <param name="type">Tipo di handler da filtrare.</param>
        /// <returns>Collezione di handler del tipo specificato.</returns>
        Task<IReadOnlyCollection<IUnifiedHandler>> GetHandlersByTypeAsync(HandlerType type);

        /// <summary>
        /// Verifica se un handler specifico è disponibile nel sistema.
        /// </summary>
        /// <param name="handlerCode">Codice identificativo dell'handler da verificare.</param>
        /// <returns>True se l'handler è disponibile, false altrimenti.</returns>
        Task<bool> IsHandlerAvailableAsync(string handlerCode);
    }
}