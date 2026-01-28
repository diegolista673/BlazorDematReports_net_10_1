using Entities.Models.DbApplication;

namespace BlazorDematReports.Services.Authentication;

/// <summary>
/// Servizio centralizzato per l'autenticazione utenti.
/// Gestisce autenticazione AD, database fallback e lockout account.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Autentica un utente verificando le credenziali.
    /// Prova prima AD (se configurato), poi fallback su database.
    /// </summary>
    /// <param name="user">Utente dal database.</param>
    /// <param name="password">Password fornita.</param>
    /// <returns>True se autenticato, False altrimenti.</returns>
    Task<bool> AuthenticateAsync(Operatori user, string password);

    /// <summary>
    /// Verifica se un account è bloccato per troppi tentativi falliti.
    /// </summary>
    /// <param name="username">Username da verificare.</param>
    /// <returns>True se l'account è bloccato.</returns>
    Task<bool> IsAccountLockedAsync(string username);


}
