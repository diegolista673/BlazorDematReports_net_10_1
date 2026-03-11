using BlazorDematReports.Core.Application;
using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Helpers;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Services.DataService;

/// <summary>
/// Implementazione dello staging per-operatore da CSV email.
/// Gestisce la tabella DatiMailCsv per ADER4, HERA16 e futuri servizi.
/// </summary>
public sealed class MailCsvService : ServiceBase<DatiMailCsv>, IMailCsvService
{
    public MailCsvService(
        ConfigUser configUser,
        IDbContextFactory<DematReportsContext> contextFactory,
        ILogger<MailCsvService> logger)
        : base(contextFactory, logger, configUser)
    {
    }

    /// <inheritdoc />
    public async Task UpsertBulkAsync(
        IReadOnlyList<DatiMailCsvDto> righe,
        CancellationToken ct = default)
    {
        if (righe.Count == 0)
            return;

        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // Carica tutte le righe esistenti in una singola query per evitare N+1
        // e per permettere la lookup in memoria durante il loop (FirstOrDefaultAsync
        // non vede le entita' appena aggiunte al tracker nella stessa sessione).
        var codici   = righe.Select(r => r.CodiceServizio).Distinct().ToList();
        var dateMin  = righe.Min(r => r.DataLavorazione);
        var dateMax  = righe.Max(r => r.DataLavorazione);

        var esistenti = await context.DatiMailCsvs
            .Where(d => codici.Contains(d.CodiceServizio)
                     && d.DataLavorazione >= dateMin
                     && d.DataLavorazione <= dateMax)
            .ToListAsync(ct);

        // Dizionario chiave → entity per lookup O(1)
        var dict = esistenti.ToDictionary(d =>
            (d.CodiceServizio, d.DataLavorazione, d.Operatore, d.TipoRisultato,
             d.IdEvento ?? string.Empty, d.Centro ?? string.Empty));

        int inseriti = 0, aggiornati = 0;

        foreach (var dto in righe)
        {
            var key = (dto.CodiceServizio, dto.DataLavorazione, dto.Operatore, dto.TipoRisultato,
                       dto.IdEvento ?? string.Empty, dto.Centro ?? string.Empty);

            if (dict.TryGetValue(key, out var existing))
            {
                existing.Documenti      = dto.Documenti;
                existing.DataIngestione = DateTime.Now;
                aggiornati++;
            }
            else
            {
                var nuova = new DatiMailCsv
                {
                    CodiceServizio  = dto.CodiceServizio,
                    DataLavorazione = dto.DataLavorazione,
                    Operatore       = dto.Operatore,
                    TipoRisultato   = dto.TipoRisultato,
                    Documenti       = dto.Documenti,
                    IdEvento        = dto.IdEvento,
                    Centro          = dto.Centro,
                    DataIngestione  = DateTime.Now
                };
                context.DatiMailCsvs.Add(nuova);
                // Aggiunge al dizionario per gestire duplicati all'interno dello stesso batch
                dict[key] = nuova;
                inseriti++;
            }
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "UpsertBulk DatiMailCsv: {Inseriti} inseriti, {Aggiornati} aggiornati ({Totale} righe)",
            inseriti, aggiornati, righe.Count);
    }

    /// <inheritdoc />
    public async Task<List<DatiMailCsv>> GetUnprocessedAsync(
        string codiceServizio,
        string tipoRisultato,
        DateOnly dataMin,
        DateOnly dataMax,
        string? centro = null,
        CancellationToken ct = default)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var query = context.DatiMailCsvs.Where(d =>
            d.CodiceServizio  == codiceServizio  &&
            d.TipoRisultato   == tipoRisultato   &&
            d.DataLavorazione >= dataMin         &&
            d.DataLavorazione <= dataMax);

        if (centro is not null)
            query = query.Where(d => d.Centro == centro);

        return await query
            .OrderBy(d => d.DataLavorazione)
            .ThenBy(d => d.Operatore)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(
        IReadOnlyList<int> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return;

        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var now = DateTime.Now;
        await context.DatiMailCsvs
            .Where(d => ids.Contains(d.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.ElaborataIl, now),
                ct);

        logger.LogInformation(
            "Aggiornato ElaborataIl su {Count} record DatiMailCsv",
            ids.Count);
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldProcessedAsync(DateTime olderThan, CancellationToken ct = default)
    {
        QueryLoggingHelper.LogQueryExecution(logger);

        await using var context = await contextFactory.CreateDbContextAsync(ct);

        int deleted = await context.DatiMailCsvs
            .Where(d => d.DataLavorazione < DateOnly.FromDateTime(olderThan))
            .ExecuteDeleteAsync(ct);

        logger.LogInformation("Eliminati {Count} record DatiMailCsv piu vecchi di {Date}", deleted, olderThan);
        return deleted;
    }
}
