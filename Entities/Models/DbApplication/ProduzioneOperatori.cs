using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ProduzioneOperatori
{
    public int IdProduzione { get; set; }

    public int IdOperatore { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public int IdFaseLavorazione { get; set; }

    public DateTime DataLavorazione { get; set; }

    public double TempoLavOreCent { get; set; }

    public string? Note { get; set; }

    public int IdReparti { get; set; }

    public int? FlagLavoratoConAltraUtenza { get; set; }

    public string? AltraUtenza { get; set; }

    public int IdTurno { get; set; }

    public int IdCentro { get; set; }

    public int? IdTipoTurno { get; set; }

    public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; } = null!;

    public virtual Operatori IdOperatoreNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;

    public virtual RepartiProduzione IdRepartiNavigation { get; set; } = null!;

    public virtual Turni IdTurnoNavigation { get; set; } = null!;

    public virtual ICollection<TipologieTotaliProduzione> TipologieTotaliProduziones { get; set; } = new List<TipologieTotaliProduzione>();
}
