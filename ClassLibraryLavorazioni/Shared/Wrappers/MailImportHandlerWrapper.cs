using LibraryLavorazioni.LavorazioniViaMail.Interfaces;
using LibraryLavorazioni.Shared.Interfaces;
using LibraryLavorazioni.Shared.Models;

namespace LibraryLavorazioni.Shared.Wrappers
{
    /// <summary>
    /// Wrapper per integrare i handler mail nel sistema unificato.
    /// </summary>
    public sealed class MailImportHandlerWrapper : IUnifiedHandler
    {
        private readonly IMailImportHandler _handler;

        public MailImportHandlerWrapper(IMailImportHandler handler)
        {
            _handler = handler;
        }

        /// <inheritdoc />
        public string Code => _handler.ServiceCode;

        /// <inheritdoc />
        public HandlerType Type => HandlerType.MailImport;

        /// <inheritdoc />
        public string Description => $"Mail Import: {_handler.ServiceCode}";

        /// <summary>
        /// Esegue l'handler mail.
        /// </summary>
        public async Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default)
        {
            var mailContext = context.ToMailImportContext();
            var result = await _handler.ExecuteAsync(context.ServiceProvider, mailContext, ct);
            return result;
        }
    }
}