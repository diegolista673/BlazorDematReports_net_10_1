using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Lavorazioni.Interfaces
{
    /// <summary>
    /// Contratto per l'esecuzione delle lavorazioni.
    /// </summary>
    public interface ILavorazioneHandler
    {
        string LavorazioneCode { get; }
        Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default);
        string? GetServiceCode() => null;
        HandlerMetadata? GetMetadata() => null;
    }

    /// <summary>
    /// Tipo di handler nel sistema unificato.
    /// </summary>
    public enum HandlerType
    {
        Lavorazione,
        EmailService,
        Custom
    }

    /// <summary>
    /// Contratto base per tutti gli handler unificati.
    /// </summary>
    public interface IUnifiedHandler
    {
        string Code { get; }
        HandlerType Type { get; }
        string Description { get; }
        Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default);
    }

    /// <summary>
    /// Servizio per l'esecuzione di handler unificati.
    /// </summary>
    public interface IUnifiedHandlerService
    {
        void RegisterHandler(IUnifiedHandler handler);
        Task<object> ExecuteHandlerAsync(string handlerCode, UnifiedExecutionContext context, CancellationToken ct = default);
        bool IsHandlerRegistered(string handlerCode);
        IEnumerable<string> GetRegisteredHandlerCodes();
    }

    /// <summary>
    /// Registry per la gestione centralizzata di tutti gli handler.
    /// </summary>
    public interface IUnifiedHandlerRegistry
    {
        void Register(IUnifiedHandler handler);
        IUnifiedHandler? Get(string code);
        bool IsRegistered(string code);
        Task<object> ExecuteAsync(string code, UnifiedExecutionContext context, CancellationToken ct = default);
        IEnumerable<string> GetAllCodes();
    }
}
