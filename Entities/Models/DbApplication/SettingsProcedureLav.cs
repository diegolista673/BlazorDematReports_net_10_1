#nullable disable

namespace Entities.Models.DbApplication
{
    public partial class SettingsProcedureLav
    {
        public int IdSettingsProcedureLav { get; set; }
        public int IdFaseLavorazione { get; set; }
        public bool FlagVisualizzaOperatori { get; set; }
        public bool FlagElaboraDatiProduzione { get; set; }
        public int IdProceduraLavorazione { get; set; }

        public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; }
        public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; }
    }
}
