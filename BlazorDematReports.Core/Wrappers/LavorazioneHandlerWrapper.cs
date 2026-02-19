using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Wrappers
{
    /// <summary>
    /// Wrapper per integrare gli handler di lavorazione nel sistema unificato.
    /// </summary>
    public sealed class LavorazioneHandlerWrapper : IUnifiedHandler
    {
        private readonly ILavorazioneHandler _handler;

        public LavorazioneHandlerWrapper(ILavorazioneHandler handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public string Code => _handler.LavorazioneCode;

        /// <inheritdoc />
        public HandlerType Type => HandlerType.Lavorazione;

        /// <inheritdoc />
        public string Description => $"Lavorazione: {_handler.LavorazioneCode}";

        /// <summary>
        /// Esegue l'handler di lavorazione.
        /// </summary>
        public async Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default)
        {
            var lavorazioneContext = context.ToLavorazioneContext();
            var result = await _handler.ExecuteAsync(lavorazioneContext, ct);
            return result ?? new List<DatiLavorazione>();
        }
    }
}