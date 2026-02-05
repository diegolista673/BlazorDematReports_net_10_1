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

    public string? MappingColonne { get; set; }

    public bool FlagAttiva { get; set; }

    public bool IsTaskEnabled { get; set; }

    public string? TaskDescription { get; set; }

    public string? TipoTask { get; set; }

    public string? CronExpression { get; set; }

    public string? TestoQueryTask { get; set; }

    public string? MailServiceCode { get; set; }

    public string? HandlerClassName { get; set; }

    public bool EnabledTask { get; set; }

    public DateTime? UltimaModificaTask { get; set; }

    public int? GiorniPrecedenti { get; set; }

    public virtual CentriLavorazione IdCentroNavigation { get; set; } = null!;

    public virtual ConfigurazioneFontiDati IdConfigurazioneNavigation { get; set; } = null!;

    public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;
}
