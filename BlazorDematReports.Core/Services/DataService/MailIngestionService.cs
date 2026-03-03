using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService;

/// <summary>
/// Servizio per la gestione dello staging dati mail ingestion.
/// </summary>
public class MailIngestionService : ServiceBase<DatiMailIngestion>, IMailIngestionService
{
    public MailIngestionService(
        ConfigUser configUser,
        IDbContextFactory<DematReportsContext> contextFactory,
        ILogger<MailIngestionService> logger)
        : base(contextFactory, logger, configUser)
    {
    }

    /// <summary>
    /// Inserisce o aggiorna record di ingestion.
    /// Se esiste già un record con (CodiceServizio, DataRiferimento, TipoDato, Centro) uguale,
    /// aggiorna Quantita e MetadataJson. Altrimenti crea nuovo record.
    /// </summary>
    public async Task<DatiMailIngestion> UpsertAsync(
        string codiceServizio,
        DateOnly dataRiferimento,
        string tipoDato,
        string? centro,
        int quantita,
        string? metadataJson = null)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync();

        var existing = await context.DatiMailIngestions
            .FirstOrDefaultAsync(d =>
                d.CodiceServizio == codiceServizio &&
                d.DataRiferimento == dataRiferimento &&
                d.TipoDato == tipoDato &&
                d.Centro == centro);

        if (existing != null)
        {
            existing.Quantita = quantita;
            existing.MetadataJson = metadataJson;
            existing.DataIngestione = DateTime.UtcNow;
            logger.LogInformation(
                "Aggiornato record ingestion esistente: {Codice}/{Tipo}/{Centro}/{Data} - Quantita={Quantita}",
                codiceServizio, tipoDato, centro ?? "NULL", dataRiferimento, quantita);
        }
        else
        {
            existing = new DatiMailIngestion
            {
                CodiceServizio = codiceServizio,
                DataRiferimento = dataRiferimento,
                TipoDato = tipoDato,
                Centro = centro,
                Quantita = quantita,
                DataIngestione = DateTime.UtcNow,
                Elaborata = false,
                MetadataJson = metadataJson
            };
            context.DatiMailIngestions.Add(existing);
            logger.LogInformation(
                "Creato nuovo record ingestion: {Codice}/{Tipo}/{Centro}/{Data} - Quantita={Quantita}",
                codiceServizio, tipoDato, centro ?? "NULL", dataRiferimento, quantita);
        }

        await context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Recupera dati non ancora elaborati (Elaborata=false) filtrati per servizio/tipo/centro/data.
    /// </summary>
    public async Task<List<DatiMailIngestion>> GetUnprocessedAsync(
        string codiceServizio,
        string tipoDato,
        string? centro = null,
        DateOnly? dataRiferimentoMin = null,
        DateOnly? dataRiferimentoMax = null)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync();

        var query = context.DatiMailIngestions
            .Where(d => !d.Elaborata)
            .Where(d => d.CodiceServizio == codiceServizio)
            .Where(d => d.TipoDato == tipoDato);

        if (centro != null)
            query = query.Where(d => d.Centro == centro);

        if (dataRiferimentoMin.HasValue)
            query = query.Where(d => d.DataRiferimento >= dataRiferimentoMin.Value);

        if (dataRiferimentoMax.HasValue)
            query = query.Where(d => d.DataRiferimento <= dataRiferimentoMax.Value);

        return await query.OrderBy(d => d.DataRiferimento).ToListAsync();
    }

    /// <summary>
    /// Marca record come elaborati.
    /// </summary>
    public async Task MarkAsProcessedAsync(List<int> ids, int elaborataDaTaskId)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync();

        await context.DatiMailIngestions
            .Where(d => ids.Contains(d.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Elaborata, true)
                .SetProperty(d => d.ElaborataIl, DateTime.UtcNow)
                .SetProperty(d => d.ElaborataDaTaskId, elaborataDaTaskId));

        logger.LogInformation(
            "Marcati come elaborati {Count} record staging (task {TaskId})",
            ids.Count, elaborataDaTaskId);
    }

    /// <summary>
    /// Elimina record elaborati più vecchi della data specificata (cleanup).
    /// </summary>
    public async Task<int> CleanupOldProcessedAsync(DateTime olderThan)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync();

        var deleted = await context.DatiMailIngestions
            .Where(d => d.Elaborata && d.ElaborataIl < olderThan)
            .ExecuteDeleteAsync();

        logger.LogInformation("Cleanup staging: eliminati {Count} record elaborati prima di {Data}", deleted, olderThan);
        return deleted;
    }
}
