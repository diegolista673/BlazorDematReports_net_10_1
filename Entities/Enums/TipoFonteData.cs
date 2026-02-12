using System.ComponentModel;

namespace Entities.Enums;

/// <summary>
/// Tipo di fonte dati per la configurazione dell'estrazione produzione.
/// </summary>
public enum TipoFonteData
{
    /// <summary>
    /// Fonte dati SQL: query personalizzata su database configurato.
    /// </summary>
    [Description("Query SQL")]
    SQL = 0,

    /// <summary>
    /// Handler C# integrato: classe custom per elaborazione dati complessa.
    /// Include handler mail (HERA16, ADER4) e altri handler personalizzati.
    /// </summary>
    [Description("Handler C# Integrato")]
    HandlerIntegrato = 1
}
