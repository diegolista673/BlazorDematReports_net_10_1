using BlazorDematReports.Core.Services.Interfaces.IDataService;
using Entities.Models.DbApplication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;

namespace BlazorDematReports.Core.Services.DataService;

/// <summary>
/// Inserisce le righe CSV grezze nella tabella HERA16 tramite EF Core.
/// Il DataTable di input proviene dalla lettura del CSV allegato all'email;
/// tutti i valori sono stringhe e vengono convertiti nei tipi corretti prima dell'insert.
/// </summary>
public sealed class Hera16DataService : IHera16DataService
{
    private readonly IDbContextFactory<DematReportsContext> _contextFactory;
    private readonly ILogger<Hera16DataService> _logger;

    /// <summary>
    /// Inizializza il servizio insert HERA16.
    /// </summary>
    public Hera16DataService(
        IDbContextFactory<DematReportsContext> contextFactory,
        ILogger<Hera16DataService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task BulkInsertAsync(
        IReadOnlyList<(DataTable Data, string FileName)> rawAttachments,
        CancellationToken ct = default)
    {
        if (rawAttachments.Count == 0)
            return;

        foreach (var (rawData, fileName) in rawAttachments)
        {
            ct.ThrowIfCancellationRequested();

            if (rawData.Rows.Count == 0)
            {
                _logger.LogWarning("HERA16 insert: allegato {File} vuoto, ignorato", fileName);
                continue;
            }

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            // Delete-then-reinsert per NomeFile: rende l'operazione idempotente su re-run o riletture mail.
            // Delete + AddRange vengono salvati in un'unica SaveChangesAsync (singola transazione EF Core).
            var existing = await context.DatiMailCsvHera16
                .Where(h => h.NomeFile == fileName)
                .ToListAsync(ct);

            if (existing.Count > 0)
            {
                context.DatiMailCsvHera16.RemoveRange(existing);
                _logger.LogInformation(
                    "HERA16 insert: rimossi {Count} record esistenti per '{File}' (idempotenza re-run)",
                    existing.Count, fileName);
            }

            var entities = MapToEntities(rawData, fileName, DateTime.Now);
            await context.DatiMailCsvHera16.AddRangeAsync(entities, ct);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "HERA16 insert completato: {Righe} righe inserite per '{File}'",
                entities.Count, fileName);
        }
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(
        IReadOnlyList<int> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var now = DateTime.Now;
        await context.DatiMailCsvHera16
            .Where(h => ids.Contains(h.IdCounter))
            .ExecuteUpdateAsync(s => s
                .SetProperty(h => h.ElaboratoIl, now),
                ct);

        _logger.LogInformation(
            "HERA16: aggiornato ElaboratoIl su {Count} record",
            ids.Count);
    }

    /// <summary>
    /// Converte il DataTable grezzo (tutto stringhe dal CSV) in una lista di entità <see cref="DatiMailCsvHera16"/>.
    /// Aggiunge le colonne audit <c>NomeFile</c> e <c>DataCaricamentoFile</c>.
    /// Il segnaposto '-' nelle colonne data viene trattato come null.
    /// </summary>
    private static List<DatiMailCsvHera16> MapToEntities(DataTable source, string nomeFile, DateTime dataCaricamento)
    {
        var result = new List<DatiMailCsvHera16>(source.Rows.Count);

        foreach (DataRow src in source.Rows)
        {
            result.Add(new DatiMailCsvHera16
            {
                CodiceMercato            = GetString(src, "codice_mercato"),
                CodiceOfferta            = GetString(src, "codice_offerta"),
                TipoDocumento            = GetString(src, "tipo_documento"),
                DataScansione            = GetDateTime(src, "data_scansione"),
                OperatoreScan            = GetString(src, "operatore_scan"),
                DataClassificazione      = GetDateTime(src, "data_classificazione"),
                OperatoreClassificazione = GetString(src, "operatore_classificazione"),
                DataIndex                = GetDateTime(src, "data_index"),
                OperatoreIndex           = GetString(src, "operatore_index"),
                DataPubblicazione        = GetDateTime(src, "data_pubblicazione"),
                CodiceScatola            = GetString(src, "codice_scatola"),
                ProgrScansione           = GetString(src, "progr_scansione"),
                NomeFile                 = nomeFile,
                DataCaricamentoFile      = dataCaricamento,
                IdentificativoAllegato   = GetInt(src, "identificativo_allegato"),
            });
        }

        return result;
    }

    /// <summary>Restituisce il valore stringa della colonna o null se assente/vuoto/'-'.</summary>
    private static string? GetString(DataRow row, string col)
    {
        if (!row.Table.Columns.Contains(col))
            return null;

        var val = row[col]?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(val) || val == "-" ? null : val;
    }

    /// <summary>
    /// Converte la stringa della colonna data in <see cref="DateTime"/>
    /// oppure restituisce null se il valore non è parsabile o è '-'.
    /// </summary>
    private static DateTime? GetDateTime(DataRow row, string col)
    {
        if (!row.Table.Columns.Contains(col))
            return null;

        var val = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(val) || val == "-")
            return null;

        if (DateTime.TryParseExact(val, "dd/MM/yyyy HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse(val, CultureInfo.InvariantCulture, out var dt2))
            return dt2;

        return null;
    }

    /// <summary>Converte la stringa della colonna in <see cref="int"/> oppure null se non parsabile.</summary>
    private static int? GetInt(DataRow row, string col)
    {
        if (!row.Table.Columns.Contains(col))
            return null;

        var val = row[col]?.ToString()?.Trim();
        return int.TryParse(val, out var result) ? result : null;
    }
}
