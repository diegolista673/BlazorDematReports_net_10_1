using BlazorDematReports.Application;
using BlazorDematReports.Services.Authentication;
using Entities.Models.DbApplication;
using IAppAuthenticationService = BlazorDematReports.Services.Authentication.IAuthenticationService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BlazorDematReports.Components.Pages.Account
{
    /// <summary>
    /// Pagina di login con supporto per autenticazione Active Directory e ambiente-specifica.
    /// </summary>
    public class LoginModel : PageModel
    {
        private readonly IDbContextFactory<DematReportsContext> _contextFactory;
        private readonly ILogger<LoginModel> _logger;
        private readonly IAppAuthenticationService _authService;
        private readonly LoginSettings _loginSettings;

        [Required(ErrorMessage = "Username è obbligatorio")]
        [BindProperty]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password è obbligatoria")]
        [BindProperty, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [TempData]
        public string? Message { get; set; }

        /// <summary>
        /// Indica se l'ambiente corrente è Development (per UI).
        /// </summary>
        public bool IsDevelopment => _loginSettings.IsDevelopment;

        /// <summary>
        /// Nome dell'ambiente corrente (per badge UI).
        /// </summary>
        public string EnvironmentName => _loginSettings.Environment;

        /// <summary>
        /// Se mostrare il badge ambiente nella UI.
        /// </summary>
        public bool ShowEnvironmentBadge => _loginSettings.ShowEnvironmentBadge;

        public LoginModel(
            ILogger<LoginModel> logger,
            IDbContextFactory<DematReportsContext> context,
            IAppAuthenticationService authService,
            IOptions<LoginSettings> loginSettings)
        {
            _logger = logger;
            _contextFactory = context;
            _authService = authService;
            _loginSettings = loginSettings.Value;

            _logger.LogInformation("Pagina di Login - Ambiente: {Environment}", _loginSettings.Environment);
        }

        public IActionResult OnGet()
        {
            // Redirect se già autenticato
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToPage("/");
            }

            // Pre-compila username/password solo in Development
            if (_loginSettings.IsDevelopment && _loginSettings.AllowAutoLogin)
            {
                UserName = _loginSettings.DefaultTestUser;
                Password = _loginSettings.DefaultTestPassword;
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // 1. Recupera utente dal database
                using var context = _contextFactory.CreateDbContext();
                var user = await context.Operatoris
                    .Where(x => x.Operatore == UserName)
                    .Include(c => c.IdcentroNavigation)
                    .Include(c => c.IdRuoloNavigation)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                // 2. Verifica autenticazione
                bool isAuthenticated = false;

                if (user != null)
                {
                    // Verifica se utente attivo
                    if (user.FlagOperatoreAttivo != true)
                    {
                        Message = "Login fallito - Utente non attivo";
                        _logger.LogWarning("Login attempt for inactive user: {UserName}", UserName);
                        return Page();
                    }

                    // Autentica tramite servizio centralizzato
                    isAuthenticated = await _authService.AuthenticateAsync(user, Password);
                }
                else
                {
                    // Timing attack mitigation: esegui delay anche se user non esiste
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                // 3. Crea sessione solo se autenticato
                if (isAuthenticated && user != null)
                {
                    await CreateAuthenticationSessionAsync(user);

                    _logger.LogInformation("Login successful for user: {UserName} in environment: {Environment}",
                        UserName, _loginSettings.Environment);

                    return Redirect("~/");
                }

                // Messaggio generico per evitare user enumeration
                Message = "Credenziali non valide. Riprova.";
                return Page();
            }
            catch (Exception ex)
            {
                // NON esporre mai dettagli exception all'utente
                _logger.LogError(ex, "Login error for user: {UserName}", UserName);
                Message = "Si è verificato un errore. Riprova più tardi.";
                return Page();
            }
        }

        /// <summary>
        /// Crea la sessione di autenticazione per l'utente verificato.
        /// </summary>
        private async Task CreateAuthenticationSessionAsync(Operatori user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Operatore),
                new Claim("User", user.Operatore),
                new Claim("IdOperatore", user.Idoperatore.ToString()),
                new Claim("Azienda", user.Azienda ?? string.Empty),
                new Claim("IdCentro", user.Idcentro.ToString()),
                new Claim("Environment", _loginSettings.Environment)
            };

            // Aggiungi ruolo
            if (user.IdRuoloNavigation != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.IdRuoloNavigation.Ruolo));
                claims.Add(new Claim("Ruolo", user.IdRuoloNavigation.Ruolo));
            }

            // Aggiungi claims del centro
            if (user.IdcentroNavigation != null)
            {
                claims.Add(new Claim("Centro", user.IdcentroNavigation.Centro));
                if (!string.IsNullOrEmpty(user.IdcentroNavigation.Sigla))
                {
                    claims.Add(new Claim("Sigla", user.IdcentroNavigation.Sigla));
                }
            }

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(4),
                IsPersistent = false,
                AllowRefresh = true,
                IssuedUtc = DateTimeOffset.UtcNow
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogDebug("Authentication session created for user: {UserName}, Role: {Role}, Centro: {Centro}",
                user.Operatore,
                user.IdRuoloNavigation?.Ruolo ?? "N/A",
                user.IdcentroNavigation?.Centro ?? "N/A");
        }
    }
}
