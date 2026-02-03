using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ConfigurazioneFaseCentro
{
    public int IdFaseCentro { get; set; }

    public int IdConfigurazione { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public int IdFaseLavorazione { get; set; }

    public int IdCentro { get; set; }

    public string? TestoQueryOverride { get; set; }

    public string? ParametriExtra { get; set; }

    public string? MappingColonne { get; set; }

    public bool FlagAttiva { get; set; }

    public virtual CentriLavorazione IdCentroNavigation { get; set; } = null!;

    public virtual ConfigurazioneFontiDati IdConfigurazioneNavigation { get; set; } = null!;

    public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;
}
