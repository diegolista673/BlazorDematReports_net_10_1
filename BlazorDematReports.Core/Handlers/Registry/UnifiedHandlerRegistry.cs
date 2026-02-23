using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Wrappers;

namespace BlazorDematReports.Core.Handlers.Registry
{
    /// <summary>
    /// Registry unificato per la gestione di tutti gli handler del sistema.
    /// Pattern: Registry (GoF) — Fornisce lookup dinamico per codice senza hardcoding.
    /// Auto-discovery: Raccoglie IProductionDataHandler via DI e li wrappa automaticamente.
    /// </summary>
    public sealed class UnifiedHandlerRegistry : IUnifiedHandlerRegistry
    {
        private readonly Dictionary<string, IRegistrableHandler> _handlersMap;
        private readonly Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>> _executors;

        /// <summary>
        /// Costruttore: raccoglie tutti gli IProductionDataHandler dal DI e li wrappa in ProductionHandlerAdapter.
        /// </summary>
        public UnifiedHandlerRegistry(IEnumerable<IProductionDataHandler> productionHandlers)
        {
            _handlersMap = new Dictionary<string, IRegistrableHandler>();
            _executors = new Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>>();

            // DI risolve TUTTI gli IProductionDataHandler registrati e li passa come collection
            foreach (var handler in productionHandlers)
            {
                // Wrap ogni handler per uniformare la firma
                var adapter = new ProductionHandlerAdapter(handler);

                // Indicizza per codice (es. "Z0082041_SOFTLINE")
                _handlersMap[adapter.Code] = adapter;
                _executors[adapter.Code] = adapter.ExecuteAsync;
            }
        }

        public void Register(IRegistrableHandler handler)
        {
            _handlersMap[handler.Code] = handler;
            _executors[handler.Code] = handler.ExecuteAsync;
        }

        public IRegistrableHandler? Get(string code)
            => _handlersMap.TryGetValue(code, out var handler) ? handler : null;

        public bool IsRegistered(string code)
            => _handlersMap.ContainsKey(code);

        public async Task<object> ExecuteAsync(string code, UnifiedExecutionContext context, CancellationToken ct = default)
        {
            if (!_executors.TryGetValue(code, out var executor))
            {
                throw new InvalidOperationException($"Handler '{code}' non trovato.");
            }
            return await executor(context, ct);
        }

        public IEnumerable<string> GetAllCodes()
            => _handlersMap.Keys;

        /// <summary>Restituisce tutti gli handler registrati del tipo specificato.</summary>
        public IReadOnlyCollection<IRegistrableHandler> GetHandlersByType(HandlerType type)
            => _handlersMap.Values.Where(h => h.Type == type).ToList();
    }
}