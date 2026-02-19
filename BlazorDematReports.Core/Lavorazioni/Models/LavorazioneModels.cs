namespace BlazorDematReports.Core.Lavorazioni.Models
{
    /// <summary>
    /// Contesto di esecuzione per le lavorazioni.
    /// </summary>
    public sealed class LavorazioneExecutionContext
    {
        public string? NomeProcedura { get; init; }
        public int IDFaseLavorazione { get; init; }
        public int IDProceduraLavorazione { get; init; }
        public int IDCentro { get; init; }
        public DateTime StartDataLavorazione { get; init; }
        public DateTime? EndDataLavorazione { get; init; }
        public int? IdConfigurazioneDatabase { get; init; }
        public required IServiceProvider ServiceProvider { get; init; }
    }

    /// <summary>
    /// Contesto di esecuzione unificato per tutti i tipi di handler.
    /// </summary>
    public sealed class UnifiedExecutionContext
    {
        public int IDProceduraLavorazione { get; init; }
        public required IServiceProvider ServiceProvider { get; init; }
        public string HandlerCode { get; init; } = string.Empty;
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
                IdConfigurazioneDatabase = Parameters.GetValueOrDefault("IdConfigurazioneDatabase") as int?,
                ServiceProvider = ServiceProvider
            };
        }
    }

    /// <summary>
    /// Metadata descrittivi per un handler di lavorazione.
    /// </summary>
    public sealed class HandlerMetadata
    {
        public required string ServiceCode { get; init; }
        public bool RequiresEmailService { get; init; }
        public string? Category { get; init; }
        public string? Description { get; init; }
    }
}
