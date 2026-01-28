namespace Entities.Models.DbApplication;

public partial class SettingsTabelle
{
    public int IdSettingsTabelle { get; set; }

    public int IdTabellaLavorazione { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;

    public virtual TabelleLavorazioni IdTabellaLavorazioneNavigation { get; set; } = null!;
}
