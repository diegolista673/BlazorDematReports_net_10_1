using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ProduzioneSistema
{
    public int IdProduzioneSistema { get; set; }

    public int IdOperatore { get; set; }

    public string? Operatore { get; set; }

    public string? OperatoreNonRiconosciuto { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public int IdFaseLavorazione { get; set; }

    public DateTime DataLavorazione { get; set; }

    public DateTime? DataAggiornamento { get; set; }

    public int? Documenti { get; set; }

    public int? Fogli { get; set; }

    public int? Pagine { get; set; }

    public int? Scarti { get; set; }

    public bool? FlagInserimentoAuto { get; set; }

    public bool? FlagInserimentoManuale { get; set; }

    public int? PagineSenzaBianco { get; set; }

    public int IdCentro { get; set; }



    public virtual CentriLavorazione IdCentroNavigation { get; set; } = null!;

    public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; } = null!;

    public virtual Operatori IdOperatoreNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;
}
