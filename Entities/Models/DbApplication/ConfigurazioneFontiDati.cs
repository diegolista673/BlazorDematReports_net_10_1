using System;
using System.Collections.Generic;
using Entities.Enums;

namespace Entities.Models.DbApplication;

/// <summary>
/// Configurazione fonte dati unificata.
/// TipoFonte è un enum convertito automaticamente in string dal Value Converter.
/// </summary>
public partial class ConfigurazioneFontiDati
{
    public int IdConfigurazione { get; set; }

    public string CodiceConfigurazione { get; set; } = null!;

    public string? DescrizioneConfigurazione { get; set; }

    /// <summary>
    /// Tipo fonte dati (SQL, HandlerIntegrato).
    /// Enum con conversione automatica a string nel database.
    /// </summary>
    public TipoFonteData TipoFonte { get; set; }

    public string? ConnectionStringName { get; set; }

    public string? HandlerClassName { get; set; }

    public string? CreatoDa { get; set; }

    public DateTime? CreatoIl { get; set; }

    public string? ModificatoDa { get; set; }

    public DateTime? ModificatoIl { get; set; }

    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
