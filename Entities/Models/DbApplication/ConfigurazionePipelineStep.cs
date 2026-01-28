using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.DbApplication;

/// <summary>
/// Step di una pipeline multi-step.
/// </summary>
[Table("ConfigurazionePipelineStep")]
public partial class ConfigurazionePipelineStep
{
    [Key]
    public int IdPipelineStep { get; set; }

    [Required]
    public int IdConfigurazione { get; set; }

    [Required]
    public int NumeroStep { get; set; }

    [Required]
    [StringLength(100)]
    public string NomeStep { get; set; } = null!;

    /// <summary>
    /// Tipo step: Query, Filter, Transform, Aggregate, Merge
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TipoStep { get; set; } = null!;

    /// <summary>
    /// Configurazione step in JSON.
    /// </summary>
    [Required]
    public string ConfigurazioneStep { get; set; } = null!;

    public bool FlagAttiva { get; set; } = true;

    // Navigation Property
    [ForeignKey("IdConfigurazione")]
    public virtual ConfigurazioneFontiDati Configurazione { get; set; } = null!;
}
