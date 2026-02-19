using BlazorDematReports.Core.Application.Dto;
using BlazorDematReports.Core.DataReading.Dto;
using System.Text.Json;

namespace BlazorDematReports.Core.Services.ProcedureEdit;

/// <summary>
/// Servizio per la gestione dello stato delle modifiche nelle procedure di lavorazione.
/// Traccia le modifiche non salvate e fornisce funzionalità di reset.
/// </summary>
public class ProcedureEditStateService : IDisposable
{
    private ProcedureLavorazioniDto? _originalModel;
    private ProcedureLavorazioniDto? _currentModel;
    
    /// <summary>
    /// Evento scatenato quando cambia lo stato delle modifiche non salvate.
    /// </summary>
    public event EventHandler<bool>? UnsavedChangesChanged;
    
    /// <summary>
    /// Indica se ci sono modifiche non salvate.
    /// </summary>
    public bool HasUnsavedChanges { get; private set; }
    
    /// <summary>
    /// Inizializza lo stato del servizio con il modello specificato.
    /// </summary>
    /// <param name="model">Modello da tracciare per le modifiche.</param>
    public void InitializeState(ProcedureLavorazioniDto model)
    {
        _originalModel = CloneModel(model);
        _currentModel = model;
        HasUnsavedChanges = false;
    }
    
    /// <summary>
    /// Rileva se ci sono state modifiche rispetto al modello originale.
    /// </summary>
    public void DetectChanges()
    {
        if (_originalModel == null || _currentModel == null) return;
        
        var hasChanges = !ModelsAreEqual(_originalModel, _currentModel);
        if (hasChanges != HasUnsavedChanges)
        {
            HasUnsavedChanges = hasChanges;
            UnsavedChangesChanged?.Invoke(this, HasUnsavedChanges);
        }
    }
    
    /// <summary>
    /// Marca lo stato corrente come salvato, aggiornando il modello di riferimento.
    /// </summary>
    public void MarkAsSaved()
    {
        if (_currentModel != null)
        {
            _originalModel = CloneModel(_currentModel);
        }
        HasUnsavedChanges = false;
        UnsavedChangesChanged?.Invoke(this, false);
    }
    
    /// <summary>
    /// Ripristina il modello corrente ai valori originali.
    /// </summary>
    public void ResetToOriginal()
    {
        if (_originalModel != null && _currentModel != null)
        {
            CopyModelValues(_originalModel, _currentModel);
            HasUnsavedChanges = false;
            UnsavedChangesChanged?.Invoke(this, false);
        }
    }
    
    /// <summary>
    /// Crea una copia profonda del modello utilizzando la serializzazione JSON.
    /// </summary>
    private ProcedureLavorazioniDto CloneModel(ProcedureLavorazioniDto model)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json = JsonSerializer.Serialize(model, options);
            return JsonSerializer.Deserialize<ProcedureLavorazioniDto>(json, options) ?? new();
        }
        catch
        {
            // Fallback a copia manuale se la serializzazione fallisce
            return new ProcedureLavorazioniDto
            {
                IdproceduraLavorazione = model.IdproceduraLavorazione,
                NomeProcedura = model.NomeProcedura,
                Note = model.Note,
                FormatoDatiProduzione = model.FormatoDatiProduzione,
                Reparto = model.Reparto,
                LogoBase64 = model.LogoBase64,
                Centro = model.Centro,
                Idcentro = model.Idcentro,
                DataInserimento = model.DataInserimento,
                LavorazioniFasiDataReadingsDto = model.LavorazioniFasiDataReadingsDto?.ToList()
            };
        }
    }
    
    /// <summary>
    /// Confronta due modelli per determinare se sono uguali.
    /// </summary>
    private bool ModelsAreEqual(ProcedureLavorazioniDto model1, ProcedureLavorazioniDto model2)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            var json1 = JsonSerializer.Serialize(model1, options);
            var json2 = JsonSerializer.Serialize(model2, options);
            return json1 == json2;
        }
        catch
        {
            // Fallback a confronto proprietà base
            return model1.NomeProcedura == model2.NomeProcedura &&
                   model1.Note == model2.Note &&
                   model1.FormatoDatiProduzione == model2.FormatoDatiProduzione &&
                   model1.Reparto == model2.Reparto &&
                   model1.LogoBase64 == model2.LogoBase64;
        }
    }
    
    /// <summary>
    /// Copia i valori dal modello sorgente al modello target.
    /// </summary>
    private void CopyModelValues(ProcedureLavorazioniDto source, ProcedureLavorazioniDto target)
    {
        target.NomeProcedura = source.NomeProcedura;
        target.Note = source.Note;
        target.FormatoDatiProduzione = source.FormatoDatiProduzione;
        target.Reparto = source.Reparto;
        target.LogoBase64 = source.LogoBase64;
        target.Centro = source.Centro;
        target.Idcentro = source.Idcentro;
        target.DataInserimento = source.DataInserimento;
        
        // Copia anche le collezioni se necessario
        if (source.LavorazioniFasiDataReadingsDto != null)
        {
            target.LavorazioniFasiDataReadingsDto = source.LavorazioniFasiDataReadingsDto.ToList();
        }
        
        if (source.QueryProcedureLavorazioniDto != null)
        {
            target.QueryProcedureLavorazioniDto = source.QueryProcedureLavorazioniDto.ToList();
        }
    }
    
    /// <summary>
    /// Libera le risorse utilizzate dal servizio.
    /// </summary>
    public void Dispose()
    {
        UnsavedChangesChanged = null;
        _originalModel = null;
        _currentModel = null;
    }
}