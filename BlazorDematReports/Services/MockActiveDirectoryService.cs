namespace BlazorDematReports.Services.Authentication;

/// <summary>
/// Implementazione mock di IActiveDirectoryService per sviluppo locale senza AD.
/// In ambiente Development, l'autenticazione AD viene bypassata.
/// </summary>
public class MockActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<MockActiveDirectoryService> _logger;

    public MockActiveDirectoryService(ILogger<MockActiveDirectoryService> logger)
    {
        _logger = logger;
        _logger.LogWarning("?? Using MockActiveDirectoryService - AD authentication is DISABLED (Development mode)");
    }

    /// <inheritdoc/>
    public Task<bool> AuthenticateAsync(string username, string password)
    {
        _logger.LogInformation("Mock AD authentication for user: {Username} - Always returns FALSE (use database fallback)", username);
        
        // Ritorna false per forzare il fallback su autenticazione database/semplificata
        return Task.FromResult(false);
    }
}
