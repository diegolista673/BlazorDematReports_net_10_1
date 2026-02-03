# Errori di Compilazione da Correggere

## ?? Sommario
Dopo la rimozione della tabella `Task` (TabellaTask), ci sono errori di compilazione che devono essere corretti sistematicamente.

## ?? Tipologie di Errori

### 1. **FlagAttiva nullable** (`bool?`)
**Problema**: `FlagAttiva` è `bool?` ma viene usato come `bool` nei confronti.

**Soluzione**: Aggiungere `== true` o `== false` nei confronti.

**Esempi da correggere**:
```csharp
// ? ERRORE
.Where(fc => fc.FlagAttiva)
config.ConfigurazioneFaseCentros.Where(m => m.FlagAttiva)

// ? CORRETTO
.Where(fc => fc.FlagAttiva == true)
config.ConfigurazioneFaseCentros.Where(m => m.FlagAttiva == true)
```

**File da correggere**:
- `BlazorDematReports/Components/Pages/Admin/PageListaConfigurazioniFonti.razor` (line 234)
- `BlazorDematReports/Components/Pages/Admin/PageConfiguraFonteDati.razor` (line 194)
- `BlazorDematReports/Components/Pages/Impostazioni/Components/ProcedureConfigurazioniWidget.razor` (lines 151, 199)
- `BlazorDematReports/Components/Pages/Impostazioni/PageEditProcedura.razor` (line 259)
- `BlazorDematReports/Services/DataService/ServiceConfigurazioneFontiDati.cs` (lines 47, 124)
- `BlazorDematReports/Services/DataService/ServiceMail.cs` (line 115)
- `ClassLibraryLavorazioni/Shared/Handlers/UnifiedDataSourceHandler.cs` (lines 56, 88)

---

### 2. **IdTask rimosso**
**Problema**: La proprietà `TaskDaEseguire.IdTask` non esiste più (FK verso tabella eliminata).

**Soluzione**: Rimuovere tutti i riferimenti a `IdTask`.

**File da correggere**:
- `BlazorDematReports/Components/Pages/Admin/PageListaConfigurazioniFonti.razor` (line 296)
  ```csharp
  // ? RIMUOVERE
  IdTask = 1,
  ```
- `BlazorDematReports/Components/Pages/Impostazioni/Components/ProcedureConfigurazioniWidget.razor` (line 233)
- `BlazorDematReports/Services/DataService/ServiceProcedureLavorazioni.cs` (line 661)

---

### 3. **IdTaskNavigation rimosso**
**Problema**: La navigation property `TaskDaEseguire.IdTaskNavigation` non esiste più.

**Soluzione**: Rimuovere tutti i `.ThenInclude(x => x.IdTaskNavigation)`.

**File da correggere**:
- `BlazorDematReports/Services/DataService/ServiceTaskDaEseguire.cs` (lines 43, 58)
  ```csharp
  // ? RIMUOVERE
  .Include(x => x.IdTaskNavigation)
  ```
- `BlazorDematReports/Services/DataService/ServiceProcedureLavorazioni.cs` (lines 121, 149, 257, 713, 732, 754, 776)
  ```csharp
  // ? RIMUOVERE
  .ThenInclude(x => x.IdTaskNavigation)
  ```
- `BlazorDematReports/Mapping/ConfigProcedureLavorazioniProfile.cs` (lines 22, 23, 54)

---

### 4. **TimeTask rimosso**
**Problema**: La proprietà `TaskDaEseguire.TimeTask` non esiste più.

**Soluzione**: Rimuovere dai mapping AutoMapper e dalle assegnazioni.

**File da correggere**:
- `BlazorDematReports/Mapping/ConfigProcedureLavorazioniProfile.cs` (lines 25, 56)
  ```csharp
  // ? RIMUOVERE
  .ForMember(dest => dest.TimeTask, opt => opt.MapFrom(...))
  ```
- `BlazorDematReports/Services/DataService/ServiceProcedureLavorazioni.cs` (line 674)
  ```csharp
  // ? RIMUOVERE
  existingTask.TimeTask = ...
  ```

---

### 5. **ConfigurazioneDatabase ? IdConfigurazioneDatabaseNavigation**
**Problema**: La navigation property si chiama `IdConfigurazioneDatabaseNavigation`, non `ConfigurazioneDatabase`.

**Soluzione**: Sostituire tutti i riferimenti.

**File da correggere**:
- `BlazorDematReports/Services/DataService/ServiceTaskDaEseguire.cs` (lines 74, 92, 111)
  ```csharp
  // ? ERRORE
  .Include(t => t.ConfigurazioneDatabase)
  
  // ? CORRETTO
  .Include(t => t.IdConfigurazioneDatabaseNavigation)
  ```
- `BlazorDematReports/Mapping/ConfigProcedureLavorazioniProfile.cs` (line 55)
  ```csharp
  // ? ERRORE
  .ForMember(dest => dest.ConfigurazioneDatabase, opt => opt.Ignore())
  
  // ? CORRETTO
  .ForMember(dest => dest.IdConfigurazioneDatabaseNavigation, opt => opt.Ignore())
  ```

---

### 6. **TabellaTasks rimosso**
**Problema**: La tabella `TabellaTask` è stata eliminata dal database.

**Soluzione**: Rimuovere query verso `context.TabellaTasks`.

**File da correggere**:
- `BlazorDematReports/Services/DataService/ServiceProcedureLavorazioni.cs` (line 418)
  ```csharp
  // ? RIMUOVERE COMPLETAMENTE
  var taskExists = await context.TabellaTasks
      .AnyAsync(t => t.IdTask == taskDto.IdTask);
  ```

