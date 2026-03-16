using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class CentriLavorazione
{
    public int Idcentro { get; set; }

    public string Centro { get; set; } = null!;

    public string Sigla { get; set; } = null!;

    public virtual ICollection<CentriVisibili> CentriVisibilis { get; set; } = new List<CentriVisibili>();

    public virtual ICollection<Clienti> Clientis { get; set; } = new List<Clienti>();

    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<Operatori> Operatoris { get; set; } = new List<Operatori>();

    public virtual ICollection<ProduzioneSistema> ProduzioneSistemas { get; set; } = new List<ProduzioneSistema>();
}
