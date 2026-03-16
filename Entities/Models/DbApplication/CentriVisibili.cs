using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class CentriVisibili
{
    public int IdCentriVisibili { get; set; }

    public int IdOperatore { get; set; }

    public int IdCentro { get; set; }

    public bool FlagVisibile { get; set; }

    public virtual CentriLavorazione IdCentroNavigation { get; set; } = null!;

    public virtual Operatori IdOperatoreNavigation { get; set; } = null!;
}
