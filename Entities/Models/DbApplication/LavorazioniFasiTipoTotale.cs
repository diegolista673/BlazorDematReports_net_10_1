namespace Entities.Models.DbApplication;

public partial class LavorazioniFasiTipoTotale
{
    public int IdLavorazioneFaseTipoTotale { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public int IdFase { get; set; }

    public int IdTipologiaTotale { get; set; }

    public virtual FasiLavorazione IdFaseNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;

    public virtual TipologieTotali IdTipologiaTotaleNavigation { get; set; } = null!;
}
