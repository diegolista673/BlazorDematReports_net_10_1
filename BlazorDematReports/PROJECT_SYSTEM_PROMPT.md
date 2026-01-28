# 🎯 SYSTEM PROMPT - BlazorDematReports Project

**Versione**: 1.0  
**Data**: 2024  
**Progetto**: BlazorDematReports  
**Proprietario**: Team DematReports

---

## 📋 Indice
1. [Stack Tecnologico](#stack-tecnologico)
2. [Architettura](#architettura)
3. [Convenzioni di Codice](#convenzioni-di-codice)
4. [Struttura Cartelle](#struttura-cartelle)
5. [Componenti UI Frequenti](#componenti-ui-frequenti)
6. [Gestione Stato e Notifiche](#gestione-stato-e-notifiche)
7. [Pattern Comuni](#pattern-comuni)
8. [Entità Principali](#entità-principali)
9. [Checklist Before Submit](#checklist-before-submit)

---

## 🛠️ Stack Tecnologico

| Componente | Versione | Dettagli |
|-----------|----------|----------|
| Framework | Blazor (Server) | Server-side rendering |
| .NET | 10.0 | Latest LTS |
| C# | 14.0 | Modern language features |
| UI Library | MudBlazor | Material Design components |
| ORM | Entity Framework Core | Database access layer |
| Database | SQL Server | Primary data store |
| Authentication | OAuth/OpenID | Role-based authorization |
| Configuration | appsettings.json | Configuration management |

---

## 🏗️ Architettura

### Pattern Principale
- **MVVM con Blazor Components**: Ereditanza da `BaseComponentPage<T>`
- **Data Access Factory**: `IDbContextFactory<DematReportsContext>` per istanze separate
- **Dependency Injection**: Constructor injection per servizi
- **Service Layer**: Servizi dedicati per logica di business

### Flusso Dati
```
Blazor Component (@page)
    ↓
Service Layer (SqlValidationService, etc.)
    ↓
Entity Framework Core
    ↓
SQL Server Database
```

### Autenticazione e Autorizzazione
```csharp
@attribute [Authorize(Roles = "ADMIN,SUPERVISOR")]
```
- Verifica ruoli ad accesso pagina
- Policy-based authorization disponibile

---

## 📝 Convenzioni di Codice

### Naming Conventions

#### Private Fields
```csharp
private string? _selectedLavorazione;
private bool _isEdit => IdConfigurazione.HasValue;
private int _selectedFaseId = 0;
```
- Prefix: `_`
- Case: `camelCase`
- Separare logica (computed vs stored)

#### Public Properties e Parameters
```csharp
[Parameter] public int? IdConfigurazione { get; set; }
[SupplyParameterFromQuery(Name = "idProcedura")]
public int? IdProceduraPrecompilata { get; set; }
```
- Case: `PascalCase`
- Route parameters con `{ParamName:type}`
- Query parameters con `@` e `[SupplyParameterFromQuery]`

#### Metodi
```csharp
private async Task OnValueChangedLavorazioneAsync(string? lavorazione)
private void ClearCurrentConfig()
private async Task SalvaConfigurazioneAsync()
```
- Async suffix: `Async` per metodi asincroni
- Verb-first naming: `Load`, `Save`, `Validate`, `Clear`
- Event handlers: `On{Event}{Action}Async`

#### File e Cartelle
```
BlazorDematReports/
├── Components/Pages/Admin/
│   └── PageConfiguraFonteDati.razor        ← Page{Feature}.razor
├── Services/Validation/
│   └── SqlValidationService.cs             ← {Feature}Service.cs
```
- Pagine: `Page{FeatureName}.razor`
- Servizi: `{Feature}Service.cs`
- Cartelle: `PascalCase`

### Formattazione Codice

#### Using Statements
```csharp
@using Entities.Models.DbApplication
@using Microsoft.EntityFrameworkCore
@using LibraryLavorazioni.Shared.Discovery
@using BlazorDematReports.Services.Validation
```
- Ordine: Microsoft → Custom → Project-specific
- Uno per linea

#### Dependency Injection
```csharp
@inject IDbContextFactory<DematReportsContext> DbFactory
@inject IConfiguration Configuration
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@inject SqlValidationService SqlValidator
```
- Un inject per linea
- Ordine: Interfaces → Concretes

### Async/Await Guidelines
```csharp
// ✅ CORRETTO
private async Task LoadDataAsync()
{
    var data = await service.GetDataAsync();
    return data;
}

// ❌ EVITARE
private async Task LoadData()
{
    return Task.CompletedTask;  // Non usare se non necessario
}
```

---

## 📁 Struttura Cartelle

```
BlazorDematReports/
│
├── Components/
│   ├── Pages/
│   │   ├── Admin/
│   │   │   ├── PageConfiguraFonteDati.razor
│   │   │   ├── PageListaConfigurazioniFonti.razor
│   │   │   └── ...altri admin pages
│   │   ├── Public/
│   │   └── ...feature pages
│   │
│   ├── Shared/
│   │   ├── BaseComponentPage.cs            ← Base class per tutti i components
│   │   ├── NavMenu.razor
│   │   └── ...shared components
│   │
│   └── Layout/
│       └── MainLayout.razor
│
├── Services/
│   ├── Validation/
│   │   ├── SqlValidationService.cs
│   │   └── ValidationResult.cs
│   ├── {FeatureName}/
│   │   └── {FeatureName}Service.cs
│   └── ...other services
│
├── Models/                                  ← (Se non in Entities project)
│   └── ...domain models
│
├── Entities/
│   ├── Models/
│   │   └── DbApplication/
│   │       ├── ConfigurazioneFontiDati.cs
│   │       ├── ConfigurazioneFaseCentro.cs
│   │       ├── FasiLavorazione.cs
│   │       └── ...other models
│   └── DbContexts/
│       └── DematReportsContext.cs
│
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

---

## 🎨 Componenti UI Frequenti

### Layout
```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudGrid>
        <MudItem xs="12" md="6">
            <!-- Content -->
        </MudItem>
    </MudGrid>
</MudContainer>
```

### Input Fields
```razor
<!-- TextField -->
<MudTextField @bind-Value="_config.CodiceConfigurazione"
              Label="Codice Univoco"
              Variant="Variant.Outlined"
              Required="true" />

<!-- Select -->
<MudSelect T="string" @bind-Value="_config.ConnectionStringName"
           Label="Connection String"
           Variant="Variant.Outlined">
    @foreach (var cs in _connectionStrings)
    {
        <MudSelectItem Value="@cs">@cs</MudSelectItem>
    }
</MudSelect>

<!-- Autocomplete -->
<MudAutocomplete T="string"
                 Label="Lavorazione"
                 SearchFunc="@((string arg, CancellationToken ct) => 
                             SearchFromSelect(base.SelectLavorazione, arg))"
                 ValueChanged="@OnValueChangedLavorazioneAsync"
                 Variant="Variant.Outlined" />

<!-- RadioGroup -->
<MudRadioGroup @bind-Value="_config.TipoFonte">
    <MudRadio Value="@("SQL")" Color="Color.Primary">SQL</MudRadio>
    <MudRadio Value="@("EmailCSV")" Color="Color.Secondary">Email CSV</MudRadio>
</MudRadioGroup>
```

### Buttons e Azioni
```razor
<!-- Primary Button -->
<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           OnClick="SalvaConfigurazioneAsync"
           Disabled="@_saving">
    @if (_saving) { <MudProgressCircular Size="Size.Small" Indeterminate /> }
    Salva Configurazione
</MudButton>

<!-- Icon Button -->
<MudIconButton Icon="@Icons.Material.Filled.Delete"
               Color="Color.Error"
               OnClick="@(() => RimuoviMapping(mapping))" />
```

### Feedback & Alerts
```razor
<!-- Alert -->
<MudAlert Severity="@_validationSeverity" Dense Elevation="1">
    @_validationMessage
</MudAlert>

<!-- Divider -->
<MudDivider Class="my-4" />

<!-- Paper -->
<MudPaper Class="pa-4" Elevation="3">
    <!-- Content -->
</MudPaper>
```

### Spacing & Classes
```html
<!-- Margin/Padding -->
<div Class="mt-4 mb-2 pa-3 pr-4">

<!-- Flexbox -->
<div Class="d-flex gap-2 justify-space-between align-center">

<!-- Display -->
<div Class="d-none d-md-flex">  <!-- Hidden on mobile, flex on medium+ -->
```

---

## 🔔 Gestione Stato e Notifiche

### Overlay (Loading Screen)
```csharp
UiState?.ShowOverlay("Caricamento lavorazioni...");
try
{
    // operazione lunga
}
finally
{
    UiState?.HideOverlay();
}
```

### Snackbar (Toast Messages)
```csharp
// Success
Snackbar.Add("Configurazione salvata!", Severity.Success);

// Warning
Snackbar.Add("Compila i campi obbligatori", Severity.Warning);

// Error
Snackbar.Add($"Errore: {ex.Message}", Severity.Error);

// Info
Snackbar.Add("Mapping pre-compilato", Severity.Info);
```

### State Management
```csharp
// Flag per loading
private bool _saving = false;
private bool _testingConnection = false;

// Messaggi
private string? _validationMessage;
private Severity _validationSeverity = Severity.Info;

// Sempre resettare nello finally
finally
{
    _saving = false;
    await InvokeAsync(StateHasChanged);
}
```

---

## 🔄 Pattern Comuni

### Inicializzazione Componente
```csharp
protected override async Task OnInitializedAsync()
{
    try
    {
        _initializing = true;
        UiState?.ShowOverlay("Caricamento...");

        // 1. Carica dati da servizi
        ListProcedureLavorazioni = await ServiceWrapper!
            .ServiceProcedureLavorazioni
            .GetTableProcedureLavorazioniByUserAsync();

        // 2. Carica dati da database
        await using var context = await DbFactory.CreateDbContextAsync();
        _procedure = await context.ProcedureLavorazionis
            .Where(p => p.Attiva == true)
            .OrderBy(p => p.NomeProcedura)
            .ToListAsync();

        // 3. Usa parametri rotta
        if (_isEdit)
        {
            var config = await context.ConfigurazioneFontiDatis
                .Include(c => c.ConfigurazioneFaseCentros)
                .FirstOrDefaultAsync(c => c.IdConfigurazione == IdConfigurazione);
        }
    }
    catch (Exception ex)
    {
        Logger?.LogError(ex, "Error initializing");
        UiState?.ShowSnackbar($"Errore: {ex.Message}", Severity.Error);
    }
    finally
    {
        _initializing = false;
        UiState?.HideOverlay();
    }
}
```

### Validazione Prima del Salvataggio
```csharp
private async Task SalvaAsync()
{
    // 1. Validazione obbligatoria
    if (string.IsNullOrWhiteSpace(_config.CodiceConfigurazione))
    {
        Snackbar.Add("Campo obbligatorio", Severity.Warning);
        return;
    }

    // 2. Validazione business logic
    if (_config.TipoFonte == "SQL")
    {
        var queryValidation = SqlValidator.ValidateQuery(_config.TestoQuery);
        if (!queryValidation.IsValid)
        {
            Snackbar.Add(queryValidation.Message, Severity.Error);
            return;  // Blocca salvataggio
        }
    }

    // 3. Esecuzione operazione
    _saving = true;
    try
    {
        await using var context = await DbFactory.CreateDbContextAsync();
        // CRUD operations
        await context.SaveChangesAsync();
        Snackbar.Add("Salvato!", Severity.Success);
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
    }
    finally
    {
        _saving = false;
    }
}
```

### Database Operations
```csharp
// Factory pattern (OBBLIGATORIO)
await using var context = await DbFactory.CreateDbContextAsync();

// Query con Include
var config = await context.ConfigurazioneFontiDatis
    .Include(c => c.ConfigurazioneFaseCentros)
    .FirstOrDefaultAsync(c => c.IdConfigurazione == IdConfigurazione);

// Add
context.ConfigurazioneFontiDatis.Add(_config);

// Update
_config.ModificatoIl = DateTime.Now;
context.ConfigurazioneFontiDatis.Update(_config);

// Delete
context.ConfigurazioneFaseCentros.RemoveRange(oldMappings);

// Save
await context.SaveChangesAsync();
```

### Event Binding in Blazor
```csharp
// ValueChanged con async
<MudTextField Value="@_selectedLavorazione"
              ValueChanged="@OnValueChangedLavorazioneAsync" />

private async Task OnValueChangedLavorazioneAsync(string? lavorazione)
{
    _selectedLavorazione = lavorazione;
    await InvokeAsync(StateHasChanged);
}

// OnClick
<MudButton OnClick="SalvaConfigurazioneAsync" />

// Inline lambda
<MudButton OnClick="@(() => RimuoviMapping(mapping))" />
```

### JSON Serialization (ParametriExtra)
```csharp
// Deserialize
var json = System.Text.Json.JsonSerializer
    .Deserialize<Dictionary<string, object>>(mapping.ParametriExtra);

// Serialize
mapping.ParametriExtra = System.Text.Json.JsonSerializer.Serialize(json);

// Con opzioni
var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
```

---

## 📊 Entità Principali

### ConfigurazioneFontiDati
```csharp
public class ConfigurazioneFontiDati
{
    public int IdConfigurazione { get; set; }
    public string CodiceConfigurazione { get; set; }  // Univoco
    public string NomeConfigurazione { get; set; }
    public string DescrizioneConfigurazione { get; set; }
    public string TipoFonte { get; set; }  // SQL, EmailCSV, HandlerIntegrato, Pipeline
    
    // SQL-specific
    public string? ConnectionStringName { get; set; }
    public string? TestoQuery { get; set; }
    
    // EmailCSV-specific
    public string? MailServiceCode { get; set; }
    
    // Handler-specific
    public string? HandlerClassName { get; set; }
    
    // Audit
    public DateTime CreatoIl { get; set; }
    public DateTime? ModificatoIl { get; set; }
    public bool FlagAttiva { get; set; }
    
    // Relations
    public ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; }
}
```

### ConfigurazioneFaseCentro
```csharp
public class ConfigurazioneFaseCentro
{
    public int IdConfigurazione { get; set; }
    public int IdProceduraLavorazione { get; set; }
    public int IdFaseLavorazione { get; set; }
    public int IdCentro { get; set; }
    
    public string? ParametriExtra { get; set; }  // JSON: { "cron": "0 5 * * *", ... }
    public bool FlagAttiva { get; set; }
    
    // Navigation properties
    public ConfigurazioneFontiDati Configurazione { get; set; }
}
```

### Master Data Entities
```csharp
- ProcedureLavorazioni (Procedure di lavorazione)
- FasiLavorazione (Fasi delle procedure)
- CentriLavorazione (Centri di lavoro)
- LavorazioniFasiDataReadings (Mapping procedure ↔ fasi)
```

---

## ✅ Checklist Before Submit

### Code Quality
- [ ] Nomi seguono convenzioni project
- [ ] Commenti solo se necessario (non ridondanti)
- [ ] Nessun codice commented-out (a meno che per debugging)
- [ ] No hardcoded strings (usare Configuration o constants)
- [ ] No Console.WriteLine() (usare Logger)

### Async/Await
- [ ] Metodi asincroni terminano con `Async`
- [ ] Usare `await InvokeAsync(StateHasChanged)` se richiesto
- [ ] No `async void` (solo `async Task`)
- [ ] Try-catch-finally con pulizia stato

### Database
- [ ] Usare `await using var context = await DbFactory.CreateDbContextAsync()`
- [ ] Include() relationships quando necessario
- [ ] Validare dati prima di CRUD
- [ ] Gestire eccezioni EF Core

### UI/UX
- [ ] Indicatori di caricamento per operazioni async
- [ ] Messaggi Snackbar appropriati
- [ ] Validazione campi obbligatori
- [ ] Gestire null/empty values
- [ ] Responsive design (xs, md, lg)

### Security
- [ ] SQL Injection: usare parametri per queries dinamiche
- [ ] Authorization: [Authorize] attributes
- [ ] Validare input utente
- [ ] Non esporre dati sensibili in messaggi

### Testing Scenarios
- [ ] Form validation (obbligatori)
- [ ] Error handling (connessione DB, service)
- [ ] Loading states (overlays, buttons disabilitati)
- [ ] Edit vs Create flows
- [ ] Navigation post-operazione

### Performance
- [ ] Evitare N+1 queries (usare Include/ThenInclude)
- [ ] Filtrare a livello database, non in memoria
- [ ] Cache dati statici (master data)
- [ ] Lazy loading se necessario

### Documentation
- [ ] Commenti XML per metodi pubblici
- [ ] Spiegare logica complessa
- [ ] Documentare parametri non ovvi

---

## 🔗 Riferimenti Utili

### MudBlazor
- https://mudblazor.com/components
- Theme customization
- Responsive grid system

### Entity Framework Core
- DbContextFactory pattern
- Include/ThenInclude
- Async operations

### Blazor Best Practices
- Lifecycle methods
- State management
- Component parameters

---

### Aggiornamenti al Prompt
- aggiorna il file ad ogni modifica significativa
- mantieni versioning e data aggiornati
- documenta cambiamenti rilevanti


## 📞 Contatti e Supporto

Per domande sulla struttura o best practices:
1. Consultare questo prompt
2. Verificare file simili nel progetto
3. Fare riferimento al code standard del team

---

**Last Updated**: 2026  
**Status**: Active  
**Maintainer**: Development Team
