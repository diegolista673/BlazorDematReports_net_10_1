using System.Collections.Immutable;
using Entities.Models.DbApplication;
using Entities.Enums;

namespace BlazorDematReports.Services.Wizard;

/// <summary>
/// State immutabile per il wizard di configurazione fonti dati.
/// Gestisce lo stato attraverso tutti gli step del wizard.
/// </summary>
public record ConfigurationWizardState
{
    public int CurrentStep { get; init; } = 1;
    public int TotalSteps { get; init; } = 4;

    // Step 1: Tipo Fonte
    public TipoFonteData? TipoFonte { get; init; } = TipoFonteData.SQL;

    // Step 2: Configurazione Specifica
    public string? ConnectionStringName { get; init; }
    public string? HandlerClassName { get; init; }
    public bool ConnectionTestPassed { get; init; }
    public string? ValidationMessage { get; init; }

    // Step 3: Procedura
    public int? IdProcedura { get; init; }
    public int? IdCentro { get; init; }
    public string? NomeProcedura { get; init; }
    public ImmutableList<FasiLavorazione> FasiDisponibili { get; init; } = ImmutableList<FasiLavorazione>.Empty;

    // Step 4: Mappings
    public ImmutableList<ConfigurazioneFaseCentro> Mappings { get; init; } = ImmutableList<ConfigurazioneFaseCentro>.Empty;
    public string? DescrizioneConfigurazione { get; init; }
    public int GiorniPrecedentiDefault { get; init; } = 10;

    // Edit mode
    public int? IdConfigurazioneEdit { get; init; }
    public string? CodiceConfigurazioneOriginal { get; init; } // Preserva codice originale in edit
    public bool IsEditMode => IdConfigurazioneEdit.HasValue;

    // Progress
    public bool IsStepValid(int step) => step switch
    {
        1 => TipoFonte.HasValue,
        2 => IsStep2Valid(),
        3 => IdProcedura.HasValue && FasiDisponibili.Any(),
        4 => Mappings.Any(),
        _ => false
    };

    private bool IsStep2Valid() => TipoFonte switch
    {
        TipoFonteData.SQL => !string.IsNullOrWhiteSpace(ConnectionStringName) && (ConnectionTestPassed || IsEditMode),
        TipoFonteData.HandlerIntegrato => !string.IsNullOrWhiteSpace(HandlerClassName),
        _ => false
    };

    public bool CanMoveNext => IsStepValid(CurrentStep);
    public bool CanMovePrevious => CurrentStep > 1;
    public bool CanFinish => CurrentStep == TotalSteps && IsStepValid(4);

    // Helper methods per creare nuovi stati
    public ConfigurationWizardState NextStep() 
        => this with { CurrentStep = Math.Min(CurrentStep + 1, TotalSteps) };

    public ConfigurationWizardState PreviousStep() 
        => this with { CurrentStep = Math.Max(CurrentStep - 1, 1) };

    public ConfigurationWizardState WithTipoFonte(TipoFonteData tipoFonte)
    {
        // Se il tipo non cambia, non resettare i campi di step 2
        if (tipoFonte == this.TipoFonte)
            return this;

        // Se il tipo cambia, resetta i campi di step 2
        return this with
        {
            TipoFonte = tipoFonte,
            ConnectionStringName = null,
            HandlerClassName = null,
            ConnectionTestPassed = false
        };
    }
    
    public ConfigurationWizardState WithConnectionString(string? connectionString, bool testPassed = false) 
        => this with 
        { 
            ConnectionStringName = connectionString,
            ConnectionTestPassed = testPassed
        };

    public ConfigurationWizardState WithHandler(string? handler) 
        => this with { HandlerClassName = handler };
    
    public ConfigurationWizardState WithProcedura(int? idProcedura, int? idCentro, string? nome, ImmutableList<FasiLavorazione>? fasi = null) 
        => this with 
        { 
            IdProcedura = idProcedura,
            IdCentro = idCentro,
            NomeProcedura = nome,
            FasiDisponibili = fasi ?? FasiDisponibili,
            // Reset mappings when changing procedure
            Mappings = ImmutableList<ConfigurazioneFaseCentro>.Empty
        };
    
