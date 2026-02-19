using Microsoft.AspNetCore.Components;

namespace BlazorDematReports.Services.ProcedureEdit;

/// <summary>
/// Servizio per la gestione della navigazione nelle pagine di modifica procedure.
/// Fornisce metodi per navigare tra tab e sezioni della pagina di edit.
/// </summary>
public class ProcedureNavigationService
{
    private readonly NavigationManager _navigationManager;
    
    public ProcedureNavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }
    
    /// <summary>
    /// Enumerazione dei tipi di tab disponibili nella pagina di edit.
    /// </summary>
    public enum TabType
    {
        Generale = 0,
        Fasi = 1,
        Task = 2,
        Mail = 3,
        Query = 4,
        Monitoring = 5
    }
    
    /// <summary>
    /// Naviga al tab specificato per la procedura indicata.
    /// </summary>
    /// <param name="proceduraId">ID della procedura.</param>
    /// <param name="tab">Tab di destinazione.</param>
    public void NavigateToTab(int proceduraId, TabType tab)
    {
        var tabName = tab.ToString().ToLower();
        var url = $"/procedure-lavorazioni/edit/{proceduraId}/{tabName}";
        _navigationManager.NavigateTo(url);
    }
    
    /// <summary>
    /// Naviga alla sezione di configurazione task con filtro opzionale per fase.
    /// </summary>
    /// <param name="proceduraId">ID della procedura.</param>
    /// <param name="faseId">ID della fase per filtrare i task (opzionale).</param>
    public void NavigateToTaskConfiguration(int proceduraId, int? faseId = null)
    {
        var url = $"/procedure-lavorazioni/edit/{proceduraId}/task";
        if (faseId.HasValue)
        {
            url += $"?fase={faseId}";
        }
        _navigationManager.NavigateTo(url);
    }
    
    /// <summary>
    /// Naviga alla configurazione dei servizi mail per la procedura.
    /// </summary>
    /// <param name="proceduraId">ID della procedura.</param>
    public void NavigateToMailConfiguration(int proceduraId)
    {
        _navigationManager.NavigateTo($"/procedure-lavorazioni/edit/{proceduraId}/mail");
    }
    
    /// <summary>
    /// Ottiene l'indice del tab basato sul nome del tab nell'URL.
    /// </summary>
    /// <param name="tabName">Nome del tab dall'URL.</param>
    /// <returns>Indice del tab (0-based).</returns>
    public int GetTabIndex(string? tabName)
    {
        if (string.IsNullOrEmpty(tabName)) return 0;
        
        return tabName.ToLower() switch
        {
            "generale" => 0,
            "fasi" => 1,
            "task" => 2,
            "mail" => 3,
            "query" => 4,
            "monitoring" => 5,
            _ => 0
        };
    }
    
    /// <summary>
    /// Converte il nome del tab in enum TabType.
    /// </summary>
    /// <param name="tabName">Nome del tab dall'URL.</param>
    /// <returns>Tipo di tab corrispondente.</returns>
    public TabType GetTabType(string? tabName)
    {
        return (TabType)GetTabIndex(tabName);
    }
    
    /// <summary>
    /// Ottiene il nome del tab per l'URL dal tipo di tab.
    /// </summary>
    /// <param name="tab">Tipo di tab.</param>
    /// <returns>Nome del tab per l'URL.</returns>
    public string GetTabName(TabType tab)
    {
        return tab.ToString().ToLower();
    }
    
    /// <summary>
    /// Naviga indietro alla lista delle procedure.
    /// </summary>
    public void NavigateBackToList()
    {
        _navigationManager.NavigateTo("/procedure-lavorazioni");
    }
    
    /// <summary>
    /// Naviga alla dashboard della procedura specifica.
    /// </summary>
    /// <param name="proceduraId">ID della procedura.</param>
    public void NavigateToProcedureDashboard(int proceduraId)
    {
        _navigationManager.NavigateTo($"/procedure-lavorazioni/{proceduraId}/dashboard");
    }
    
    /// <summary>
    /// Ottiene l'URL corrente senza query parameters.
    /// </summary>
    /// <returns>URL base corrente.</returns>
    public string GetCurrentBaseUrl()
    {
        var uri = new Uri(_navigationManager.Uri);
        return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
    }
    
    /// <summary>
    /// Verifica se l'URL corrente corrisponde a una pagina di edit procedure.
    /// </summary>
    /// <returns>True se siamo in una pagina di edit procedure.</returns>
    public bool IsInProcedureEditPage()
    {
        return _navigationManager.Uri.Contains("/procedure-lavorazioni/edit/");
    }
    
    /// <summary>
    /// Estrae l'ID della procedura dall'URL corrente.
    /// </summary>
    /// <returns>ID della procedura se presente nell'URL, altrimenti null.</returns>
    public int? GetCurrentProcedureId()
    {
        var uri = _navigationManager.Uri;
        var match = System.Text.RegularExpressions.Regex.Match(uri, @"/procedure-lavorazioni/edit/(\d+)");
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }
        
        return null;
    }
}