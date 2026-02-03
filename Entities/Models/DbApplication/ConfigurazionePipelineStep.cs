using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ConfigurazionePipelineStep
{
    public int IdPipelineStep { get; set; }

    public int IdConfigurazione { get; set; }

    public int NumeroStep { get; set; }

    public string NomeStep { get; set; } = null!;

    public string TipoStep { get; set; } = null!;

    public string ConfigurazioneStep { get; set; } = null!;

    public bool FlagAttiva { get; set; }

    public virtual ConfigurazioneFontiDati IdConfigurazioneNavigation { get; set; } = null!;
}