    public ConfigurationWizardState WithMapping(ConfigurazioneFaseCentro mapping) 
        => this with { Mappings = Mappings.Add(mapping) };
    
    public ConfigurationWizardState WithoutMapping(int index) 
        => this with { Mappings = Mappings.RemoveAt(index) };
    
    public ConfigurationWizardState WithMappingUpdate(int index, ConfigurazioneFaseCentro mapping)
        => this with { Mappings = Mappings.SetItem(index, mapping) };
    
    public ConfigurationWizardState WithDescrizione(string? descrizione) 
        => this with { DescrizioneConfigurazione = descrizione };
    
    public ConfigurationWizardState WithValidationMessage(string? message) 
        => this with { ValidationMessage = message };
    
    /// <summary>
    /// Crea una ConfigurazioneFontiDati dal state corrente per il salvataggio.
    /// </summary>
    public ConfigurazioneFontiDati ToConfigurationEntity()
    {
        // Genera codice univoco
        string codiceConfigurazione;
        if (IsEditMode && !string.IsNullOrWhiteSpace(CodiceConfigurazioneOriginal))
        {
            // In edit mode, mantieni il codice originale
            codiceConfigurazione = CodiceConfigurazioneOriginal;
        }
        else if (IsEditMode)
        {
            // Fallback se non c'è il codice originale
            codiceConfigurazione = $"Config{IdProcedura:D4}";
        }
        else
        {
            // Nuovo record: genera codice univoco con timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            codiceConfigurazione = $"Config{IdProcedura:D4}_{timestamp}";
        }
        
        return new ConfigurazioneFontiDati
        {
            IdConfigurazione = IdConfigurazioneEdit ?? 0,
            CodiceConfigurazione = codiceConfigurazione,
            DescrizioneConfigurazione = DescrizioneConfigurazione ?? $"Config_{NomeProcedura}",
            TipoFonte = TipoFonte ?? TipoFonteData.SQL,
            ConnectionStringName = ConnectionStringName,
            HandlerClassName = HandlerClassName,
            CreatoIl = DateTime.Now,
            ModificatoIl = IsEditMode ? DateTime.Now : null
        };
    }
}

/// <summary>
/// Servizio per gestire lo stato del wizard.
/// Mantiene lo stato corrente e notifica i componenti dei cambiamenti.
/// </summary>
public class ConfigurationWizardStateService
{
    private ConfigurationWizardState _state = new();
    
    public event Action? OnStateChanged;
    
    public ConfigurationWizardState State => _state;
    
    public void UpdateState(Func<ConfigurationWizardState, ConfigurationWizardState> updateFunc)
    {
        _state = updateFunc(_state);
        NotifyStateChanged();
    }
    
    public void Reset()
    {
        _state = new ConfigurationWizardState();
        NotifyStateChanged();
    }
    
    public void LoadEditState(ConfigurazioneFontiDati config, List<ConfigurazioneFaseCentro> mappings, int? idProcedura = null, int? idCentro = null, string? nomeProcedura = null, List<FasiLavorazione>? fasi = null)
    {
        _state = new ConfigurationWizardState
        {
            IdConfigurazioneEdit = config.IdConfigurazione,
            CodiceConfigurazioneOriginal = config.CodiceConfigurazione,
            TipoFonte = config.TipoFonte,
            ConnectionStringName = config.ConnectionStringName,
            HandlerClassName = config.HandlerClassName,
            ConnectionTestPassed = config.TipoFonte == TipoFonteData.SQL && !string.IsNullOrWhiteSpace(config.ConnectionStringName),
            DescrizioneConfigurazione = config.DescrizioneConfigurazione,
            Mappings = mappings.ToImmutableList(),
            IdProcedura = idProcedura,
            IdCentro = idCentro,
            NomeProcedura = nomeProcedura,
            FasiDisponibili = fasi?.ToImmutableList() ?? ImmutableList<FasiLavorazione>.Empty
        };
        NotifyStateChanged();
    }
    
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
