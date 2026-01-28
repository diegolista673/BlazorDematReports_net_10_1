namespace Entities.Models.DbApplication;

public partial class TabelleLavorazioni
{
    public int IdTabella { get; set; }

    public string? Tabella { get; set; }

    public string? Tipologia { get; set; }

    public string? Commessa { get; set; }

    public string? ScatolaCaptivaEsiti { get; set; }

    public string? ScatolaCaptivaInEsiti { get; set; }

    public string? ScatolaCaptivaMassiva { get; set; }

    public string? ScatolaCaptiva { get; set; }

    public string? ScatolaCaptivaPrioritario { get; set; }

    public string? TabellaBatchEsitiInesiti { get; set; }

    public string? TabellaBatchMassiva { get; set; }

    public virtual ICollection<SettingsTabelle> SettingsTabelles { get; set; } = new List<SettingsTabelle>();
}
