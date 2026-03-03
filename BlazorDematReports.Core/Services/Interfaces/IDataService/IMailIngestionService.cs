using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService;

/// <summary>
/// Contratto per il servizio di gestione dati mail ingestion.
/// </summary>
public interface IMailIngestionService
{
    /// <summary>
    /// Inserisce o aggiorna record di ingestion (UPSERT basato su unique constraint).
    /// </summary>
    /// <param name="codiceServizio">Codice servizio (es. 'ADER4').</param>
    /// <param name="dataRiferimento">Data lavorazione riportata nell'email.</param>
    /// <param name="tipoDato">Tipo dato (es. 'ScansioneCaptiva').</param>
    /// <param name="centro">Centro (es. 'VERONA', NULL se aggregato).</param>
    /// <param name="quantita">Quantità aggregata.</param>
    /// <param name="metadataJson">Metadata opzionali in formato JSON.</param>
    Task<DatiMailIngestion> UpsertAsync(
        string codiceServizio,
        DateOnly dataRiferimento,
        string tipoDato,
        string? centro,
        int quantita,
        string? metadataJson = null);

    /// <summary>
    /// Recupera dati non ancora elaborati filtrati per servizio/tipo/centro.
    /// </summary>
    Task<List<DatiMailIngestion>> GetUnprocessedAsync(
        string codiceServizio,
        string tipoDato,
        string? centro = null,
        DateOnly? dataRiferimentoMin = null,
        DateOnly? dataRiferimentoMax = null);

    /// <summary>
    /// Marca record come elaborati aggiornando flag e timestamp.
    /// </summary>
    Task MarkAsProcessedAsync(List<int> ids, int elaborataDaTaskId);

    /// <summary>
    /// Elimina record elaborati più vecchi di una certa data (cleanup).
    /// </summary>
    Task<int> CleanupOldProcessedAsync(DateTime olderThan);
}
