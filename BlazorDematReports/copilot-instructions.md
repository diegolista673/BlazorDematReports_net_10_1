# GitHub Copilot Instructions - BlazorDematReports

**Project**: BlazorDematReports  
**Framework**: Blazor Server  
**.NET Version**: 10.0  
**C# Version**: 14.0

---

## 🎯 Project Overview

BlazorDematReports is a data configuration management system for production workflows. The project manages data sources, scheduling, and task automation through a Blazor Server interface with Material Design (MudBlazor) components.

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
```

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
```

---

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
```

---

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
4. Test async operations work correctly
5. Verify UI responsive across breakpoints

---

## 📚 Related Files

- **Project System Prompt**: `prompts/PROJECT_SYSTEM_PROMPT.md`
- **Main Page**: `BlazorDematReports/Components/Pages/PageConfiguraFonteDati.razor`
- **List Page**: `BlazorDematReports/Components/Pages/PageListaConfigurazioniFonti.razor`
- **Validation Service**: `BlazorDematReports/Services/Validation/SqlValidationService.cs`

---

## [ISTRUZIONI DI OTTIMIZZAZIONE TOKEN - PRIORITÀ ALTA]
Per favore, segui queste regole per ridurre i costi:
1. Sii conciso: Usa un linguaggio diretto, elimina saluti, frasi di circostanza e ripetizioni non necessarie.
2. Struttura: Usa il formato Markdown (liste, tabelle) per organizzare le informazioni invece di lunghi paragrafi.
3. Output: Se non diversamente specificato, rispondi in modo sintetico. Se necessario, preferisci JSON strutturato a risposte testuali lunghe.
4. Context: Riassumi o estrai solo le informazioni cruciali dai file forniti, evitando di riscrivere intere sezioni.
5. Code: Se lavori con codice, ometti i commenti superflui se non richiesti.


**Version**: 1.0  
**Last Updated**: 2024  
**Status**: Active
