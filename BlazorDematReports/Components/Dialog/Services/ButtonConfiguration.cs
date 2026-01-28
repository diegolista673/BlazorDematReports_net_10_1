using MudBlazor;

namespace BlazorDematReports.Components.Dialog.Services;

/// <summary>
/// Gestisce la configurazione del pulsante Save.
/// </summary>
public class ButtonConfiguration
{
    public string Text { get; private set; } = "Nessuna Modifica";
    public string Icon { get; private set; } = Icons.Material.Filled.Check;
    public Color Color { get; private set; } = Color.Default;
    public bool IsEnabled { get; private set; }

    public void Update(bool hasUnsavedChanges, bool hasConfiguredTasks, bool isProcessing)
    {
        if (hasUnsavedChanges)
        {
            Text = hasConfiguredTasks ? "Salva Modifiche" : "Conferma";
            Icon = Icons.Material.Filled.Save;
            Color = Color.Success;
            IsEnabled = hasConfiguredTasks && !isProcessing;
        }
        else
        {
            Text = "Nessuna Modifica";
            Icon = Icons.Material.Filled.Check;
            Color = Color.Default;
            IsEnabled = !isProcessing;
        }
    }
}