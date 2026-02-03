using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class FasiLavorazione
{
    public int IdFaseLavorazione { get; set; }

    public string FaseLavorazione { get; set; } = null!;

    public bool UtilizzataDaSistema { get; set; }

    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<LavorazioniFasiDataReading> LavorazioniFasiDataReadings { get; set; } = new List<LavorazioniFasiDataReading>();

    public virtual ICollection<LavorazioniFasiTipoTotale> LavorazioniFasiTipoTotales { get; set; } = new List<LavorazioniFasiTipoTotale>();

    public virtual ICollection<ProduzioneOperatori> ProduzioneOperatoris { get; set; } = new List<ProduzioneOperatori>();

    public virtual ICollection<ProduzioneSistema> ProduzioneSistemas { get; set; } = new List<ProduzioneSistema>();
}
