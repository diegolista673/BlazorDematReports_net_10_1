using BlazorDematReports.Core.Services.Wizard;
using Entities.Enums;

namespace BlazorDematReports.Core.Services.Validation;

/// <summary>
/// Validatore per gli step del wizard.
/// Implementa regole di validazione dichiarative per ogni step.
/// </summary>
public interface IValidationRule<T>
{
    ValidationResult Validate(T input);
}

/// <summary>
/// Validatore per lo Step 1: Tipo Fonte
/// </summary>
public class TipoFonteValidationRule : IValidationRule<TipoFonteData?>
{
    public ValidationResult Validate(TipoFonteData? tipoFonte)
    {
        if (!tipoFonte.HasValue)
            return ValidationResult.Error("Seleziona un tipo di fonte dati");

        return ValidationResult.Success("Tipo fonte valido");
    }
}

/// <summary>
/// Validatore per lo Step 2: Configurazione Specifica
/// </summary>
public class ConfigurazioneSpecificaValidationRule : IValidationRule<ConfigurationWizardState>
{
    public ValidationResult Validate(ConfigurationWizardState state)
    {
        return state.TipoFonte switch
        {
            TipoFonteData.SQL => ValidateSql(state),
            TipoFonteData.HandlerIntegrato => ValidateHandler(state),
            _ => ValidationResult.Error("Tipo fonte non riconosciuto")
        };
    }
    
    private ValidationResult ValidateSql(ConfigurationWizardState state)
    {
        if (string.IsNullOrWhiteSpace(state.ConnectionStringName))
            return ValidationResult.Error("Seleziona una connection string");
        
        if (!state.ConnectionTestPassed && !state.IsEditMode)
            return ValidationResult.Error("Esegui e supera il test di connessione");
        
        return ValidationResult.Success("Configurazione SQL valida");
    }

    
    private ValidationResult ValidateHandler(ConfigurationWizardState state)
    {
        if (string.IsNullOrWhiteSpace(state.HandlerClassName))
            return ValidationResult.Error("Seleziona un handler C#");
        
        return ValidationResult.Success("Configurazione Handler valida");
    }
}

/// <summary>
/// Validatore per lo Step 3: Procedura
/// </summary>
public class ProceduraValidationRule : IValidationRule<ConfigurationWizardState>
{
    public ValidationResult Validate(ConfigurationWizardState state)
    {
        if (!state.IdProcedura.HasValue)
            return ValidationResult.Error("Seleziona una procedura di lavorazione");
        
        if (!state.FasiDisponibili.Any())
            return ValidationResult.Warning("Nessuna fase disponibile per questa procedura. Aggiungi fasi prima di continuare.");
        
        return ValidationResult.Success($"Procedura selezionata: {state.NomeProcedura}");
    }
}

/// <summary>
/// Validatore per lo Step 4: Mappings
/// </summary>
public class MappingsValidationRule : IValidationRule<ConfigurationWizardState>
{
    public ValidationResult Validate(ConfigurationWizardState state)
    {
        if (!state.Mappings.Any())
            return ValidationResult.Error("Aggiungi almeno un mapping Fase/Centro");
        
        // Check duplicati (stessa fase + stesso cron)
        var duplicates = state.Mappings
            .GroupBy(m => new { m.IdFaseLavorazione, m.CronExpression })
            .Where(g => g.Count() > 1)
            .ToList();
        
        if (duplicates.Any())
            return ValidationResult.Error("Mapping duplicati rilevati: stessa fase con stesso cron non consentita");
        
        // Verifica query SQL se tipo SQL
        if (state.TipoFonte == TipoFonteData.SQL)
        {
            var mappingsWithoutQuery = state.Mappings
                .Where(m => string.IsNullOrWhiteSpace(m.TestoQueryTask))
                .ToList();
            
            if (mappingsWithoutQuery.Any())
                return ValidationResult.Warning("Alcuni mapping non hanno una query SQL configurata");
        }
        
        return ValidationResult.Success($"{state.Mappings.Count} mapping configurati correttamente");
    }
}

/// <summary>
/// Orchestratore dei validatori per tutti gli step.
/// </summary>
public class ConfigurationStepValidator
{
    private readonly TipoFonteValidationRule _tipoFonteRule = new();
    private readonly ConfigurazioneSpecificaValidationRule _configSpecificaRule = new();
    private readonly ProceduraValidationRule _proceduraRule = new();
    private readonly MappingsValidationRule _mappingsRule = new();
    
    public ValidationResult ValidateStep(int step, ConfigurationWizardState state)
    {
        return step switch
        {
            1 => _tipoFonteRule.Validate(state.TipoFonte),
            2 => _configSpecificaRule.Validate(state),
            3 => _proceduraRule.Validate(state),
            4 => _mappingsRule.Validate(state),
            _ => ValidationResult.Error("Step non valido")
        };
    }
    
    public ValidationResult ValidateAll(ConfigurationWizardState state)
    {
        for (int step = 1; step <= state.TotalSteps; step++)
        {
            var result = ValidateStep(step, state);
            if (!result.IsValid)
                return ValidationResult.Error($"Step {step} non valido: {result.Message}");
        }
        
        return ValidationResult.Success("Tutti gli step validati con successo");
    }
}
