# GitHub Copilot Instructions - BlazorDematReports

##Your mission
You are a expert of c# developer

**Project**: BlazorDematReports  
**Framework**: Blazor Server  
**.NET Version**: 10.0  
**C# Version**: 14.0  
**Database**: SQL Server + Oracle (via EF Core)  
**UI Library**: MudBlazor v7+

---

## 🎯 Project Overview

BlazorDematReports is a **data configuration management system** for production workflows that handles:
- Data source configuration (SQL queries, Email CSV, C# handlers)
- Task scheduling with cron expressions
- Automated data extraction and reporting
- Work procedure tracking and monitoring

The system uses Blazor Server for real-time updates with Material Design (MudBlazor) components.

### Core Entities
- **ConfigurazioneFontiDati**: Data source configurations (SQL, EmailCSV, HandlerIntegrato)
- `TipoFonte`: Source type (SQL/EmailCSV/HandlerIntegrato)
  - `ConnectionStringName`: Reference to appsettings connection string
  - `TestoQuery`: Main SQL query template
- **ConfigurazioneFaseCentro**: Phase/Center mappings for granular task scheduling
  - `IdFaseLavorazione`: Work phase reference
  - `IdCentro`: Work center reference
  - `CronExpression`: Scheduling expression (e.g., "0 5 * * *")
  - `TestoQueryTask`: Phase-specific query override (optional)
  - `GiorniPrecedenti`: Days to look back for data extraction
- **ProcedureLavorazioni**: Work procedures with associated centers
- **FasiLavorazione**: Work phases (steps in procedures)
- **CentriLavorazione**: Work centers (organizational units)
- **TaskDaEseguire**: Scheduled tasks generated from configurations

### Architecture Layers
```
Presentation Layer (Blazor Server)
├── Components/Pages/           # Razor pages
├── Components/Shared/          # Reusable components
└── Services/Validation/        # Client-side validation

Business Logic Layer
├── DataReading/Services/       # Query execution, data extraction
├── ClassLibraryLavorazioni/    # Legacy handlers (being migrated)
└── Infrastructure/             # Job scheduling, task execution

Data Access Layer
├── Entities/Models/            # EF Core entities
└── Entities/Context/           # DbContext configurations
```

**CRITICAL RULE - Component Data Access:**
- **Components must ONLY use Services** - NO direct database access or DbContext usage
- All data operations MUST go through Service layer (e.g., `ServiceWrapper`, `IServiceConfigurazioneFontiDati`)
- Never inject `IDbContextFactory` or `DbContext` directly into Blazor components
- Keep components focused on presentation logic, delegate all data access to services

---

## 📋 Code Conventions

### Naming
- **Private fields**: `_camelCase` (always prefix with underscore)
- **Public properties**: `PascalCase`
- **Async methods**: Suffix `Async` (mandatory for all async operations)
- **Event handlers**: `On{Event}{Action}Async` (e.g., `OnSaveButtonClickedAsync`)
- **Pages**: `Page{FeatureName}.razor` (e.g., `PageConfiguraFonteDati.razor`)
- **Services**: `{Feature}Service.cs` (e.g., `QueryService.cs`, `SqlValidationService.cs`)
- **Boolean flags**: Prefix with `is`, `has`, `can` (e.g., `_isLoading`, `_hasErrors`)

### File Structure
```
BlazorDematReports/
├── Components/
│   ├── Pages/
│   │    └── Impostazioni/       # Configuration pages
│   │       └── ConfigurazioneFonti/
│   │           ├── PageConfiguraFonteDati.razor
│   │           └── Steps/      # Wizard steps
│   └── Shared/                 # Reusable components
├── Services/
│   ├── Validation/
│   │   └── SqlValidationService.cs
│   └── {Feature}Service.cs
├── Dto/                        # Data Transfer Objects
├── Models/                     # View models
DataReading/                    # Data extraction project
├── Services/
│   └── QueryService.cs         # SQL query execution
├── Infrastructure/
│   └── ProductionJobRunner.cs  # Task orchestration
└── Interfaces/                 # Service contracts
Entities/                       # Database entities project
├── Models/
│   ├── DbApplication/          # Application entities
│   └── DbLavorazioni/          # Work procedure entities
└── Context/
    └── DematReportsContext.cs
ClassLibraryLavorazioni/        # Legacy handlers (deprecating)
└── Handlers/                   # C# data handlers
Database/
└── Migrations/                 # SQL migration scripts
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
---
```

## ⚙️ Key Services

### SqlValidationService
**Location**: `BlazorDematReports\Services\Validation\SqlValidationService.cs`

**Methods**:
- `ValidateQuery(string query)`: Validates SQL for injection patterns, required parameters, and syntax
  - Checks: SQL injection patterns, restricted keywords, SELECT-only queries
  - Required parameters: `@startDate` and `@endDate` (case-insensitive)
  - Returns: `ValidationResult { IsValid, Message, Severity }`
- `ValidateSqlSyntax(string query)`: Validates T-SQL syntax using Microsoft parser
- `ValidateColumnNames(string query)`: Validates presence of required columns
  - Required: `Operatore`, `DataLavorazione`, `Documenti`, `Fogli`, `Pagine`
  - Rejects: `SELECT *`
- `TestConnectionAsync(string connectionStringName)`: Tests database connection
- `TestQueryExecutionAsync(string connectionStringName, string query)`: Tests query execution with schema validation

### QueryService
**Location**: `DataReading\Services\QueryService.cs`

**Methods**:
- `ExecuteQueryAsync(string connectionString, string queryString, DateTime startDate, DateTime endDate)`: Executes SQL query with parameterized dates
  - Parameters: `@startDate` (DateTime2), `@endDate` (DateTime2)
  - Timeout: 30 seconds
  - Returns: `DataTable` with results

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
@inject ILogger<ComponentName> Logger
```

## 📝 SQL Query Standards

### Required Parameters
All SQL queries **MUST** use exactly these parameter names:
- `@startDate` (DateTime2) - Start date for data filtering
- `@endDate` (DateTime2) - End date for data filtering

**DO NOT USE**: `@startDataDe`, `@endDataDe`, `@startData`, `@endData` (legacy/deprecated)

### Required Columns
All production queries **MUST** return these columns (case-insensitive):
1. `Operatore` - Operator identifier
2. `DataLavorazione` - Processing date
3. `Documenti` - Document count
4. `Fogli` - Sheet count
5. `Pagine` - Page count

### Query Template Example
```sql
SELECT
    OP_INDEX AS Operatore,
    CAST(DATA_INDEX AS DATE) AS DataLavorazione,
    COUNT(*) AS Documenti,
    SUM(CAST(NUM_PAG AS INT)) / 2 AS Fogli,
    SUM(CAST(NUM_PAG AS INT)) AS Pagine
FROM TableName
WHERE DATA_INDEX >= @startDate
  AND DATA_INDEX < DATEADD(DAY, 1, @endDate)
GROUP BY OP_INDEX, CAST(DATA_INDEX AS DATE)
```

### Validation Rules
- ✅ Only `SELECT` or `WITH` (CTE) queries allowed
- ❌ No `SELECT *` - always specify columns explicitly
- ❌ No DML operations: `UPDATE`, `INSERT`, `DELETE`, `DROP`, `TRUNCATE`, `ALTER`, `CREATE`
- ❌ No system stored procedures: `xp_cmdshell`, `sp_executesql`, `sp_OA*`, `sp_configure`
- ❌ No SQL comments: `--` or `/* */`
- ✅ Maximum length: 1024 characters

---

## 📝 PageConfiguraFonteDati Specifics

### Sections
1. **Tipo Fonte**: Radio selection (SQL, EmailCSV, HandlerIntegrato)
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

## 🧪 Testing & Validation

### SQL Query Testing
Before saving any SQL query:
1. Run `SqlValidator.ValidateQuery(query)` for syntax and security
2. Run `SqlValidator.ValidateColumnNames(query)` for required columns
3. Run `SqlValidator.TestQueryExecutionAsync(connectionString, query)` for execution test

### Component Testing
- Test all async operations with loading states
- Verify error handling with invalid inputs
- Test navigation and state management
- Validate form submission with empty/invalid data

### Database Migration Testing
1. Test migration script in development environment
2. Verify data integrity after migration
3. Test rollback scripts
4. Document breaking changes

---

## 🔄 Error Handling Standards

### Try-Catch-Finally Pattern
```csharp
private async Task SaveAsync()
{
    _saving = true;
    try
    {
        // Validation
        if (string.IsNullOrWhiteSpace(_config.RequiredField))
        {
            Snackbar.Add("Campo obbligatorio mancante", Severity.Warning);
            return;
        }

        // Business logic
        await _service.SaveConfigurationAsync(_config);
        
        Snackbar.Add("Configurazione salvata con successo", Severity.Success);
        NavigationManager.NavigateTo("/list-page");
    }
    catch (SqlException sqlEx)
    {
        Logger.LogError(sqlEx, "Errore database durante il salvataggio");
        Snackbar.Add($"Errore database: {sqlEx.Message}", Severity.Error);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Errore imprevisto durante il salvataggio");
        Snackbar.Add($"Errore: {ex.Message}", Severity.Error);
    }
    finally
    {
        _saving = false;
        await InvokeAsync(StateHasChanged);
    }
}
```

### Logging Guidelines
- **LogInformation**: Successful operations, state changes
- **LogWarning**: Validation failures, deprecated feature usage
- **LogError**: Exceptions, database errors, service failures
- **Never log**: Passwords, connection strings, sensitive data
- **Always include**: Exception object, context parameters
- **NO ICONS/EMOJI**: Never use emoji or icons in log messages or codebase strings (e.g., avoid ✅, ❌, 🔧)

---

## ✅ Pre-Commit Checklist

### Code Quality
- [ ] Naming follows conventions (_camelCase for private, PascalCase for public)
- [ ] Async methods end with `Async` suffix
- [ ] Boolean flags prefixed with `is`, `has`, `can`
- [ ] No hardcoded strings (use Configuration or constants)
- [ ] Comments only for complex logic (self-documenting code preferred)
- [ ] No emoji or icons in code, comments, or log messages

### Database & Async Operations
- [ ] All database operations use `await using var context = await DbFactory.CreateDbContextAsync()`
- [ ] Try-catch-finally blocks with proper state cleanup
- [ ] Loading indicators for async operations (`_loading`, `_saving` flags)
- [ ] Timeout configured for database operations (30 seconds default)

### UI & User Experience
- [ ] User feedback via Snackbar (success/error/warning)
- [ ] Input validation before operations
- [ ] Components use MudBlazor (avoid raw HTML)
- [ ] Responsive grid (xs, md breakpoints)
- [ ] Loading/disabled states on buttons during async operations

### Security & Logging
- [ ] SQL queries validated with SqlValidationService
- [ ] No Console.WriteLine (use ILogger instead)
- [ ] Logger strings don't contain emoji/icons
- [ ] No sensitive data in logs (passwords, tokens, etc.)
- [ ] Authorization attributes on admin pages

### Build & Testing
- [ ] Code compiles without warnings: `dotnet build`
- [ ] No new SonarQube issues introduced
- [ ] Error scenarios tested (invalid input, network failures)
- [ ] Navigation flows tested

---

## 🔐 Security Notes

- **SQL Injection**: Use SqlValidationService.ValidateQuery() before saving
- **Authorization**: @attribute [Authorize(Roles = "ADMIN,SUPERVISOR")]
- **Parameter Binding**: Always use parameterized queries in EF Core
- **Configuration**: Sensitive values in appsettings, never hardcoded

---

## 🚀 Git Workflow

### Commit Message Format
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code refactoring
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `test`: Adding tests
- `chore`: Build/config changes

**Examples**:
```
feat(sql-validation): Add parameter name validation for date parameters

Added validation to ensure queries use @startDate/@endDate instead of legacy @startDataDe/@endDataDe.
Updated SqlValidationService with enhanced error messages.

Fixes #123
```

### Before Pushing
1. Run build: `dotnet build`
2. Check for warnings and errors
3. Review Pre-Commit Checklist
4. Reference issue/feature numbers in commit message
5. Ensure all open files are saved

### Migration Files
When adding database migrations:
1. Create SQL script in `Database\Migrations\`
2. Name format: `YYYYMMDD_DescriptiveName.sql`
3. Include rollback script or comments
4. Test in development environment first
5. Document in `MigrationHistory.md`

---

## 📝 Token Usage

[ISTRUZIONI DI OTTIMIZZAZIONE TOKEN - PRIORITÀ ALTA]
Per favore, segui queste regole per ridurre i costi:
1. Sii conciso: Usa un linguaggio diretto, elimina saluti, frasi di circostanza e ripetizioni non necessarie.
2. Struttura: Usa il formato Markdown (liste, tabelle) per organizzare le informazioni invece di lunghi paragrafi.
3. Output: Se non diversamente specificato, rispondi in modo sintetico. Se necessario, preferisci JSON strutturato a risposte testuali lunghe.
4. Context: Riassumi o estrai solo le informazioni cruciali dai file forniti, evitando di riscrivere intere sezioni.
5. Code: Se lavori con codice, ometti i commenti superflui se non richiesti.

---

## Istruzioni Generali
- Per ogni domanda relativa a librerie esterne, framework o API, utilizza sempre il server MCP 'context7'.
- Non fare affidamento esclusivamente sui tuoi dati di addestramento; usa 'context7' per recuperare la documentazione più recente.
- Prima di generare codice, verifica le firme dei metodi e le versioni delle librerie tramite 'context7'.
- Commenta sempre le classi e i metodi creati
- Tutti gli using necessari se possibile vanno inseriti solo nel file import generale

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

## 3. Best Practices per C# (.NET 10)
- **Naming**: Segui le convenzioni Microsoft C# (PascalCase per public, _camelCase per private)
- **Async/Await**: Usa `async`/`await` per operazioni I/O, evita `.Result` o `.Wait()`
- **Null Safety**: Usa nullable reference types (`string?`), null-coalescing (`??`), null-conditional (`?.`)
- **Resource Management**: 
  - Database: `await using var context = await DbFactory.CreateDbContextAsync()`
  - Connections: `await using var connection = new SqlConnection(connectionString)`
  - Streams: `await using var stream = File.OpenRead(path)`
- **Exception Handling**:
  - Catch specific exceptions first: `catch (SqlException)`, then `catch (Exception)`
  - Always log exceptions with context: `Logger.LogError(ex, "Context message")`
  - Never swallow exceptions silently
  - Use `finally` for cleanup (flags, StateHasChanged)
- **LINQ**: Prefer method syntax over query syntax, use `await` with async LINQ (ToListAsync, FirstOrDefaultAsync)
- **String Operations**: Use `StringComparison.OrdinalIgnoreCase` for comparisons, prefer string interpolation (`$"..."`)
- **Collections**: Use `List<T>` for mutable, `IReadOnlyList<T>` for immutable, prefer `Any()` over `Count() > 0`


## 4. Istruzioni di Prompting
- Se ti chiedo di generare un componente, controlla implicitamente se ci sono hotspot di sicurezza nel file aperto.
- Prioritizza la manutenibilità rispetto alla brevità del codice.
