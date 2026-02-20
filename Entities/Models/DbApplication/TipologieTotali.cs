namespace Entities.Models.DbApplication;

public partial class TipologieTotali
{
    public int IdTipoTotale { get; set; }

    public string TipoTotale { get; set; } = null!;

    public virtual ICollection<LavorazioniFasiTipoTotale> LavorazioniFasiTipoTotales { get; set; } = new List<LavorazioniFasiTipoTotale>();

    public virtual ICollection<TipologieTotaliProduzione> TipologieTotaliProduziones { get; set; } = new List<TipologieTotaliProduzione>();
}
