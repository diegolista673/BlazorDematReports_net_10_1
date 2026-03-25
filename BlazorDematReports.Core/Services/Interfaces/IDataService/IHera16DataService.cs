using System.Data;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService;

/// <summary>
/// Contratto per l'inserimento bulk di righe CSV grezze nella tabella HERA16.
/// </summary>
public interface IHera16DataService
{
    /// <summary>
    /// Inserisce in bulk gli allegati CSV grezzi nella tabella HERA16 via SqlBulkCopy.
    /// Converte le colonne data da stringa (formato dd/MM/yyyy HH:mm:ss) a DateTime
    /// e aggiunge le colonne audit nome_file e data_caricamento_file.
    /// </summary>
    /// <param name="rawAttachments">Lista di (DataTable grezzo, nomeFile) da inserire.</param>
    /// <param name="ct">Token di cancellazione.</param>
    Task BulkInsertAsync(
        IReadOnlyList<(DataTable Data, string FileName)> rawAttachments,
        CancellationToken ct = default);


}