---

### 7. **GiorniPrecedenti non nullable**
**Problema**: `ConfigurazioneFontiDati.GiorniPrecedenti` è `int`, non `int?`.

**Soluzione**: Rimuovere l'operatore `??`.

**File da correggere**:
- `BlazorDematReports/Components/Pages/Admin/PageConfiguraFonteDati.razor` (line 102)
  ```csharp
  // ? ERRORE
  GiorniPrecedenti="@(_config.GiorniPrecedenti ?? 1)"
  
  // ? CORRETTO
  GiorniPrecedenti="@(_config.GiorniPrecedenti)"
  ```

---

### 8. **PipelineSteps ? ConfigurazionePipelineSteps**
**Problema**: La collection si chiama `ConfigurazionePipelineSteps`, non `PipelineSteps`.

**Soluzione**: Correggere il nome.

**File da correggere**:
- `ClassLibraryLavorazioni/Shared/Handlers/UnifiedDataSourceHandler.cs` (lines 284, 429)
  ```csharp
  // ? ERRORE
  var steps = config.PipelineSteps
  .Include(c => c.PipelineSteps)
  
  // ? CORRETTO
  var steps = config.ConfigurazionePipelineSteps
  .Include(c => c.ConfigurazionePipelineSteps)
  ```

---

### 9. **Tasks ? TaskDaEseguires**
**Problema**: La collection in `ConfigurazioneFontiDati` si chiama `TaskDaEseguires`, non `Tasks`.

**Soluzione**: Correggere il nome.

**File da correggere**:
- `BlazorDematReports/Services/DataService/ServiceConfigurazioneFontiDati.cs` (line 168)
  ```csharp
  // ? ERRORE
  .Include(c => c.Tasks)
  
  // ? CORRETTO
  .Include(c => c.TaskDaEseguires)
  ```

---

### 10. **ThenInclude su IEnumerable**
**Problema**: Dopo `.Where()` su una collection, non si può fare `.ThenInclude()`.

**Soluzione**: Rimuovere il `.ThenInclude()` o ristrutturare la query.

**File da correggere**:
- `BlazorDematReports/Services/DataService/ServiceMail.cs` (lines 114, 115, 149)
  ```csharp
  // ? ERRORE
  .Include(c => c.ConfigurazioneFaseCentros.Where(fc => fc.FlagAttiva == true))
      .ThenInclude(fc => fc.Procedura)
  
  // ? CORRETTO - Opzione 1: Rimuovere Where
  .Include(c => c.ConfigurazioneFaseCentros)
      .ThenInclude(fc => fc.Procedura)
  
  // ? CORRETTO - Opzione 2: Filtrare dopo il caricamento
  .Include(c => c.ConfigurazioneFaseCentros)
      .ThenInclude(fc => fc.Procedura)
  // Poi filtrare in memoria:
  var activeConfigs = configs.Select(c => new {
      Config = c,
      ActiveMappings = c.ConfigurazioneFaseCentros.Where(fc => fc.FlagAttiva == true)
  });
  ```

---

## ? Lista di Controllo

### ServiceMail.cs
- [x] `FlagAttiva == true` in tutte le query (FATTO)
- [ ] Correggere `.ThenInclude()` dopo `.Where()`

### ProductionJobInfrastructure.cs
- [x] `IdConfigurazioneDatabaseNavigation` invece di `ConfigurazioneDatabase` (FATTO)
- [x] Rimuovere `ParametriExtra` da `ConfigurazioneFontiDati` (usa `ConfigurazioneFaseCentro.ParametriExtra`)

### Razor Components
- [ ] Correggere tutti i `FlagAttiva` con `== true`
- [ ] Rimuovere tutti i `IdTask = ...`

### Services
- [ ] Rimuovere tutti i `.ThenInclude(x => x.IdTaskNavigation)`
- [ ] Sostituire `ConfigurazioneDatabase` con `IdConfigurazioneDatabaseNavigation`
- [ ] Rimuovere query verso `TabellaTasks`

### Mapping (AutoMapper)
- [ ] Rimuovere mapping per `IdTaskNavigation`
- [ ] Rimuovere mapping per `TimeTask`
- [ ] Correggere `ConfigurazioneDatabase` ? `IdConfigurazioneDatabaseNavigation`

### UnifiedDataSourceHandler.cs
- [ ] `FlagAttiva == true` invece di `!config.FlagAttiva`
- [ ] `PipelineSteps` ? `ConfigurazionePipelineSteps`

---

## ?? Strategia di Fix

1. **Batch 1 - FlagAttiva**: Cercare tutti i `FlagAttiva` e aggiungere `== true`
2. **Batch 2 - IdTask**: Rimuovere tutti i riferimenti a `IdTask` e `IdTaskNavigation`
3. **Batch 3 - TimeTask**: Rimuovere tutti i riferimenti a `TimeTask`
4. **Batch 4 - Navigation**: Correggere i nomi delle navigation properties
5. **Batch 5 - Collections**: Correggere i nomi delle collections (`Tasks`, `PipelineSteps`)
6. **Batch 6 - ThenInclude**: Sistemare le query con `.ThenInclude()` dopo `.Where()`

---

## ?? Note
- **Non creare nuove migration** finché tutti gli errori non sono risolti
- **Testare il build dopo ogni batch di modifiche**
- **Committare dopo ogni batch riuscito**
