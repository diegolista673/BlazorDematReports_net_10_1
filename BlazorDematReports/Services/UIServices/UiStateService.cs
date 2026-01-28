using MudBlazor;

namespace BlazorDematReports.Services.UIServices
{
    /// <summary>
    /// Servizio per la gestione dello stato UI globale, come overlay, menu e snackbar.
    /// </summary>
    public class UiStateService
    {
        // Overlay
        /// <summary>
        /// Evento scatenato quando cambia la visibilità dell'overlay.
        /// </summary>
        public event Action<bool, string?>? OnOverlayChanged;
        private bool _isOverlayVisible;
        private string? _overlayMessage;


        /// <summary>
        /// Indica se l'overlay è visibile.
        /// </summary>
        public bool IsOverlayVisible => _isOverlayVisible;
        /// <summary>
        /// Messaggio visualizzato nell'overlay.
        /// </summary>
        public string? OverlayMessage => _overlayMessage;

        /// <summary>
        /// Mostra l'overlay con un messaggio opzionale.
        /// </summary>
        /// <param name="message">Messaggio da visualizzare.</param>
        public void ShowOverlay(string? message = null)
        {
            _isOverlayVisible = true;
            _overlayMessage = message;
            OnOverlayChanged?.Invoke(_isOverlayVisible, _overlayMessage);
        }

        /// <summary>
        /// Nasconde l'overlay e cancella il messaggio.
        /// </summary>
        public void HideOverlay()
        {
            _isOverlayVisible = false;
            _overlayMessage = null;
            OnOverlayChanged?.Invoke(_isOverlayVisible, _overlayMessage);
        }

        // Menu
        /// <summary>
        /// Evento scatenato quando cambia lo stato di abilitazione del menu.
        /// </summary>
        public event Action<bool>? OnMenuDisabledChanged;
        private bool _isMenuDisabled;
        /// <summary>
        /// Indica se il menu è disabilitato.
        /// </summary>
        public bool IsMenuDisabled => _isMenuDisabled;

        /// <summary>
        /// Imposta lo stato di abilitazione del menu e scatena l'evento OnMenuDisabledChanged.
        /// </summary>
        /// <param name="disabled">True per disabilitare il menu, false per abilitarlo.</param>
        public void SetMenuDisabled(bool disabled)
        {
            _isMenuDisabled = disabled;
            OnMenuDisabledChanged?.Invoke(_isMenuDisabled);
        }

        /// <summary>
        /// Evento scatenato per mostrare uno snackbar.
        /// </summary>
        public event Action<string, Severity>? OnSnackbarRequested;

        /// <summary>
        /// Mostra uno snackbar con il messaggio e la severità specificata.
        /// </summary>
        /// <param name="message">Messaggio da visualizzare.</param>
        /// <param name="severity">Severità dello snackbar.</param>
        public void ShowSnackbar(string message, Severity severity = Severity.Normal)
        {
            OnSnackbarRequested?.Invoke(message, severity);
        }

        // Overlay Edit
        private bool _isVisibleEditing;
        private string? _textEditOverlay;

        /// <summary>
        /// Indica se l'overlay di editing è visibile.
        /// </summary>
        public bool IsVisibleEditing => _isVisibleEditing;
        /// <summary>
        /// Messaggio visualizzato nell'overlay di editing.
        /// </summary>
        public string? TextEditOverlay => _textEditOverlay;

        /// <summary>
        /// Evento scatenato quando cambia la visibilità dell'overlay di editing.
        /// </summary>
        public event Action<bool, string?>? OnEditOverlayChanged;

        /// <summary>
        /// Mostra l'overlay di editing con un messaggio opzionale.
        /// </summary>
        /// <param name="message">Messaggio da visualizzare.</param>
        public void ShowEditOverlay(string? message = null)
        {
            _isVisibleEditing = true;
            _textEditOverlay = message;
            OnEditOverlayChanged?.Invoke(_isVisibleEditing, _textEditOverlay);
        }

        /// <summary>
        /// Nasconde l'overlay di editing e cancella il messaggio.
        /// </summary>
        public void HideEditOverlay()
        {
            _isVisibleEditing = false;
            _textEditOverlay = null;
            OnEditOverlayChanged?.Invoke(_isVisibleEditing, _textEditOverlay);
        }
    }
}
