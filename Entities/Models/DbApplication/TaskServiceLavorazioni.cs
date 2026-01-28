using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class TaskServiceLavorazioni
{
    public int IdService { get; set; }

    public string TaskService { get; set; } = null!;

    public int IdProceduraLavorazione { get; set; }

    public string IdTaskHangFire { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;
}
