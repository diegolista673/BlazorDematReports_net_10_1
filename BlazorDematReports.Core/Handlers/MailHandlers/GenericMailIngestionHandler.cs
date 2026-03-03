using BlazorDematReports.Core.Lavorazioni.Interfaces;
using BlazorDematReports.Core.Lavorazioni.Models;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using BlazorDematReports.Core.Utility.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace BlazorDematReports.Core.Handlers.MailHandlers;

/// <summary>
/// Handler generico per l'ingestion di tutte le email configurate nel sistema.
/// Chiamato da un singolo recurring job Hangfire (es. ore 07:00).
/// Orchestra tutti i servizi email registrati (ADER4, HERA16, ...) e salva i dati
/// aggregati nella tabella DatiMailIngestion.
/// I task produzione fase-specifici leggeranno poi da quella tabella.
/// </summary>
[Description("Ingestion generica email - salva dati aggregati in staging")]
public sealed class GenericMailIngestionHandler : IProductionDataHandler
{
    private readonly ILogger<GenericMailIngestionHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IMailIngestionProcessor> _mailProcessors;

    public GenericMailIngestionHandler(
        ILogger<GenericMailIngestionHandler> logger,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IMailIngestionProcessor> mailProcessors)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _mailProcessors = mailProcessors;
    }

    /// <inheritdoc />
    public string HandlerCode => "MAIL_INGESTION";

    /// <inheritdoc />
    public string? GetServiceCode() => null;

    /// <inheritdoc />
    public HandlerMetadata GetMetadata() => new()
    {
        ServiceCode = "MAIL_INGESTION",
        RequiresEmailService = true,
        Category = "Mail Ingestion"
    };

    /// <summary>
    /// Esegue l'ingestion di tutte le mail configurate.
    /// Ogni IMailIngestionProcessor (ADER4, HERA16, ...) viene chiamato in sequenza.
    /// Ritorna lista vuota perché questo handler non produce direttamente DatiLavorazione.
    /// </summary>
    public async Task<List<DatiLavorazione>> ExecuteAsync(
        ProductionExecutionContext context,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Inizio ingestion mail generica - Processori registrati: {Count}", _mailProcessors.Count());

        int totalProcessed = 0;
        int totalErrors = 0;

        using var scope = _scopeFactory.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IMailIngestionService>();

        foreach (var processor in _mailProcessors)
        {
            try
            {
                _logger.LogInformation("Avvio processore: {Code}", processor.ServiceCode);

                var result = await processor.ProcessAndSaveAsync(ingestionService, ct);

                totalProcessed += result.RecordsSaved;
                _logger.LogInformation(
                    "Processore {Code} completato: {Saved} record salvati",
                    processor.ServiceCode, result.RecordsSaved);
            }
            catch (Exception ex)
            {
                totalErrors++;
                _logger.LogError(ex, "Errore processore {Code}", processor.ServiceCode);
            }
        }

        _logger.LogInformation(
            "Ingestion mail completata: {Total} record salvati, {Errors} errori",
            totalProcessed, totalErrors);

        return new List<DatiLavorazione>();
    }
}

/// <summary>
/// Contratto per processori mail specifici per servizio (ADER4, HERA16).
/// Ogni implementazione sa come leggere le proprie email e salvare in staging.
/// </summary>
public interface IMailIngestionProcessor
{
    /// <summary>
    /// Codice servizio (es. 'ADER4', 'HERA16').
    /// </summary>
    string ServiceCode { get; }

    /// <summary>
    /// Legge email del servizio e salva dati in staging tramite IMailIngestionService.
    /// </summary>
    /// <param name="ingestionService">Servizio per salvare in DatiMailIngestion.</param>
    /// <param name="ct">Token cancellazione.</param>
    /// <returns>Risultato con numero record salvati.</returns>
    Task<IngestionResult> ProcessAndSaveAsync(IMailIngestionService ingestionService, CancellationToken ct);
}

/// <summary>
/// Risultato operazione ingestion.
/// </summary>
public sealed class IngestionResult
{
    public int RecordsSaved { get; init; }
    public int EmailsProcessed { get; init; }
    public List<string> Errors { get; init; } = new();
}
