using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class TaskDaEseguire
{
    public int IdTaskDaEseguire { get; set; }

    public string IdTaskHangFire { get; set; } = null!;

    public int IdLavorazioneFaseDateReading { get; set; }

    public string Stato { get; set; } = null!;

    public DateTime DataStato { get; set; }

    public int? GiorniPrecedenti { get; set; }

    public string? CronExpression { get; set; }

    public bool Enabled { get; set; }

    public DateTime? LastRunUtc { get; set; }

    public string? LastError { get; set; }

    public int ConsecutiveFailures { get; set; }

    public int? IdConfigurazioneDatabase { get; set; }

    public virtual ConfigurazioneFontiDati? IdConfigurazioneDatabaseNavigation { get; set; }

    public virtual LavorazioniFasiDataReading IdLavorazioneFaseDateReadingNavigation { get; set; } = null!;
}
