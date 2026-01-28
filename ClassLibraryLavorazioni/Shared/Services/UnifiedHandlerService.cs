using LibraryLavorazioni.Shared.Interfaces;
using LibraryLavorazioni.Shared.Models;
using Microsoft.Extensions.Logging;

namespace LibraryLavorazioni.Shared.Services
{
    /// <summary>
    /// Servizio unificato per l'esecuzione di tutti i tipi di handler del sistema.
    /// Fornisce un'interfaccia comune per lavorazioni e importazioni mail.
    /// </summary>
    public class UnifiedHandlerService : IUnifiedHandlerService
    {
        private readonly IUnifiedHandlerRegistry _registry;
        private readonly ILogger<UnifiedHandlerService> _logger;

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="UnifiedHandlerService"/>.
        /// </summary>
        /// <param name="registry">Registry unificato degli handler.</param>
        /// <param name="logger">Logger per la registrazione degli eventi.</param>
        public UnifiedHandlerService(IUnifiedHandlerRegistry registry, ILogger<UnifiedHandlerService> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        /// <summary>
        /// Esegue un handler utilizzando il codice identificativo.
        /// </summary>
        /// <param name="handlerCode">Codice identificativo dell'handler da eseguire.</param>
        /// <param name="context">Contesto di esecuzione unificato.</param>
        /// <param name="ct">Token di cancellazione per gestire l'interruzione dell'operazione.</param>
        /// <returns>Risultato dell'esecuzione dell'handler.</returns>
        public async Task<object> ExecuteHandlerAsync(string handlerCode, UnifiedExecutionContext context, CancellationToken ct = default)
        {
            _logger.LogInformation("Executing unified handler: {HandlerCode}", handlerCode);

            // Prova prima con il codice esatto
            var resolvedCode = ResolveHandlerCode(handlerCode);
            var result = await _registry.ExecuteAsync(resolvedCode, context, ct);

            _logger.LogInformation("Handler {HandlerCode} completed successfully", resolvedCode);

            return result;
        }

        /// <summary>
        /// Restituisce la collezione di tutti gli handler disponibili nel sistema.
        /// </summary>
        /// <returns>Collezione read-only dei codici degli handler disponibili.</returns>
        public Task<IReadOnlyCollection<string>> GetAvailableHandlersAsync()
        {
            return Task.FromResult(_registry.Codes);
        }

        /// <summary>
        /// Restituisce gli handler filtrati per tipo.
        /// </summary>
        /// <param name="type">Tipo di handler da filtrare.</param>
        /// <returns>Collezione di handler del tipo specificato.</returns>
        public Task<IReadOnlyCollection<IUnifiedHandler>> GetHandlersByTypeAsync(HandlerType type)
        {
            return Task.FromResult(_registry.GetHandlersByType(type));
        }

        /// <summary>
        /// Verifica se un handler specifico è disponibile nel sistema.
        /// </summary>
        /// <param name="handlerCode">Codice identificativo dell'handler da verificare.</param>
        /// <returns>True se l'handler è disponibile, false altrimenti.</returns>
        public Task<bool> IsHandlerAvailableAsync(string handlerCode)
        {
            try
            {
                ResolveHandlerCode(handlerCode);
                return Task.FromResult(true);
            }
            catch (InvalidOperationException)
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Risolve il codice dell'handler tentando di trovare corrispondenze esatte o parziali.
        /// </summary>
        /// <param name="inputCode">Codice di input da risolvere.</param>
        /// <returns>Codice risolto dell'handler.</returns>
        /// <exception cref="InvalidOperationException">Se nessun handler corrisponde al codice fornito.</exception>
        private string ResolveHandlerCode(string inputCode)
        {
            _logger.LogDebug("Tentativo di risoluzione del codice handler: {InputCode}", inputCode);

            // 1. Prova prima con il codice esatto
            if (_registry.TryGetHandler(inputCode, out _))
            {
                _logger.LogDebug("Codice handler risolto esattamente: {InputCode}", inputCode);
                return inputCode;
            }

            // 2. Se non trovato, cerca pattern parziali
            var availableCodes = _registry.Codes.ToList();
            
            // Cerca handler che terminano con il codice fornito (es: "28_AUT" -> "Z0072370_28AUT")
            var partialMatch = availableCodes.FirstOrDefault(code => 
                code.EndsWith("_" + inputCode, StringComparison.OrdinalIgnoreCase) ||
                code.EndsWith(inputCode, StringComparison.OrdinalIgnoreCase));

            if (partialMatch != null)
            {
                _logger.LogInformation("Codice handler risolto tramite corrispondenza parziale: {InputCode} -> {ResolvedCode}", 
                    inputCode, partialMatch);
                return partialMatch;
            }

            // 3. Se ancora non trovato, lancia eccezione
            var errorMessage = $"Handler non trovato per il codice '{inputCode}'. " +
                              $"Codici disponibili: {string.Join(", ", availableCodes)}";
            
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }
}