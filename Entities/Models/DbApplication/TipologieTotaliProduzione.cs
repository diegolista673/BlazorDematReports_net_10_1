namespace Entities.Models.DbApplication;

public partial class TipologieTotaliProduzione
{
    public int IdproduzioneTotale { get; set; }

    public int IdproduzioneOperatore { get; set; }

    public int IdtipologiaTotale { get; set; }

    public int Totale { get; set; }

    public string TipoTotale { get; set; } = null!;

    public virtual ProduzioneOperatori IdproduzioneOperatoreNavigation { get; set; } = null!;

    public virtual TipologieTotali IdtipologiaTotaleNavigation { get; set; } = null!;
}
