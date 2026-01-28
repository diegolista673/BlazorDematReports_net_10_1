using LibraryLavorazioni.Lavorazioni.Models;
using LibraryLavorazioni.Utility.Models;

namespace LibraryLavorazioni.Lavorazioni.Interfaces
{
    /// <summary>
    /// Contratto per l'esecuzione delle lavorazioni.
    /// </summary>
    public interface ILavorazioneHandler
    {
        /// <summary>
        /// Codice identificativo univoco della lavorazione.
        /// </summary>
        string LavorazioneCode { get; }

        /// <summary>
        /// Esegue la lavorazione specificata.
        /// </summary>
        Task<List<DatiLavorazione>> ExecuteAsync(LavorazioneExecutionContext context, CancellationToken ct = default);
    }


}