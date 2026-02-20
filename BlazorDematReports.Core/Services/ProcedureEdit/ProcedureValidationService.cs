
using BlazorDematReports.Core.Application.Dto;

namespace BlazorDematReports.Core.Services.ProcedureEdit;

/// <summary>
/// Servizio per la validazione delle procedure di lavorazione durante la modifica.
/// Fornisce validazioni cross-tab e controlli di integrità dei dati.
/// </summary>
public class ProcedureValidationService
{
    /// <summary>
    /// Risultato della validazione con dettagli degli errori.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }



    /// <summary>
    /// Valida solo le informazioni generali della procedura.
    /// </summary>
    /// <param name="procedura">Procedura da validare.</param>
    /// <returns>Risultato della validazione.</returns>
    public ValidationResult ValidateGeneralInfo(ProcedureLavorazioniDto procedura)
    {
        var result = new ValidationResult { IsValid = true };

        ValidateRequiredFields(procedura, result);

        result.IsValid = result.Errors.Count == 0;
        return result;
    }



    /// <summary>
    /// Valida i campi obbligatori della procedura.
    /// </summary>
    private void ValidateRequiredFields(ProcedureLavorazioniDto procedura, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(procedura.NomeProcedura))
        {
            result.AddError("Il nome della procedura è obbligatorio");
        }

        if (procedura.Idcentro == null || procedura.Idcentro <= 0)
        {
            result.AddError("Il centro è obbligatorio");
        }

        if (string.IsNullOrWhiteSpace(procedura.FormatoDatiProduzione))
        {
            result.AddError("Il formato dati produzione è obbligatorio");
        }

        if (string.IsNullOrWhiteSpace(procedura.Reparto))
        {
            result.AddError("Il reparto è obbligatorio");
        }

        // Validazione lunghezza campi
        if (procedura.NomeProcedura?.Length > 100)
        {
            result.AddError("Il nome della procedura non può superare i 100 caratteri");
        }

        if (procedura.Note?.Length > 500)
        {
            result.AddWarning("Le note sono molto lunghe (oltre 500 caratteri)");
        }
    }



}