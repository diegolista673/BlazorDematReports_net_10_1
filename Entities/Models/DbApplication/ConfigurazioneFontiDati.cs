using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ConfigurazioneFontiDati
{
    public int IdConfigurazione { get; set; }

    public string CodiceConfigurazione { get; set; } = null!;

    public string? DescrizioneConfigurazione { get; set; }

    public string TipoFonte { get; set; } = null!;

    public string? ConnectionStringName { get; set; }

    public string? MailServiceCode { get; set; }

    public string? HandlerClassName { get; set; }

    public string? CreatoDa { get; set; }

    public DateTime? CreatoIl { get; set; }

    public string? ModificatoDa { get; set; }

    public DateTime? ModificatoIl { get; set; }

    public bool FlagAttiva { get; set; }

    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<ConfigurazionePipelineStep> ConfigurazionePipelineSteps { get; set; } = new List<ConfigurazionePipelineStep>();

    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
