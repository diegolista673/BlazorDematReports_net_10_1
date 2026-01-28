using BlazorDematReports.Application;
using Entities.Models.DbApplication;
using Microsoft.Extensions.Options;

namespace BlazorDematReports.Services.Authentication;

/// <summary>
/// Implementazione del servizio di autenticazione centralizzato.
/// Gestisce autenticazione AD in base all'ambiente configurato.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IActiveDirectoryService _adService;
    private readonly LoginSettings _loginSettings;

    public AuthenticationService(
        ILogger<AuthenticationService> logger,
        IActiveDirectoryService adService,
        IOptions<LoginSettings> loginSettings)
    {
        _logger = logger;
        _adService = adService;
        _loginSettings = loginSettings.Value;
    }

    /// <inheritdoc/>
    public async Task<bool> AuthenticateAsync(Operatori user, string password)
    {
        if (user == null)
        {
            _logger.LogWarning("Authentication attempt with null user");
            return false;
        }

        try
        {
            bool isAuthenticated = false;

            // Modalità Development - autenticazione semplificata
            if (_loginSettings.IsDevelopment)
            {
                _logger.LogDebug("Development mode: simplified authentication for {Username}", user.Operatore);
                
                if (_loginSettings.AllowAutoLogin)
                {
                    // In Development con AllowAutoLogin, accetta qualsiasi password
                    isAuthenticated = true;
                }
                else
                {
                    // Verifica password di test
                    isAuthenticated = password == _loginSettings.DefaultTestPassword;
                }
            }
            else
            {
                // Modalità Production - prova Active Directory
                if (_loginSettings.RequireActiveDirectory)
                {
                    _logger.LogDebug("Attempting AD authentication for user: {Username}", user.Operatore);
                    isAuthenticated = await _adService.AuthenticateAsync(user.Operatore, password);
                }
                else
                {
                    // Se AD non richiesto, accetta (backward compatibility)
                    isAuthenticated = true;
                }
            }

            if (isAuthenticated)
            {
                _logger.LogInformation("Successful authentication for user: {Username}", user.Operatore);
            }
            else
            {
                _logger.LogWarning("Failed authentication for user: {Username}", user.Operatore);
            }

            return isAuthenticated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error for user: {Username}", user.Operatore);
            return false;
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsAccountLockedAsync(string username)
    {
        // Lockout disabilitato - ritorna sempre false
        return Task.FromResult(false);
    }


}
