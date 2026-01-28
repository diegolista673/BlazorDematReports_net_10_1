using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Application;

/// <summary>
/// Configurazione per l'autenticazione Active Directory.
/// </summary>
public class ActiveDirectorySettings
{
    /// <summary>
    /// Nome del dominio Active Directory (es. "postel.it").
    /// </summary>
    [Required]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in secondi per le operazioni AD.
    /// </summary>
    [Range(5, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
