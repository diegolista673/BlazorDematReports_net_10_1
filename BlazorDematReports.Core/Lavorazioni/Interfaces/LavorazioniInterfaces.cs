using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Lavorazioni.Interfaces
{
    /// <summary>
    /// Contratto per handler che producono dati di lavorazione da sorgenti SQL/Oracle/Email.
    /// DOMAIN LAYER: Implementata da handler concrete type-safe.
    /// Restituisce List&lt;DatiLavorazione&gt; 
    /// </summary>
    public interface IProductionDataHandler
    {
        /// <summary>Codice identificativo univoco dell'handler (es. "Z0082041_SOFTLINE", "PRATICHE_SUCCESSIONE").</summary>
        string HandlerCode { get; }

        /// <summary>Esegue l'handler e restituisce dati di lavorazione elaborati.</summary>
        Task<List<DatiLavorazione>> ExecuteAsync(ProductionExecutionContext context, CancellationToken ct = default);

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
    /// Contratto per entry nel registry unificato (adapter per storage generico).
    /// INFRASTRUCTURE LAYER: Implementata da adapter/wrapper.
    /// Restituisce object per supportare handler con return type diversi.
    /// </summary>
    public interface IRegistrableHandler
    {
        /// <summary>Codice identificativo univoco per lookup nel registry.</summary>
        string Code { get; }

        /// <summary>Tipo di handler (Lavorazione, EmailService, Custom).</summary>
        HandlerType Type { get; }

        /// <summary>Descrizione testuale dell'handler.</summary>
        string Description { get; }

        /// <summary>Esegue l'handler con context generico e restituisce risultato come object.</summary>
        Task<object> ExecuteAsync(UnifiedExecutionContext context, CancellationToken ct = default);
    }

    /// <summary>
    /// Servizio per l'esecuzione di handler unificati.
    /// Facade che nasconde complessità del registry fornendo API semplificata.
    /// </summary>
    public interface IUnifiedHandlerService
    {
        void RegisterHandler(IRegistrableHandler handler);
        Task<object> ExecuteHandlerAsync(string handlerCode, UnifiedExecutionContext context, CancellationToken ct = default);
        bool IsHandlerRegistered(string handlerCode);
        IEnumerable<string> GetRegisteredHandlerCodes();
    }

    /// <summary>
    /// Registry per la gestione centralizzata di tutti gli handler.
    /// Fornisce lookup dinamico e esecuzione by-code senza hardcoding.
    /// </summary>
    public interface IUnifiedHandlerRegistry
    {
        /// <summary>Registra un handler nel registry manualmente.</summary>
        void Register(IRegistrableHandler handler);

        /// <summary>Recupera handler per codice (null se non trovato).</summary>
        IRegistrableHandler? Get(string code);

        /// <summary>Verifica se un handler è registrato.</summary>
        bool IsRegistered(string code);

        /// <summary>Esegue handler per codice con context generico.</summary>
        Task<object> ExecuteAsync(string code, UnifiedExecutionContext context, CancellationToken ct = default);

        /// <summary>Restituisce tutti i codici handler registrati.</summary>
        IEnumerable<string> GetAllCodes();

        /// <summary>Restituisce tutti gli handler registrati di un determinato tipo.</summary>
        IReadOnlyCollection<IRegistrableHandler> GetHandlersByType(HandlerType type);
    }
}
