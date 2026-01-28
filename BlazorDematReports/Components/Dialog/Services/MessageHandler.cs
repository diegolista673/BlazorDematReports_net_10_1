using MudBlazor;

namespace BlazorDematReports.Components.Dialog.Services;

/// <summary>
/// Gestisce i messaggi di stato del dialog.
/// </summary>
public class MessageHandler
{
    public string? CurrentMessage { get; private set; }
    public Severity CurrentSeverity { get; private set; }

    public void SetError(string message)
    {
        CurrentMessage = message;
        CurrentSeverity = Severity.Error;
    }

    public void SetSuccess(string message)
    {
        CurrentMessage = message;
        CurrentSeverity = Severity.Success;
    }

    public void ClearMessage() => CurrentMessage = null;
}