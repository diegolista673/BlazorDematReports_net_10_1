using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Models.DbApplication;

/// <summary>
/// Mapping configurazione ? procedura/fase/centro.
/// </summary>
[Table("ConfigurazioneFaseCentro")]
public partial class ConfigurazioneFaseCentro
{
    [Key]
    public int IdFaseCentro { get; set; }

    [Required]
    public int IdConfigurazione { get; set; }

    [Required]
    public int IdProceduraLavorazione { get; set; }

    [Required]
    public int IdFaseLavorazione { get; set; }

    [Required]
    public int IdCentro { get; set; }

    /// <summary>
    /// Query override per questa specifica combinazione fase/centro.
    /// </summary>
    public string? TestoQueryOverride { get; set; }

    /// <summary>
    /// Parametri extra in JSON. Es: {"department": "GENOVA"}
    /// </summary>
    public string? ParametriExtra { get; set; }

    /// <summary>
    /// Mapping colonne in JSON. Es: {"Operatore": "OP_SCAN"}
    /// </summary>
    public string? MappingColonne { get; set; }

    public bool FlagAttiva { get; set; } = true;

    // Navigation Properties
    [ForeignKey("IdConfigurazione")]
    public virtual ConfigurazioneFontiDati Configurazione { get; set; } = null!;

    [ForeignKey("IdProceduraLavorazione")]
    public virtual ProcedureLavorazioni Procedura { get; set; } = null!;

    [ForeignKey("IdFaseLavorazione")]
    public virtual FasiLavorazione Fase { get; set; } = null!;

    [ForeignKey("IdCentro")]
    public virtual CentriLavorazione Centro { get; set; } = null!;
}
