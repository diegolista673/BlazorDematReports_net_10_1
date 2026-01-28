using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class Operatori
{
    public int Idoperatore { get; set; }

    public string Operatore { get; set; } = null!;

    public int Idcentro { get; set; }

    public string Azienda { get; set; } = null!;

    public int IdRuolo { get; set; }

    public bool FlagOperatoreAttivo { get; set; }

    public string? Password { get; set; }

    public virtual ICollection<CentriVisibili> CentriVisibilis { get; set; } = new List<CentriVisibili>();

    public virtual Ruoli IdRuoloNavigation { get; set; } = null!;

    public virtual CentriLavorazione IdcentroNavigation { get; set; } = null!;

    public virtual ICollection<ProcedureCliente> ProcedureClientes { get; set; } = new List<ProcedureCliente>();

    public virtual ICollection<ProcedureLavorazioni> ProcedureLavorazionis { get; set; } = new List<ProcedureLavorazioni>();

    public virtual ICollection<ProduzioneOperatori> ProduzioneOperatoris { get; set; } = new List<ProduzioneOperatori>();

    public virtual ICollection<ProduzioneSistema> ProduzioneSistemas { get; set; } = new List<ProduzioneSistema>();
}
