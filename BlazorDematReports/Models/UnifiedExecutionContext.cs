using BlazorDematReports.Core.Models;

namespace LibraryLavorazioni.Shared.Models
{
    /// <summary>
    /// Contesto di esecuzione unificato per tutti i tipi di handler.
    /// </summary>
    public sealed class UnifiedExecutionContext
    {
        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int IDProceduraLavorazione { get; init; }

        /// <summary>
        /// Service provider per dependency injection.
        /// </summary>
        public required IServiceProvider ServiceProvider { get; init; }

        /// <summary>
        /// Codice dell'handler da eseguire.
        /// </summary>
        public string HandlerCode { get; init; } = string.Empty;

        /// <summary>
        /// Parametri specifici per il contesto (opzionali).
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = new();

        /// <summary>
        /// Converte il contesto unificato in un contesto specifico per lavorazioni.
        /// </summary>
        public LavorazioneExecutionContext ToLavorazioneContext()
        {
            return new LavorazioneExecutionContext
            {
                IDProceduraLavorazione = IDProceduraLavorazione,
                IDFaseLavorazione = Parameters.GetValueOrDefault("IDFaseLavorazione", 0) as int? ?? 0,
                IDCentro = (int)Parameters.GetValueOrDefault("IDCentro")!,
                NomeProcedura = Parameters.GetValueOrDefault("NomeProcedura") as string,
                StartDataLavorazione = Parameters.GetValueOrDefault("StartDataLavorazione", DateTime.Now) as DateTime? ?? DateTime.Now,
                EndDataLavorazione = Parameters.GetValueOrDefault("EndDataLavorazione") as DateTime?,
                ServiceProvider = ServiceProvider
            };
        }
    }
}