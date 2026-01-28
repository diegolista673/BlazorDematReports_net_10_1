namespace BlazorDematReports.Application;

/// <summary>
/// Modello di configurazione per le impostazioni di login basate sull'ambiente.
/// Gestisce la differenza tra modalità Development (test) e Production (Active Directory).
/// </summary>
public class LoginSettings
{
    /// <summary>
    /// Nome dell'ambiente corrente (Development, Production, Staging).
    /// </summary>
    public string Environment { get; set; } = "Default";

    /// <summary>
    /// Se true, richiede autenticazione Active Directory in produzione.
    /// </summary>
    public bool RequireActiveDirectory { get; set; } = false;

    /// <summary>
    /// Username predefinito per ambiente di test (solo Development).
    /// </summary>
    public string DefaultTestUser { get; set; } = string.Empty;

    /// <summary>
    /// Password predefinita per ambiente di test (solo Development).
    /// </summary>
    public string DefaultTestPassword { get; set; } = string.Empty;

    /// <summary>
    /// Se true, mostra il badge dell'ambiente nella pagina di login.
    /// </summary>
    public bool ShowEnvironmentBadge { get; set; } = true;

    /// <summary>
    /// Se true, permette login automatico senza password (solo Development).
    /// </summary>
    public bool AllowAutoLogin { get; set; } = false;

    /// <summary>
    /// Verifica se l'ambiente corrente è Development.
    /// </summary>
    public bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Verifica se l'ambiente corrente è Production.
    /// </summary>
    public bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
}
