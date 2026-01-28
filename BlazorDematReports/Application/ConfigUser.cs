namespace BlazorDematReports.Application
{
    /// <summary>
    /// Gestisce le informazioni di configurazione e i dati utente correnti tramite IHttpContextAccessor.
    /// </summary>
    public class ConfigUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ConfigUser> _logger;

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

        private string? _utente;
        /// <summary>
        /// Nome utente corrente autenticato.
        /// </summary>
        public string Utente
        {
            get
            {
                _utente = _httpContextAccessor.HttpContext!.User.Identity!.Name!;
                return _utente;
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
                var claims = _httpContextAccessor!.HttpContext!.User.Identities.First().Claims.ToList();
                var _idUtente = Convert.ToInt32(claims?.FirstOrDefault(x => x.Type.Equals("IdOperatore", StringComparison.OrdinalIgnoreCase))?.Value);
                return _idUtente;
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
                var claims = _httpContextAccessor!.HttpContext!.User.Identities.First().Claims.ToList();
                var _idCentroOrigine = Convert.ToInt32(claims?.FirstOrDefault(x => x.Type.Equals("IdCentro", StringComparison.OrdinalIgnoreCase))?.Value);
                return _idCentroOrigine;
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
                bool res = _httpContextAccessor!.HttpContext!.User.IsInRole("ADMIN");
                if (res)
                {
                    _isAdminRole = true;
                }
                else
                {
                    _isAdminRole = false;
                }
                return _isAdminRole;
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
                bool res = _httpContextAccessor!.HttpContext!.User.IsInRole("SUPERVISOR");
                if (res)
                {
                    _isSupervisorRole = true;
                }
                else
                {
                    _isSupervisorRole = false;
                }
                return _isSupervisorRole;
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
                bool res = _httpContextAccessor!.HttpContext!.User.IsInRole("RESPONSABILE");
                if (res)
                {
                    _isResponsabile = true;
                }
                else
                {
                    _isResponsabile = false;
                }
                return _isResponsabile;
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
                bool res = _httpContextAccessor.HttpContext!.User.IsInRole("USER");
                if (res)
                {
                    _isUserRole = true;
                }
                else
                {
                    _isUserRole = false;
                }
                return _isUserRole;
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
                var claims = _httpContextAccessor!.HttpContext!.User.Identities.First().Claims.ToList();
                var _centro = claims?.FirstOrDefault(x => x.Type.Equals("Centro", StringComparison.OrdinalIgnoreCase))?.Value;
                return _centro!;
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
                var claims = _httpContextAccessor!.HttpContext!.User.Identities.First().Claims.ToList();
                var _siglaCentro = claims?.FirstOrDefault(x => x.Type.Equals("Sigla", StringComparison.OrdinalIgnoreCase))?.Value;
                return _siglaCentro!;
            }
            set { _siglaCentro = value; }
        }
    }
}
