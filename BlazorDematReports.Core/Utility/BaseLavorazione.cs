using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Classe base per i processor di lavorazione custom.
    /// Fornisce le proprietà comuni per i parametri di esecuzione senza dipendenze inutilizzate.
    /// </summary>
    public abstract class BaseLavorazione
    {
        public string? NomeProcedura { get; set; }
        public int IDFaseLavorazione { get; set; }
        public int IDProceduraLavorazione { get; set; }
        public int? IDCentro { get; set; }
        public DateTime StartDataLavorazione { get; set; }
        public DateTime? EndDataLavorazione { get; set; }

        /// <summary>
        /// Metodo da implementare per recuperare i dati di lavorazione dalla sorgente specifica.
        /// </summary>
        /// <param name="ct">Token di cancellazione per operazioni asincrone.</param>
        public abstract Task<List<DatiLavorazione>> SetDatiDematAsync(CancellationToken ct = default);
    }
}



