using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class RepartiProduzione
{
    public int IdReparti { get; set; }

    public string Reparti { get; set; } = null!;

    public virtual ICollection<ProcedureLavorazioni> ProcedureLavorazionis { get; set; } = new List<ProcedureLavorazioni>();

    public virtual ICollection<ProduzioneOperatori> ProduzioneOperatoris { get; set; } = new List<ProduzioneOperatori>();
}
