using LibraryLavorazioni.Lavorazioni.Interfaces;
using LibraryLavorazioni.LavorazioniViaMail.Interfaces;
using LibraryLavorazioni.Shared.Interfaces;
using LibraryLavorazioni.Shared.Models;
using LibraryLavorazioni.Shared.Wrappers;

namespace LibraryLavorazioni.Shared.Registry
{
    /// <summary>
    /// Registry unificato per la gestione di tutti gli handler del sistema.
    /// Supporta sia lavorazioni che importazioni mail con un'interfaccia comune.
    /// </summary>
    public sealed class UnifiedHandlerRegistry : IUnifiedHandlerRegistry
    {
        private readonly Dictionary<string, IUnifiedHandler> _handlersMap;
        private readonly Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>> _executors;

        /// <summary>
        /// Inizializza il registry con le collezioni di handler fornite.
        /// </summary>
        /// <param name="lavorazioneHandlers">Collezione di handler per lavorazioni.</param>
        /// <param name="mailImportHandlers">Collezione di handler per mail import.</param>
        public UnifiedHandlerRegistry(
            IEnumerable<ILavorazioneHandler> lavorazioneHandlers,
            IEnumerable<IMailImportHandler> mailImportHandlers)
        {
            _handlersMap = new Dictionary<string, IUnifiedHandler>();
            _executors = new Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>>();

            // Registra handler di lavorazione
            foreach (var handler in lavorazioneHandlers)
            {
                var wrapper = new LavorazioneHandlerWrapper(handler);
                _handlersMap[wrapper.Code] = wrapper;
                _executors[wrapper.Code] = wrapper.ExecuteAsync;
            }

            // Registra handler mail
            foreach (var handler in mailImportHandlers)
            {
                var wrapper = new MailImportHandlerWrapper(handler);
                _handlersMap[wrapper.Code] = wrapper;
                _executors[wrapper.Code] = wrapper.ExecuteAsync;
            }
        }

        /// <inheritdoc />
        public IUnifiedHandler Get(string code)
            => _handlersMap.TryGetValue(code, out var handler)
                ? handler
                : throw new InvalidOperationException($"Handler non trovato per il codice '{code}'. " +
                                                    $"Codici disponibili: {string.Join(", ", _handlersMap.Keys)}");

        /// <inheritdoc />
        public async Task<object> ExecuteAsync(string code, UnifiedExecutionContext context, CancellationToken ct = default)
        {
            if (!_executors.TryGetValue(code, out var executor))
            {
                throw new InvalidOperationException($"Executor non trovato per il codice '{code}'.");
            }

            return await executor(context, ct);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<string> Codes => _handlersMap.Keys.ToList();

        /// <inheritdoc />
        public IReadOnlyCollection<IUnifiedHandler> GetHandlersByType(HandlerType type)
            => _handlersMap.Values.Where(h => h.Type == type).ToList();

        /// <inheritdoc />
        public bool TryGetHandler(string code, out IUnifiedHandler? handler)
            => _handlersMap.TryGetValue(code, out handler);
    }
}