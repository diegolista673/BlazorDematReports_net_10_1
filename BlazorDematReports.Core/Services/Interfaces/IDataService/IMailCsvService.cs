using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService;

/// <summary>
/// Contratto per lo staging per-operatore da CSV email.
/// Tabella unificata DatiMailCsv per ADER4, HERA16 e futuri servizi.
/// </summary>
public interface IMailCsvService
{
    /// <summary>
    /// Inserisce o aggiorna in bulk le righe per-operatore estratte dai CSV.
    /// Chiave unica: (CodiceServizio, DataLavorazione, Operatore, TipoRisultato, IdEvento, Centro).
    /// </summary>
    Task UpsertBulkAsync(
        IReadOnlyList<DatiMailCsvDto> righe,
        CancellationToken ct = default);

    /// <summary>
    /// Recupera record non ancora elaborati per un handler produzione.
    /// </summary>
    Task<List<DatiMailCsv>> GetUnprocessedAsync(
        string codiceServizio,
        string tipoRisultato,
        DateOnly dataMin,
        DateOnly dataMax,
        string? centro = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marca i record come elaborati dopo l'INSERT in ProduzioneSistema.
    /// </summary>
    Task MarkAsProcessedAsync(
        IReadOnlyList<int> ids,
        int taskId,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina record elaborati piu vecchi della data indicata.
    /// </summary>
    Task<int> CleanupOldProcessedAsync(DateTime olderThan, CancellationToken ct = default);
}

/// <summary>
/// DTO per inserimento bulk in DatiMailCsv.
/// Una istanza per ogni coppia (Operatore, TipoRisultato) estratta dal CSV.
/// </summary>
public sealed record DatiMailCsvDto(
    string CodiceServizio,
    DateOnly DataLavorazione,
    string Operatore,
    string TipoRisultato,
    int Documenti,
    string? IdEvento = null,
    string? Centro = null);
