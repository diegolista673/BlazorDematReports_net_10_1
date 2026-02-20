namespace Entities.Models.DbApplication;

public partial class VwConfigurazioneTaskSummary
{
    public int IdConfigurazione { get; set; }

    public string CodiceConfigurazione { get; set; } = null!;

    public string TipoFonte { get; set; } = null!;

    public int IdFaseCentro { get; set; }

    public int IdFaseLavorazione { get; set; }

    public bool MappingAttivo { get; set; }

    public bool TaskAbilitato { get; set; }

    public int? NumeroTask { get; set; }

    public int? TaskAttivi { get; set; }

    public int? TaskConfigured { get; set; }

    public int? TaskCompleted { get; set; }

    public int? TaskError { get; set; }
}
