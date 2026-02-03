# GitHub Copilot Instructions - BlazorDematReports

**Project**: BlazorDematReports  
**Framework**: Blazor Server  
**.NET Version**: 10.0  
**C# Version**: 14.0

---

## 🎯 Project Overview

BlazorDematReports is a data configuration management system for production workflows. 
The project manages data sources, scheduling, and task automation through a Blazor Server interface with Material Design (MudBlazor) components.

### Core Entities
- **ConfigurazioneFontiDati**: Data source configurations (SQL, Email CSV, C# Handler, Pipeline)
- **ConfigurazioneFaseCentro**: Phase/Center mappings for task scheduling
- **ProcedureLavorazioni**: Work procedures with associated centers
- **FasiLavorazione**: Work phases
- **CentriLavorazione**: Work centers

---

## 📋 Code Conventions

### Naming
- **Private fields**: `_camelCase`
- **Public properties**: `PascalCase`
- **Async methods**: Suffix `Async`
- **Event handlers**: `On{Event}{Action}Async`
- **Pages**: `Page{FeatureName}.razor`
- **Services**: `{Feature}Service.cs`

### File Structure
```
BlazorDematReports/
├── Components/Pages/Admin/
│   └── Page*.razor           # Admin pages
├── Services/
│   ├── Validation/
│   │   └── SqlValidationService.cs
│   └── {Feature}Service.cs
├── Models/ or Entities/
└── appsettings.json
---

## 🛠️ Technology Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor Server |
| UI Components | MudBlazor |
| Database | Entity Framework Core + SQL Server |
| Configuration | IConfiguration |
| Dependency Injection | Constructor injection |
| Validation | Custom SqlValidationService |

---

## 🔧 Common Patterns

### Database Access (REQUIRED)
```csharp
await using var context = await DbFactory.CreateDbContextAsync();
var data = await context.EntitySet
    .Include(e => e.NavigationProperty)
    .ToListAsync();
```

### Async Operations with Loading State
```csharp
_loading = true;
try
{
    // async operation
    await DoSomethingAsync();
    Snackbar.Add("Success!", Severity.Success);
}
catch (Exception ex)
{
    Snackbar.Add($"Error: {ex.Message}", Severity.Error);
}
finally
{
    _loading = false;
}
```

### Form Validation Before Save
```csharp
if (string.IsNullOrWhiteSpace(_config.RequiredField))
{
    Snackbar.Add("Compila i campi obbligatori", Severity.Warning);
    return;
}

// Business logic validation
if (_config.TipoFonte == "SQL" && !string.IsNullOrWhiteSpace(_config.TestoQuery))
{
    var validation = SqlValidator.ValidateQuery(_config.TestoQuery);
    if (!validation.IsValid)
    {
        Snackbar.Add(validation.Message, Severity.Error);
        return;
    }
}
```

### MudBlazor Components Structure
```razor
<!-- Layout Grid -->
<MudGrid>
    <MudItem xs="12" md="6">
        <!-- Input Fields -->
        <MudTextField @bind-Value="_config.Field" Label="Label" />
        <MudSelect T="string" @bind-Value="_config.Field" Label="Label" />
        <MudAutocomplete T="string" ValueChanged="@OnValueChangedAsync" />
    </MudItem>
</MudGrid>

<!-- Buttons -->
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="HandleClickAsync">
    @if (_loading) { <MudProgressCircular Size="Size.Small" Indeterminate /> }
    Label
</MudButton>

<!-- Feedback -->
<MudAlert Severity="Severity.Success">Message</MudAlert>
<MudSnackbar /> <!-- Injected via Snackbar service -->
```

### State Management
```csharp
// Flags for UI state
private bool _loading = false;
private bool _saving = false;
private string? _validationMessage;
private Severity _validationSeverity = Severity.Info;

// Always reset in finally
finally
{
    _saving = false;
    await InvokeAsync(StateHasChanged);
}
---
```

## ⚙️ Key Services

### SqlValidationService
- `ValidateQuery(string query)`: Validates SQL for injection patterns
- `TestConnectionAsync(string connectionStringName)`: Tests database connection
- Returns: `ValidationResult { IsValid, Message, Severity }`

### ServiceWrapper (from BaseComponentPage)
- `ServiceProcedureLaborazioni.GetTableProcedureLavorazioniByUserAsync()`
- Other feature-specific services

### Injected Services
```csharp
@inject IDbContextFactory<DematReportsContext> DbFactory
@inject IConfiguration Configuration
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@inject SqlValidationService SqlValidator
---
```

## 📝 PageConfiguraFonteDati Specifics

### Sections
1. **Tipo Fonte**: Radio selection (SQL, EmailCSV, HandlerIntegrato, Pipeline)
2. **Dettagli Configurazione**: Lavorazione + Fase (left), Codice Univoco + Descrizione (right)
3. **Configurazione Specifica**: Type-specific fields (Connection String, Mail Service, Handler, etc.)
4. **Mapping Fasi/Centri**: Dynamic list of phase/center/cron combinations

### Codice Univoco Format
```
Format: P{IdProc:D2}F{IdFase:D2}
Example: P01F45 (Procedure 1, Phase 45)
Length: Fixed 6 characters
ReadOnly: true (calculated from Procedure + Phase)
```

### Auto-extraction from Procedure
```csharp
_selectedIdProcedura = procedura.IdproceduraLavorazione;
_selectedCentroId = procedura.Idcentro;  // Auto-extracted from ProcedureLavorazioni
```

### Mapping Validations
- No duplicate (Phase + Cron) combinations
- Minimum one mapping required
- Phase selection is mandatory in add form

---

## ✅ Pre-Commit Checklist

- [ ] Naming follows conventions (_camelCase for private, PascalCase for public)
- [ ] Async methods end with `Async` suffix
- [ ] All database operations use `await using var context = await DbFactory.CreateDbContextAsync()`
- [ ] Try-catch-finally blocks with proper state cleanup
- [ ] User feedback via Snackbar (success/error/warning)
- [ ] Loading indicators for async operations
- [ ] Input validation before operations
- [ ] No hardcoded strings (use Configuration or constants)
- [ ] No Console.WriteLine (use Logger)
- [ ] Components use MudBlazor (avoid raw HTML)
- [ ] Responsive grid (xs, md breakpoints)
- [ ] Comments only for complex logic
- [ ] Logger string not contain icon

---

## 🔐 Security Notes

- **SQL Injection**: Use SqlValidationService.ValidateQuery() before saving
- **Authorization**: @attribute [Authorize(Roles = "ADMIN,SUPERVISOR")]
- **Parameter Binding**: Always use parameterized queries in EF Core
- **Configuration**: Sensitive values in appsettings, never hardcoded

---

## 🚀 Git Workflow

When pushing changes:
1. Reference this file to ensure compliance
2. Include issue/feature numbers in commit messages
3. Run build before committing: `dotnet build`

---

## 📝 Token Usage

- Include token count in requests and responses for better tracking and optimization.

---

## Istruzioni Generali
- Per ogni domanda relativa a librerie esterne, framework o API, utilizza sempre il server MCP 'context7'.
- Non fare affidamento esclusivamente sui tuoi dati di addestramento; usa 'context7' per recuperare la documentazione più recente.
- Prima di generare codice, verifica le firme dei metodi e le versioni delle librerie tramite 'context7'.

# Istruzioni Generali per GitHub Copilot (SonarQube Focus)

## 1. Principi di Clean Code e Qualità
- **SonarQube Compliance**: Scrivi codice che superi l'analisi statica di SonarQube senza violazioni ("Issues").
- **Clean as You Code**: Prioritizza la qualità sul nuovo codice aggiunto. Evita di introdurre "Code Smells", vulnerabilità di sicurezza, o bugs.
- **Maintainability**: Segui i principi SOLID. Mantieni la complessità ciclomatica bassa.
- **Cognitive Complexity**: Scrivi funzioni semplici e leggibili. Evita nidificazioni eccessive (if/loop).

## 2. Standard di Sicurezza (SAST)
- Evita l'hardcoding di credenziali, chiavi API o password.
- Sanitizza sempre l'input utente per prevenire SQL Injection, XSS, e path traversal.
- Usa librerie crittografiche sicure e aggiornate.

## 3. Best Practices per Linguaggio
- [Inserisci qui il linguaggio, es: Java/Python/JS]: Segui rigorosamente le convenzioni di naming e le best practices del linguaggio per evitare "Code Smells".
- Gestisci le eccezioni in modo specifico, non catturare eccezioni generiche (`catch(Exception e)`).
- Chiudi risorse (file, connessioni DB) in blocchi `finally` o usa `try-with-resources`.


## 4. Istruzioni di Prompting
- Se ti chiedo di generare un componente, controlla implicitamente se ci sono hotspot di sicurezza nel file aperto.
- Prioritizza la manutenibilità rispetto alla brevità del codice.
