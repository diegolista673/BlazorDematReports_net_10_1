using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Wrappers
{
    /// <summary>
    /// Adapter che integra handler di produzione (IProductionDataHandler) nel registry unificato.
    /// Converte tra context specifico/generico e return type specifico/object.
    /// Pattern: Adapter (GoF) — Adatta IProductionDataHandler a IRegistrableHandler.
    /// </summary>
    public sealed class ProductionHandlerAdapter : IRegistrableHandler
    {
        private readonly IProductionDataHandler _handler;

        public ProductionHandlerAdapter(IProductionDataHandler handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public string Code => _handler.HandlerCode;

        /// <inheritdoc />
        public HandlerType Type => HandlerType.Lavorazione;

        /// <inheritdoc />
        public string Description => $"Production Handler: {_handler.HandlerCode}";

        /// <summary>
        /// Esegue l'handler di produzione adattando context e return type per il registry.
        /// </summary>
        public async Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default)
        {
            var productionContext = context.ToProductionContext();
            var result = await _handler.ExecuteAsync(productionContext, ct);
            return result ?? new List<DatiLavorazione>();
        }
    }
}