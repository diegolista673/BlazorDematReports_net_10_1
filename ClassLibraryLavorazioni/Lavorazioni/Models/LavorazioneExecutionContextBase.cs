namespace LibraryLavorazioni.Lavorazioni.Models
{
    public sealed class LavorazioneExecutionContext
    {
        public string? NomeProcedura { get; init; }
        public int IDFaseLavorazione { get; init; }
        public int IDProceduraLavorazione { get; init; }
        public int? IDCentro { get; init; }
        public DateTime StartDataLavorazione { get; init; }
        public DateTime? EndDataLavorazione { get; init; }
        public required IServiceProvider ServiceProvider { get; init; }
        
        /// <summary>
        /// ID configurazione per sistema unificato fonti dati.
        /// NULL se usa sistema legacy (IdQuery o QueryIntegrata).
        /// </summary>
        public int? IdConfigurazioneDatabase { get; init; }
    }
}