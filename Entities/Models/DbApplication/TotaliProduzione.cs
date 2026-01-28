#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class TotaliProduzione
    {
        public int IdproduzioneTotale { get; set; }
        public int IdproduzioneOperatore { get; set; }
        public int IdtipologiaTotale { get; set; }
        public int Totale { get; set; }

        public virtual ProduzioneOperatori IdproduzioneOperatoreNavigation { get; set; }
        public virtual TipologieTotali IdtipologiaTotaleNavigation { get; set; }
    }
}
