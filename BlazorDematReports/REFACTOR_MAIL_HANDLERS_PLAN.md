# Piano di Refactoring: Unificazione Handler Mail

## Obiettivo
Eliminare la logica separata per gli handler mail (MailImportHandler, MailImportHandlerWrapper) e trattarli come handler standard (`ILavorazioneHandler`), riducendo duplicazione e complessità.

---

## Analisi Situazione Attuale

### File Coinvolti
1. **ClassLibraryLavorazioni/LavorazioniViaMail/**
   - `Interfaces/IMailImportHandler.cs` - Interfaccia specifica mail
   - `Interfaces/IMailImportService.cs` - Servizio mail import
   - Handler concreti (HERA16Handler, ADER4Handler, etc.)

2. **ClassLibraryLavorazioni/Shared/Wrappers/**
   - `MailImportHandlerWrapper.cs` - Wrapper specifico per mail
   - `LavorazioneHandlerWrapper.cs` - Wrapper generico

3. **ClassLibraryLavorazioni/Shared/Handlers/**
   - `UnifiedDataSourceHandler.cs` - Handler unificato esistente

4. **BlazorDematReports/Services/**
   - `ServiceMail.cs` - Servizio che filtra configurazioni mail

### Logica Attuale
```csharp
// IMailImportHandler (specifico)
public interface IMailImportHandler
{
    string MailServiceCode { get; }
    Task<DataTable> ImportFromEmailAsync(DateTime startDate, DateTime endDate);
}

// ILavorazioneHandler (generico)
public interface ILavorazioneHandler
{
    Task<DataTable> ExecuteAsync(DateTime startDate, DateTime endDate, string? parameters = null);
}
```

**Problema**: Due interfacce separate creano duplicazione e complessità nella gestione.

---

## Piano di Refactoring (7 Step)

### STEP 1: Analisi Dipendenze
**Obiettivo**: Mappare tutti i punti dove `IMailImportHandler` è utilizzato.

**Azioni**:
- [ ] Cercare tutti i riferimenti a `IMailImportHandler`
- [ ] Cercare tutti i riferimenti a `MailImportHandlerWrapper`
- [ ] Identificare codice che dipende da `MailServiceCode`
- [ ] Documentare dependency graph

**Output**: File `MAIL_HANDLER_DEPENDENCIES.md` con lista completa dipendenze

---

### STEP 2: Estendere ILavorazioneHandler con Metadata
**Obiettivo**: Permettere agli handler di esporre metadati opzionali (es. codice servizio).

**Modifiche**:

#### ClassLibraryLavorazioni/Lavorazioni/Interfaces/ILavorazioneHandler.cs
```csharp
public interface ILavorazioneHandler
{
    Task<DataTable> ExecuteAsync(DateTime startDate, DateTime endDate, string? parameters = null);
    
    // NUOVO: Metadata opzionali
    string? GetServiceCode() => null;  // Default: nessun codice
    HandlerMetadata GetMetadata() => new(); // Default: metadata vuoto
}

public record HandlerMetadata
{
    public string? ServiceCode { get; init; }
    public bool RequiresEmailService { get; init; }
    public Dictionary<string, string>? AdditionalProperties { get; init; }
}
```

**Benefici**:
- Handler mail possono esporre `ServiceCode` senza interfaccia separata
- Backward compatible (metodi con default implementation)
- Estensibile per futuri metadati

---

### STEP 3: Migrare Handler Mail Concreti
**Obiettivo**: Convertire handler mail da `IMailImportHandler` a `ILavorazioneHandler`.

#### Esempio: HERA16Handler (prima)
```csharp
public class HERA16Handler : IMailImportHandler
{
    public string MailServiceCode => "HERA16";
    
    public async Task<DataTable> ImportFromEmailAsync(DateTime startDate, DateTime endDate)
    {
        // logica import
    }
}
```

#### Esempio: HERA16Handler (dopo)
```csharp
[HandlerCode("HERA16")]
[Description("Import dati HERA16 da allegati email")]
public class HERA16Handler : ILavorazioneHandler
{
    private readonly IMailImportService _mailService;
    
    public HERA16Handler(IMailImportService mailService)
    {
        _mailService = mailService;
    }
    
    public string? GetServiceCode() => "HERA16";
    
    public HandlerMetadata GetMetadata() => new()
    {
        ServiceCode = "HERA16",
        RequiresEmailService = true
    };
    
    public async Task<DataTable> ExecuteAsync(DateTime startDate, DateTime endDate, string? parameters = null)
    {
        // Stessa logica di prima, ma con firma standard
        return await _mailService.ImportDataAsync("HERA16", startDate, endDate);
    }
}
```

**Azioni per ogni handler mail**:
- [ ] HERA16Handler
- [ ] ADER4Handler
- [ ] Altri handler mail esistenti

---

### STEP 4: Eliminare MailImportHandlerWrapper
**Obiettivo**: Usare solo `LavorazioneHandlerWrapper` per tutti gli handler.

**Modifiche**:

#### LavorazioneHandlerWrapper.cs (estendere)
```csharp
public class LavorazioneHandlerWrapper
{
    private readonly ILavorazioneHandler _handler;
    private readonly ILogger<LavorazioneHandlerWrapper> _logger;
    
    // NUOVO: Supporto metadata
    public string? ServiceCode => _handler.GetServiceCode();
    public HandlerMetadata Metadata => _handler.GetMetadata();
    
    public async Task<DataTable> ExecuteAsync(DateTime startDate, DateTime endDate, string? parameters = null)
    {
        try
        {
            _logger.LogInformation("Esecuzione handler {HandlerType}, ServiceCode: {ServiceCode}", 
                _handler.GetType().Name, ServiceCode);
                
            return await _handler.ExecuteAsync(startDate, endDate, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore esecuzione handler {HandlerType}", _handler.GetType().Name);
            throw;
        }
    }
}
```

**Da eliminare**:
- [ ] `MailImportHandlerWrapper.cs`
- [ ] Tutti i riferimenti a `MailImportHandlerWrapper`

---

### STEP 5: Aggiornare HandlerDiscoveryService
**Obiettivo**: Discovery automatico riconosce handler mail tramite metadata.

#### HandlerDiscoveryService.cs (modificare)
```csharp
public class HandlerDiscoveryService
{
    private static IReadOnlyList<HandlerInfo> DiscoverHandlers()
    {
        var handlers = new List<HandlerInfo>();
        var assembly = typeof(ILavorazioneHandler).Assembly;

        var handlerTypes = assembly.GetTypes()
            .Where(t => typeof(ILavorazioneHandler).IsAssignableFrom(t)
                     && t.IsClass && !t.IsAbstract && t.IsPublic);

        foreach (var type in handlerTypes)
        {
            var handler = CreateHandlerInstance(type);
            var metadata = handler?.GetMetadata() ?? new HandlerMetadata();
            
            handlers.Add(new HandlerInfo
            {
                ClassName = type.Name,
                FullTypeName = type.FullName!,
                Code = GetHandlerCode(type),
                Description = GetHandlerDescription(type),
                HandlerType = type,
                
                // NUOVO: Metadata
                ServiceCode = metadata.ServiceCode,
                RequiresEmailService = metadata.RequiresEmailService,
                Metadata = metadata
            });
        }

        return handlers.OrderBy(h => h.ClassName).ToList().AsReadOnly();
    }
}

public class HandlerInfo
{
    public string ClassName { get; init; } = null!;
    public string? Code { get; init; }
    public string? Description { get; init; }
    
    // NUOVO
    public string? ServiceCode { get; init; }
    public bool RequiresEmailService { get; init; }
    public HandlerMetadata? Metadata { get; init; }
}
```

**Benefici**:
- UI può mostrare icona email per handler che richiedono servizio mail
- Filtro automatico handler mail senza logica separata

---

### STEP 6: Aggiornare UI Wizard
**Obiettivo**: Mostrare handler mail con indicatore visivo.

#### Step2_ConfigurazioneSpecifica.razor (già fatto parzialmente)
```razor
<MudSelect @bind-Value="@_handler" Label="Handler C#">
    @foreach (var handler in _availableHandlers)
    {
        <MudSelectItem Value="@handler.ClassName">
            <div class="d-flex align-items-center gap-2">
                @if (handler.RequiresEmailService)
                {
                    <MudIcon Icon="@Icons.Material.Filled.Email" Size="Size.Small" Color="Color.Secondary" />
                }
                else
                {
                    <MudIcon Icon="@Icons.Material.Filled.Code" Size="Size.Small" />
                }
                <div>
                    <MudText Typo="Typo.body1">@handler.Code</MudText>
                    <MudText Typo="Typo.body2" Class="text-muted">@handler.Description</MudText>
                    @if (!string.IsNullOrWhiteSpace(handler.ServiceCode))
                    {
                        <MudChip T="string" Size="Size.Small" Color="Color.Info">@handler.ServiceCode</MudChip>
                    }
                </div>
            </div>
        </MudSelectItem>
    }
</MudSelect>
```

**Campo codice servizio**:
- Auto-popolato se handler ha `ServiceCode`
- Read-only se auto-popolato
- Editabile solo per handler senza codice predefinito

---

### STEP 7: Cleanup e Rimozione Codice Obsoleto
**Obiettivo**: Eliminare interfacce, wrapper e servizi mail-specifici.

#### File da eliminare:
- [ ] `ClassLibraryLavorazioni/LavorazioniViaMail/Interfaces/IMailImportHandler.cs`
- [ ] `ClassLibraryLavorazioni/Shared/Wrappers/MailImportHandlerWrapper.cs`
- [ ] Eventuali factory specifiche per handler mail

#### File da aggiornare:
- [ ] `ServiceMail.cs` - Mantenere solo metodi invio email, non discovery handler
- [ ] `ServiceConfigurazioneFontiDati.cs` - Usare `HandlerClassName` uniformemente
- [ ] Rimuovere branch `TipoFonte == "EmailCSV"` da validatori residui

#### Aggiornare documentazione:
- [ ] `copilot-instructions.md` - Rimuovere riferimenti a mail handler separati
- [ ] `NOTE.txt` - Aggiornare sezione handler
- [ ] `REFACTOR_HANDLERS.md` - Marcare come completato

---

## Ordine di Esecuzione

### Fase 1: Preparazione (STEP 1-2)
1. Analizzare dipendenze
2. Estendere `ILavorazioneHandler` con metadata
3. **CHECKPOINT**: Build senza errori, nessun breaking change

### Fase 2: Migrazione (STEP 3-4)
1. Convertire handler mail uno per uno
2. Testare ogni handler dopo conversione
3. Eliminare `MailImportHandlerWrapper`
4. **CHECKPOINT**: Tutti gli handler funzionano, test passano

### Fase 3: Discovery e UI (STEP 5-6)
1. Aggiornare `HandlerDiscoveryService`
2. Migliorare UI wizard con icone/indicatori
3. **CHECKPOINT**: Wizard mostra correttamente handler mail

### Fase 4: Cleanup (STEP 7)
1. Eliminare file obsoleti
2. Aggiornare documentazione
3. **CHECKPOINT**: Build pulita, nessun warning

---

## Rischi e Mitigazioni

### Rischio 1: Breaking Changes per Handler Esistenti
**Mitigazione**: 
- Usare default implementation nei metodi `ILavorazioneHandler`
- Conversione graduale handler per handler
- Test unitari per ogni handler migrato

### Rischio 2: Configurazioni Mail Esistenti nel DB
**Mitigazione**:
- Query `MailConfigurationQuery` già supporta entrambi i tipi
- Nessuna migrazione DB necessaria
- Backward compatibility garantita

### Rischio 3: Perdita Funzionalità Mail-Specific
**Mitigazione**:
- `HandlerMetadata` cattura tutte le proprietà necessarie
- `IMailImportService` rimane disponibile per DI
- Logica mail import preservata

---

## Benefici Attesi

1. **Riduzione Complessità**
   - 1 interfaccia invece di 2
   - 1 wrapper invece di 2
   - Logica discovery unificata

2. **Manutenibilità**
   - Meno codice duplicato
   - Pattern uniforme per tutti gli handler
   - Testing semplificato

3. **Estensibilità**
   - Nuovi handler mail seguono stesso pattern
   - Metadata system estensibile
   - Discovery automatico

4. **User Experience**
   - UI consistente per tutti gli handler
   - Indicatori visivi per tipo handler
   - Meno confusione tra tipi fonte

---

## Criteri di Successo

- [ ] Tutti gli handler mail convertiti a `ILavorazioneHandler`
- [ ] `MailImportHandlerWrapper` eliminato
- [ ] `IMailImportHandler` eliminato
- [ ] UI wizard mostra handler mail con icone
- [ ] `dotnet build` senza errori/warning
- [ ] Test manuali: creazione config mail tramite handler
- [ ] Test manuali: esecuzione task mail esistenti
- [ ] Documentazione aggiornata

---

## Timeline Stimata

- **Fase 1**: 2-3 ore (analisi + estensione interfaccia)
- **Fase 2**: 4-6 ore (migrazione handler + eliminazione wrapper)
- **Fase 3**: 2-3 ore (discovery + UI)
- **Fase 4**: 1-2 ore (cleanup + documentazione)

**Totale**: 9-14 ore di sviluppo

---

## Note Implementazione

### Compatibilità Backward
- Configurazioni esistenti con `TipoFonte = "EmailCSV"` continuano a funzionare
- Query `MailConfigurationQuery` filtra sia vecchie che nuove configurazioni
- Nessuna migrazione dati richiesta

### Testing
- Test unitari per ogni handler migrato
- Test integrazione wizard
- Test esecuzione task mail
- Test discovery handler

### Rollback Plan
- Git branch dedicato per refactoring
- Commit granulari per ogni step
- Possibilità di rollback a step intermedi
- Backup configurazioni DB prima del deploy

---

## Prossimi Passi

1. **Approvazione piano**: Review con team/stakeholder
2. **Creazione branch**: `feature/unify-mail-handlers`
3. **Esecuzione STEP 1**: Analisi dipendenze
4. **Iterazione**: Un step alla volta con checkpoint

---

**Documento creato**: 2025-01-XX  
**Ultima modifica**: 2025-01-XX  
**Stato**: PIANIFICAZIONE
