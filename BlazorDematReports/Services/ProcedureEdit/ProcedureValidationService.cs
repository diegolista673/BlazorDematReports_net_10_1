using DataReading.Dto;
using BlazorDematReports.Dto;
using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Services.ProcedureEdit;

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
    /// Valida l'intera procedura di lavorazione.
    /// </summary>
    /// <param name="procedura">Procedura da validare.</param>
    /// <returns>Risultato della validazione.</returns>
    public ValidationResult ValidateProcedure(ProcedureLavorazioniDto procedura)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validazione campi obbligatori
        ValidateRequiredFields(procedura, result);
        
        // Validazione fasi
        ValidateFasi(procedura, result);
        
        // Validazione task
        ValidateTask(procedura, result);
        
        // Validazione servizi mail
        ValidateMailServices(procedura, result);
        
        // Validazione query
        ValidateQueries(procedura, result);
        
        result.IsValid = result.Errors.Count == 0;
        return result;
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
    /// Valida la configurazione dei task per una fase specifica.
    /// </summary>
    /// <param name="fase">Fase da validare.</param>
    /// <returns>Risultato della validazione.</returns>
    public ValidationResult ValidatePhaseTask(LavorazioniFasiDataReadingDto fase)
    {
        var result = new ValidationResult { IsValid = true };
        
        if (fase.TaskDaEseguireDto != null)
        {
            foreach (var task in fase.TaskDaEseguireDto)
            {
                ValidateSingleTask(task, result);
            }
            
            // Verifica duplicati nella stessa fase
            CheckForDuplicateTasksInPhase(fase, result);
        }
        
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
    
    /// <summary>
    /// Valida la configurazione delle fasi.
    /// </summary>
    private void ValidateFasi(ProcedureLavorazioniDto procedura, ValidationResult result)
    {
        if (procedura.LavorazioniFasiDataReadingsDto == null || !procedura.LavorazioniFasiDataReadingsDto.Any())
        {
            result.AddWarning("Nessuna fase configurata per questa procedura");
            return;
        }
        
        // Verifica che ci sia massimo una fase con grafico documenti
        var fasiConGrafico = procedura.LavorazioniFasiDataReadingsDto
            .Where(f => f.FlagGraficoDocumenti == true)
            .Count();
        
        if (fasiConGrafico > 1)
        {
            result.AddError("È consentita una sola fase nel grafico documenti");
        }
        
        // Verifica nomi fasi duplicati
        var nomiDuplicati = procedura.LavorazioniFasiDataReadingsDto
            .GroupBy(f => f.FaseLavorazione)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var nome in nomiDuplicati)
        {
            result.AddError($"Fase duplicata: '{nome}'");
        }
    }
    
    /// <summary>
    /// Valida la configurazione dei task.
    /// </summary>
    private void ValidateTask(ProcedureLavorazioniDto procedura, ValidationResult result)
    {
        if (procedura.LavorazioniFasiDataReadingsDto == null) return;
        
        foreach (var fase in procedura.LavorazioniFasiDataReadingsDto)
        {
            if (fase.TaskDaEseguireDto != null)
            {
                foreach (var task in fase.TaskDaEseguireDto)
                {
                    ValidateSingleTask(task, result);
                }
            }
        }
    }
    
    /// <summary>
    /// Valida un singolo task.
    /// </summary>
    private void ValidateSingleTask(TaskDaEseguireDto task, ValidationResult result)
    {
        if (task.IdTask == null || task.IdTask <= 0)
        {
            result.AddError("Tipo task non valido");
        }
        
        if (task.GiorniPrecedenti < 1 || task.GiorniPrecedenti > 31)
        {
            result.AddError("I giorni precedenti devono essere tra 1 e 31");
        }
        
        // Validazione task temporizzati
        if (task.IdTask == 2 && task.TimeTask == null)
        {
            result.AddError("L'orario è obbligatorio per i task temporizzati");
        }
        
        // Validazione configurazione unificata
        if (!task.IdConfigurazioneDatabase.HasValue)
        {
            result.AddError("Il task deve avere una configurazione fonti dati associata (IdConfigurazioneDatabase)");
        }
    }
    
    /// <summary>
    /// Verifica duplicati di task nella stessa fase.
    /// </summary>
    private void CheckForDuplicateTasksInPhase(LavorazioniFasiDataReadingDto fase, ValidationResult result)
    {
        if (fase.TaskDaEseguireDto == null) return;
        
        var duplicates = fase.TaskDaEseguireDto
            .GroupBy(t => new { t.IdTask, t.TimeTask, t.IdConfigurazioneDatabase })
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicate in duplicates)
        {
            var taskName = duplicate.IdTask switch
            {
                1 => "Giornaliero",
                2 => "Temporizzato",
                3 => "Mensile",
                _ => "Sconosciuto"
            };
            
            result.AddError($"Task duplicato nella fase '{fase.FaseLavorazione}': {taskName}");
        }
    }
    
    /// <summary>
    /// Valida la configurazione dei servizi mail.
    /// </summary>
    private void ValidateMailServices(ProcedureLavorazioniDto procedura, ValidationResult result)
    {
        var procedurePhase = procedura.LavorazioniFasiDataReadingsDto?
            .FirstOrDefault(f => f.IdFaseLavorazione == 0);
        
        if (procedurePhase?.TaskDaEseguireDto != null)
        {
            var mailServices = procedurePhase.TaskDaEseguireDto
                .Where(t => t.IdConfigurazioneDatabase.HasValue)
                .ToList();
            
            // Verifica configurazione servizi mail
            foreach (var service in mailServices)
            {
                if (service.IdTask == 2 && service.TimeTask == null)
                {
                    result.AddError($"Configurazione ID '{service.IdConfigurazioneDatabase}': orario obbligatorio per task temporizzati");
                }
            }
        }
    }
    
    /// <summary>
    /// Valida la configurazione delle query.
    /// </summary>
    private void ValidateQueries(ProcedureLavorazioniDto procedura, ValidationResult result)
    {
        if (procedura.QueryProcedureLavorazioniDto != null)
        {
            foreach (var query in procedura.QueryProcedureLavorazioniDto)
            {
                if (string.IsNullOrWhiteSpace(query.Titolo))
                {
                    result.AddError("Titolo query obbligatorio");
                }
                
                if (string.IsNullOrWhiteSpace(query.Descrizione))
                {
                    result.AddError("Testo query obbligatorio");
                }
                
                // Nota: Connessione ora gestita in ConfigurazioneFontiDati
            }
        }
    }
}