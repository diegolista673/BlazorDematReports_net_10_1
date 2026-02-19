using BlazorDematReports.Application;
using Microsoft.Extensions.Options;
using System.DirectoryServices.AccountManagement;

namespace BlazorDematReports.Services.Authentication;

/// <summary>
/// Implementazione del servizio di autenticazione Active Directory.
/// Utilizza PrincipalContext per la validazione delle credenziali.
/// </summary>
public class ActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<ActiveDirectoryService> _logger;
    private readonly ActiveDirectorySettings _settings;

    public ActiveDirectoryService(
        ILogger<ActiveDirectoryService> logger,
        IOptions<ActiveDirectorySettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(_settings?.Domain))
        {
            throw new InvalidOperationException(
                "Active Directory Domain not configured. " +
                "Add 'ActiveDirectory:Domain' to appsettings.json");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("AD authentication attempt with empty credentials");
            return false;
        }

        return await Task.Run(() => AuthenticateInternal(username, password));
    }

    private bool AuthenticateInternal(string username, string password)
    {
#pragma warning disable CA1416 // Convalida compatibilità della piattaforma
        try
        {
            // Definiamo il contesto del dominio
            using var context = new PrincipalContext(ContextType.Domain, _settings.Domain);
            
            // Validazione DIRETTA delle credenziali dell'utente
            // Questo metodo effettua internamente il bind LDAP necessario
            bool isValid = context.ValidateCredentials(username, password);

            if (!isValid)
            {
                _logger.LogWarning("Credenziali non valide o account bloccato per: {Username}", username);
                return false;
            }
            
            _logger.LogInformation("Autenticazione AD riuscita per: {Username}", username);
            return true;
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Server Active Directory non raggiungibile per l'utente {Username}", username);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'autenticazione AD per l'utente {Username}", username);
            return false;
        }
#pragma warning restore CA1416 // Convalida compatibilità della piattaforma
    }
}
