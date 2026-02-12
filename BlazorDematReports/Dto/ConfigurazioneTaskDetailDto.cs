using Entities.Enums;
using MudBlazor;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO dettagliato per la gestione dei task associati a una configurazione
    /// </summary>
    public class ConfigurazioneTaskDetailDto
    {
        public int IdConfigurazione { get; set; }
        public string CodiceConfigurazione { get; set; } = null!;
        public TipoFonteData TipoFonte { get; set; }
        
        // Query Management
        public string? TestoQueryPrincipale { get; set; }
        public bool HasMultipleQueries => Mappings.Any(m => !string.IsNullOrEmpty(m.TestoQueryOverride));
        
        // Mappings con Task
        public List<MappingConTaskDto> Mappings { get; set; } = new();
        
        // Statistiche Task
        public int TotaleTasks => Mappings.Sum(m => m.Tasks.Count);
        public int TaskAttivi => Mappings.Sum(m => m.Tasks.Count(t => t.Enabled));
        public int TaskInErrore => Mappings.Sum(m => m.Tasks.Count(t => t.Stato == "ERROR"));
    }

    public class MappingConTaskDto
    {
        public int IdFaseCentro { get; set; }
        public string NomeProcedura { get; set; } = null!;
        public string NomeFase { get; set; } = null!;
        public string NomeCentro { get; set; } = null!;
        public bool FlagAttiva { get; set; }
        
        // Query specifica per questo mapping
        public string? TestoQueryOverride { get; set; }
        public bool UsaQueryOverride => !string.IsNullOrEmpty(TestoQueryOverride);
        
        // Task associati a questo mapping
        public List<TaskDto> Tasks { get; set; } = new();
    }

    public class TaskDto
    {
        public int IdTaskDaEseguire { get; set; }
        public string IdTaskHangFire { get; set; } = null!;
        public string Stato { get; set; } = null!;
        public bool Enabled { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? LastRunUtc { get; set; }
        public string? LastError { get; set; }
        public int ConsecutiveFailures { get; set; }
        
        // Computed
        public string DisplayStatus => $"{Stato} ({(Enabled ? "Attivo" : "Disabilitato")})";
        public Color StatusColor => Stato switch
        {
            "CONFIGURED" => Color.Info,
            "COMPLETED" => Color.Success,
            "ERROR" => Color.Error,
            _ => Color.Default
        };
    }
}
