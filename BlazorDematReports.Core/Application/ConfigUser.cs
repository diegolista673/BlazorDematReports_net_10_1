using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlazorDematReports.Core.Application
{
    /// <summary>
    /// Gestisce le informazioni di configurazione e i dati utente correnti tramite IHttpContextAccessor.
    /// </summary>
    public class ConfigUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ConfigUser> _logger;
        private bool _isInitialized = false;

        // Cached values
        private string? _cachedUtente;
        private int _cachedIdUtente;
        private int _cachedIdCentroOrigine;
        private bool _cachedIsAdminRole;
        private bool _cachedIsSupervisorRole;
        private bool _cachedIsResponsabile;
        private bool _cachedIsUserRole;
        private string? _cachedCentroOrigine;
        private string? _cachedSiglaCentroOrigine;

        /// <summary>
        /// Identificativo del centro di lavorazione richiesto.
        /// </summary>
        public int IdCentroLavorazioneRichiesto { get; set; }

        /// <summary>
        /// Costruttore che inizializza il logger e l'accessor del contesto HTTP.
        /// </summary>
        /// <param name="logger">Logger per la classe ConfigUser.</param>
        /// <param name="httpContextAccessor">Accessor per il contesto HTTP.</param>
        public ConfigUser(ILogger<ConfigUser> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Indica se ConfigUser è stato inizializzato con i dati dall'HttpContext.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Inizializza ConfigUser estraendo i dati dall'HttpContext.
        /// Deve essere chiamato da OnInitializedAsync nel componente Blazor.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                {
                    _logger?.LogWarning("HttpContext non disponibile durante l'inizializzazione di ConfigUser");
                    _isInitialized = false;
                    return;
                }

                var user = httpContext.User;
                if (user == null || !user.Identity?.IsAuthenticated == true)
                {
                    _logger?.LogWarning("Utente non autenticato durante l'inizializzazione di ConfigUser");
                    _isInitialized = false;
                    return;
                }

                // Extract and cache user name
                _cachedUtente = user.Identity?.Name;

                // Extract and cache claims
                var claims = user.Identities.FirstOrDefault()?.Claims.ToList() ?? new();

                if (claims.Count > 0)
                {
                    _cachedIdUtente = Convert.ToInt32(claims.FirstOrDefault(x => x.Type.Equals("IdOperatore", StringComparison.OrdinalIgnoreCase))?.Value ?? "0");
                    _cachedIdCentroOrigine = Convert.ToInt32(claims.FirstOrDefault(x => x.Type.Equals("IdCentro", StringComparison.OrdinalIgnoreCase))?.Value ?? "0");
                    _cachedCentroOrigine = claims.FirstOrDefault(x => x.Type.Equals("Centro", StringComparison.OrdinalIgnoreCase))?.Value;
                    _cachedSiglaCentroOrigine = claims.FirstOrDefault(x => x.Type.Equals("Sigla", StringComparison.OrdinalIgnoreCase))?.Value;
                }

                // Cache roles
                _cachedIsAdminRole = user.IsInRole("ADMIN");
                _cachedIsSupervisorRole = user.IsInRole("SUPERVISOR");
                _cachedIsResponsabile = user.IsInRole("RESPONSABILE");
                _cachedIsUserRole = user.IsInRole("USER");

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Errore durante l'inizializzazione di ConfigUser");
                _isInitialized = false;
            }

            await Task.CompletedTask;
        }

        private string? _utente;
        /// <summary>
        /// Nome utente corrente autenticato.
        /// </summary>
        public string Utente
        {
            get
            {
                if (_isInitialized)
                    return _cachedUtente ?? string.Empty;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    _utente = httpContext.User.Identity?.Name;
                    return _utente ?? string.Empty;
                }

                return string.Empty;
            }
            set { _utente = value; }
        }

        private int _idUtente;
        /// <summary>
        /// Identificativo dell'utente corrente.
        /// </summary>
        public int IdUtente
        {
            get
            {
                if (_isInitialized)
                    return _cachedIdUtente;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var claims = httpContext.User.Identities.FirstOrDefault()?.Claims.ToList() ?? new();
                    _idUtente = Convert.ToInt32(claims.FirstOrDefault(x => x.Type.Equals("IdOperatore", StringComparison.OrdinalIgnoreCase))?.Value ?? "0");
                    return _idUtente;
                }

                return 0;
            }
            set { _idUtente = value; }
        }

        private int _idCentroOrigine;
        /// <summary>
        /// Identificativo del centro di origine dell'utente.
        /// </summary>
        public int IdCentroOrigine
        {
            get
            {
                if (_isInitialized)
                    return _cachedIdCentroOrigine;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var claims = httpContext.User.Identities.FirstOrDefault()?.Claims.ToList() ?? new();
                    _idCentroOrigine = Convert.ToInt32(claims.FirstOrDefault(x => x.Type.Equals("IdCentro", StringComparison.OrdinalIgnoreCase))?.Value ?? "0");
                    return _idCentroOrigine;
                }

                return 0;
            }
            set { _idCentroOrigine = value; }
        }

        private bool _isAdminRole;
        /// <summary>
        /// Indica se l'utente ha il ruolo di amministratore.
        /// </summary>
        public bool IsAdminRole
        {
            get
            {
                if (_isInitialized)
                {
                    _isAdminRole = _cachedIsAdminRole;
                    return _isAdminRole;
                }

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    _isAdminRole = httpContext.User.IsInRole("ADMIN");
                    return _isAdminRole;
                }

                return false;
            }
            set { _isAdminRole = value; }
        }

        private bool _isSupervisorRole;
        /// <summary>
        /// Indica se l'utente ha il ruolo di supervisore.
        /// </summary>
        public bool IsSupervisorRole
        {
            get
            {
                if (_isInitialized)
                {
                    _isSupervisorRole = _cachedIsSupervisorRole;
                    return _isSupervisorRole;
                }

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    _isSupervisorRole = httpContext.User.IsInRole("SUPERVISOR");
                    return _isSupervisorRole;
                }

                return false;
            }
            set { _isSupervisorRole = value; }
        }

        private bool _isResponsabile;
        /// <summary>
        /// Indica se l'utente ha il ruolo di responsabile.
        /// </summary>
        public bool isResponsabile
        {
            get
            {
                if (_isInitialized)
                {
                    _isResponsabile = _cachedIsResponsabile;
                    return _isResponsabile;
                }

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    _isResponsabile = httpContext.User.IsInRole("RESPONSABILE");
                    return _isResponsabile;
                }

                return false;
            }
            set { _isResponsabile = value; }
        }

        private bool _isUserRole;
        /// <summary>
        /// Indica se l'utente ha il ruolo di utente base.
        /// </summary>
        public bool IsUserRole
        {
            get
            {
                if (_isInitialized)
                {
                    _isUserRole = _cachedIsUserRole;
                    return _isUserRole;
                }

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    _isUserRole = httpContext.User.IsInRole("USER");
                    return _isUserRole;
                }

                return false;
            }
            set { _isUserRole = value; }
        }

        private string? _centro;
        /// <summary>
        /// Nome del centro di origine dell'utente.
        /// </summary>
        public string CentroOrigine
        {
            get
            {
                if (_isInitialized)
                    return _cachedCentroOrigine ?? string.Empty;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var claims = httpContext.User.Identities.FirstOrDefault()?.Claims.ToList() ?? new();
                    _centro = claims.FirstOrDefault(x => x.Type.Equals("Centro", StringComparison.OrdinalIgnoreCase))?.Value;
                    return _centro ?? string.Empty;
                }

                return string.Empty;
            }
            set { _centro = value; }
        }

        private string? _siglaCentro;
        /// <summary>
        /// Sigla del centro di origine dell'utente.
        /// </summary>
        public string SiglaCentroOrigine
        {
            get
            {
                if (_isInitialized)
                    return _cachedSiglaCentroOrigine ?? string.Empty;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var claims = httpContext.User.Identities.FirstOrDefault()?.Claims.ToList() ?? new();
                    _siglaCentro = claims.FirstOrDefault(x => x.Type.Equals("Sigla", StringComparison.OrdinalIgnoreCase))?.Value;
                    return _siglaCentro ?? string.Empty;
                }

                return string.Empty;
            }
            set { _siglaCentro = value; }
        }
    }
}
