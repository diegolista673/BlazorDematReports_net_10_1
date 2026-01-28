using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.DbApplication;

public partial class TaskDaEseguire
{
    public int IdTaskDaEseguire { get; set; }

    public string IdTaskHangFire { get; set; } = null!;

    public TimeOnly? TimeTask { get; set; }

    public int IdTask { get; set; }

    public int IdLavorazioneFaseDateReading { get; set; }

    public string Stato { get; set; } = null!;

    public DateTime DataStato { get; set; }

    public int? GiorniPrecedenti { get; set; }

    public string? CronExpression { get; set; }

    public bool Enabled { get; set; }

    public DateTime? LastRunUtc { get; set; }

    public string? LastError { get; set; }

    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// FK a ConfigurazioneFontiDati per il sistema unificato.
    /// Contiene la configurazione completa per query SQL, Email, Handler o Pipeline.
    /// </summary>
    public int? IdConfigurazioneDatabase { get; set; }

    public virtual LavorazioniFasiDataReading IdLavorazioneFaseDateReadingNavigation { get; set; } = null!;

    public virtual TabellaTask IdTaskNavigation { get; set; } = null!;

    /// <summary>
    /// Navigation property per configurazione unificata fonti dati.
    /// </summary>
    [ForeignKey("IdConfigurazioneDatabase")]
    public virtual ConfigurazioneFontiDati? ConfigurazioneDatabase { get; set; }
}
