using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Wrappers;

namespace BlazorDematReports.Core.Handlers.Registry
{
    /// <summary>
    /// Registry unificato per la gestione di tutti gli handler del sistema.
    /// </summary>
    public sealed class UnifiedHandlerRegistry : IUnifiedHandlerRegistry
    {
        private readonly Dictionary<string, IUnifiedHandler> _handlersMap;
        private readonly Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>> _executors;

        public UnifiedHandlerRegistry(IEnumerable<ILavorazioneHandler> lavorazioneHandlers)
        {
            _handlersMap = new Dictionary<string, IUnifiedHandler>();
            _executors = new Dictionary<string, Func<UnifiedExecutionContext, CancellationToken, Task<object>>>();

            foreach (var handler in lavorazioneHandlers)
            {
                var wrapper = new LavorazioneHandlerWrapper(handler);
                _handlersMap[wrapper.Code] = wrapper;
                _executors[wrapper.Code] = wrapper.ExecuteAsync;
            }
        }

        public void Register(IUnifiedHandler handler)
        {
            _handlersMap[handler.Code] = handler;
            _executors[handler.Code] = handler.ExecuteAsync;
        }

        public IUnifiedHandler? Get(string code)
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

        public IReadOnlyCollection<HandlerType> GetHandlersByType(HandlerType type)
            => _handlersMap.Values.Where(h => h.Type == type).Select(h => h.Type).ToList();
    }
}