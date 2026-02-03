using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.DbApplication;

/// <summary>
/// Configurazione unificata per fonti dati.
/// Supporta: SQL, EmailCSV, HandlerIntegrato, Pipeline.
/// </summary>
[Table("ConfigurazioneFontiDati")]
public partial class ConfigurazioneFontiDati
{
    [Key]
    public int IdConfigurazione { get; set; }

    [Required]
    [StringLength(100)]
    public string CodiceConfigurazione { get; set; } = null!;



    [StringLength(500)]
    public string? DescrizioneConfigurazione { get; set; }

    /// <summary>
    /// Tipo fonte: SQL, EmailCSV, HandlerIntegrato, Pipeline
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TipoFonte { get; set; } = null!;

    // Configurazione SQL
    public string? TestoQuery { get; set; }

    [StringLength(100)]
    public string? ConnectionStringName { get; set; }

    // Configurazione Email
    [StringLength(100)]
    public string? MailServiceCode { get; set; }

    // Configurazione Handler C#
    [StringLength(200)]
    public string? HandlerClassName { get; set; }

    // Metadata
    [StringLength(100)]
    public string? CreatoDa { get; set; }

    public DateTime CreatoIl { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string? ModificatoDa { get; set; }

    public DateTime? ModificatoIl { get; set; }

    public bool FlagAttiva { get; set; } = true;

    [Required]
    public int? GiorniPrecedenti { get; set; } = 10;

    // Navigation Properties
    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; }
        = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<ConfigurazionePipelineStep> PipelineSteps { get; set; }
        = new List<ConfigurazionePipelineStep>();

    public virtual ICollection<TaskDaEseguire> Tasks { get; set; }
        = new List<TaskDaEseguire>();
}
