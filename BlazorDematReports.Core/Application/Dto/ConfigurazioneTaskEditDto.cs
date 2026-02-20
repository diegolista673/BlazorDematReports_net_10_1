
using Entities.Enums;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO per la creazione/modifica di un singolo task.
    /// Usa TipoFonteData enum per type safety completa.
    /// </summary>
    /// <summary>
    /// DTO per la creazione/modifica di un singolo task.
    /// Usa TipoFonteData enum per type safety completa.
    /// </summary>
    public class ConfigurazioneTaskEditDto
    {
        public int IdFaseCentro { get; set; }
        public int IdConfigurazione { get; set; }
        public int IdProceduraLavorazione { get; set; }
        public int IdFaseLavorazione { get; set; }
        public int IdCentro { get; set; }

        /// <summary>
        /// Tipo di task (SQL o HandlerIntegrato).
        /// Type-safe: usa enum invece di stringa.
        /// </summary>
        public TipoFonteData TipoTask { get; set; } = TipoFonteData.SQL;

        public string CronExpression { get; set; } = "0 5 * * *";
        public string? TestoQueryTask { get; set; }
        public string? HandlerClassName { get; set; }

        // Display (readonly)
        public string NomeProcedura { get; set; } = string.Empty;
        public string NomeFase { get; set; } = string.Empty;
        public string NomeCentro { get; set; } = string.Empty;

        /// <summary>
        /// Valida che la configurazione sia corretta in base al tipo task.
        /// </summary>
        /// <summary>
        /// Valida che la configurazione sia corretta in base al tipo task.
        /// </summary>
        /// <returns>True se la configurazione è valida, false altrimenti.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(CronExpression))
                return false;

            return TipoTask switch
            {
                TipoFonteData.SQL => !string.IsNullOrWhiteSpace(TestoQueryTask),
                TipoFonteData.HandlerIntegrato => !string.IsNullOrWhiteSpace(HandlerClassName),
                _ => false
            };
        }

        /// <summary>
        /// Restituisce una descrizione leggibile della configurazione.
        /// </summary>
        /// <summary>
        /// Restituisce una descrizione leggibile della configurazione.
        /// </summary>
        /// <returns>Descrizione della configurazione.</returns>
        public string GetDescription()
        {
            var parts = new List<string>
            {
                $"Tipo: {TipoTask}",
                $"CRON: {CronExpression}",
                "Stato: Gestito da TaskDaEseguire"
            };

            if (!string.IsNullOrWhiteSpace(TestoQueryTask))
                parts.Add($"Query: {TestoQueryTask.Substring(0, Math.Min(50, TestoQueryTask.Length))}...");

            if (!string.IsNullOrWhiteSpace(HandlerClassName))
                parts.Add($"Handler: {HandlerClassName}");

            return string.Join(" | ", parts);
        }
    }
}
