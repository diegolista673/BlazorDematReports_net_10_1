namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO per la creazione/modifica di un singolo task
    /// </summary>
    public class ConfigurazioneTaskEditDto
    {
        public int IdFaseCentro { get; set; }
        public int IdConfigurazione { get; set; }
        public int IdProceduraLavorazione { get; set; }
        public int IdFaseLavorazione { get; set; }
        public int IdCentro { get; set; }
        
        // Configurazione Task
        public string TipoTask { get; set; } = TipoTaskEnum.SQL;
        public string CronExpression { get; set; } = "0 5 * * *";
        public string? TestoQueryTask { get; set; }
        public string? MailServiceCode { get; set; }
        public string? HandlerClassName { get; set; }
        public bool EnabledTask { get; set; } = true;
        
        // Display (readonly)
        public string NomeProcedura { get; set; } = string.Empty;
        public string NomeFase { get; set; } = string.Empty;
        public string NomeCentro { get; set; } = string.Empty;
        
        /// <summary>
        /// Valida che la configurazione sia corretta in base al tipo task
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(CronExpression))
                return false;
            
            return TipoTask switch
            {
                TipoTaskEnum.SQL => !string.IsNullOrWhiteSpace(TestoQueryTask),
                TipoTaskEnum.EmailCSV => !string.IsNullOrWhiteSpace(MailServiceCode),
                TipoTaskEnum.HandlerIntegrato => !string.IsNullOrWhiteSpace(HandlerClassName),
                TipoTaskEnum.Pipeline => true, // Pipeline ha logica separata
                _ => false
            };
        }
        
        /// <summary>
        /// Restituisce una descrizione leggibile della configurazione
        /// </summary>
        public string GetDescription()
        {
            var parts = new List<string>
            {
                $"Tipo: {TipoTask}",
                $"CRON: {CronExpression}",
                $"Stato: {(EnabledTask ? "Attivo" : "Disattivo")}"
            };
            
            if (!string.IsNullOrWhiteSpace(TestoQueryTask))
                parts.Add($"Query: {TestoQueryTask.Substring(0, Math.Min(50, TestoQueryTask.Length))}...");
            
            if (!string.IsNullOrWhiteSpace(MailServiceCode))
                parts.Add($"Mail: {MailServiceCode}");
            
            if (!string.IsNullOrWhiteSpace(HandlerClassName))
                parts.Add($"Handler: {HandlerClassName}");
            
            return string.Join(" | ", parts);
        }
    }
    
    /// <summary>
    /// Enum per tipologie task supportate
    /// </summary>
    public static class TipoTaskEnum
    {
        public const string SQL = "SQL";
        public const string EmailCSV = "EmailCSV";
        public const string HandlerIntegrato = "HandlerIntegrato";
        public const string Pipeline = "Pipeline";
        
        public static List<string> GetAll() => new() { SQL, EmailCSV, HandlerIntegrato, Pipeline };
        
        public static string GetDisplayName(string tipoTask) => tipoTask switch
        {
            SQL => "Query SQL",
            EmailCSV => "Email CSV Import",
            HandlerIntegrato => "Handler C# Integrato",
            Pipeline => "Pipeline Multi-Step",
            _ => tipoTask
        };
        
        public static string GetIcon(string tipoTask) => tipoTask switch
        {
            SQL => "Storage",
            EmailCSV => "Email",
            HandlerIntegrato => "Code",
            Pipeline => "AccountTree",
            _ => "HelpOutline"
        };
    }
}
