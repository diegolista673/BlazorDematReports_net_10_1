using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface IElaboratoreDatiLavorazione
    {
        /// <summary>
        /// Elabora i dati di lavorazione originali, normalizzando gli operatori, raggruppando e aggregando i dati,
        /// e restituendo una lista di <see cref="DatiElaborati"/> pronti per la persistenza o l'analisi.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        public Task<List<DatiElaborati>> ElaboraDatiLavorazioneAsync(
                    List<DatiLavorazione> datiOriginali,
                    int idCentro,
                    int idProceduraLavorazione,
                    int idFaseLavorazione,
                    CancellationToken ct = default);

    }
}