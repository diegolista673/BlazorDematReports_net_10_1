using System;
using System.Collections.Generic;
using Entities.Enums;

namespace Entities.Models.DbApplication;

/// <summary>
/// Configurazione centralizzata delle fonti dati per l'estrazione della produzione.
/// </summary>
public partial class ConfigurazioneFontiDati
{
    /// <summary>
    /// Identificativo univoco della configurazione.
    /// </summary>
    public int IdConfigurazione { get; set; }

    /// <summary>
    /// Codice univoco della configurazione (formato: P{IdProc:D2}F{IdFase:D2}).
    /// </summary>
    public string CodiceConfigurazione { get; set; } = null!;

    /// <summary>
    /// Descrizione estesa della configurazione.
    /// </summary>
    public string? DescrizioneConfigurazione { get; set; }

    /// <summary>
    /// Tipo di fonte dati (SQL, HandlerIntegrato).
    /// </summary>
    public TipoFonteData TipoFonte { get; set; }

    /// <summary>
    /// Nome della connection string in appsettings.json (per TipoFonte = SQL).
    /// </summary>
    public string? ConnectionStringName { get; set; }

    /// <summary>
    /// Nome della classe handler C# (per TipoFonte = HandlerIntegrato).
    /// </summary>
    public string? HandlerClassName { get; set; }

    /// <summary>
    /// Utente che ha creato la configurazione.
    /// </summary>
    public string? CreatoDa { get; set; }

    /// <summary>
    /// Data e ora di creazione della configurazione.
    /// </summary>
    public DateTime? CreatoIl { get; set; }

    /// <summary>
    /// Utente che ha modificato la configurazione per ultimo.
    /// </summary>
    public string? ModificatoDa { get; set; }

    /// <summary>
    /// Data e ora dell'ultima modifica.
    /// </summary>
    public DateTime? ModificatoIl { get; set; }

    /// <summary>
    /// Mapping fase/centro associati alla configurazione.
    /// </summary>
    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    /// <summary>
    /// Task schedulati generati da questa configurazione.
    /// </summary>
    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
