namespace BlazorDematReports.Core.Constants
{
    /// <summary>
    /// Costanti centralizzate per le espressioni CRON predefinite.
    /// Single source of truth per label e valori usati in tutto il sistema.
    /// </summary>
    public static class CronPresets
    {
        /// <summary>
        /// Lista completa dei preset CRON disponibili (valore + etichetta leggibile).
        /// </summary>
        public static readonly IReadOnlyList<(string Value, string Label)> All =
        [
            ("0 5 * * *",   "Giornaliero 05:00"),
            ("0 2 * * *",   "Giornaliero 02:00"),
            ("0 */4 * * *", "Ogni 4 ore"),
            ("0 * * * *",   "Ogni ora"),
            ("0 0 1 * *",   "Mensile (1° giorno)"),
            ("0 0 * * 0",   "Settimanale (Domenica)"),
            ("0 0 * * 1",   "Settimanale (Lunedì)"),
        ];

        /// <summary>
        /// Restituisce l'etichetta leggibile per il valore CRON specificato.
        /// Se il valore non corrisponde a un preset, restituisce il valore raw.
        /// </summary>
        /// <param name="cron">Espressione CRON da tradurre.</param>
        /// <returns>Etichetta descrittiva o il valore raw se non trovato.</returns>
        public static string GetLabel(string? cron)
        {
            if (string.IsNullOrWhiteSpace(cron))
                return "N/A";

            var match = All.FirstOrDefault(p => p.Value == cron);
            return match.Value is not null ? match.Label : cron;
        }

        /// <summary>
        /// Verifica se il valore CRON corrisponde a uno dei preset disponibili.
        /// </summary>
        /// <param name="cron">Espressione CRON da verificare.</param>
        public static bool IsPreset(string? cron) =>
            !string.IsNullOrWhiteSpace(cron) && All.Any(p => p.Value == cron);
    }
}
