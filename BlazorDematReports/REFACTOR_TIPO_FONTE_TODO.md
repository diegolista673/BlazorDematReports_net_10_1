# Refactoring TipoFonte: Da String a Enum - Guida Completamento

## ✅ Lavoro Completato (50%)

### Layer Entities & Database
1. ✅ **Enum creato**: `Entities/Enums/TipoFonteData.cs`
   - Valori: SQL (0), HandlerIntegrato (1)
   - Attributi Description per UI
   
2. ✅ **Converter EF Core**: `Entities/Converters/TipoFonteDataConverter.cs`
   - Converte automaticamente enum ↔ string nel database
   - **Nessuna modifica al DB necessaria!**

3. ✅ **Model aggiornato**: `ConfigurazioneFontiDati.cs`
   - `TipoFonte` ora è `TipoFonteData` invece di `string`
   
4. ✅ **DbContext configurato**: `DematReportsContext.cs`
   - Aggiunto `.HasConversion<TipoFonteDataConverter>()`

### Layer UI & Helpers
5. ✅ **Helper creato**: `BlazorDematReports/Helpers/TipoFonteDataHelper.cs`
   - `GetDescription()` - Ottiene testo leggibile
   - `GetColor()` - Ottiene colore MudBlazor
   - `GetIcon()` - Ottiene icona MudBlazor
   - `GetAvailableTypes()` - Filtra tipi obsoleti

6. ✅ **Wizard Step1**: `Step1_TipoFonte.razor`
   - Radio buttons ora usano `TipoFonteData`
   - Usa helper per icone e colori

7. ✅ **Wizard Step2**: `Step2_ConfigurazioneSpecifica.razor`
   - Confronti aggiornati: `State.TipoFonte == TipoFonteData.SQL`

8. ✅ **Wizard State**: `ConfigurationWizardStateService.cs`
   - `TipoFonte` ora è `TipoFonteData?`
   - Metodo `WithTipoFonte(TipoFonteData)`

9. ✅ **Lista Configurazioni**: `PageListaConfigurazioniFonti.razor`
   - `GetTipoColor` usa `TipoFonteDataHelper`

10. ✅ **DTO**: `ConfigurazioneRiepilogoDto.cs`
    - `TipoFonte` aggiornato a `TipoFonteData`

### Database
11. ✅ **Migration di verifica**: `Database/Migrations/20250113_Verify_TipoFonte_Enum_Compatibility.sql`

---

## ⚠️ Lavoro Rimanente (50%) - 33 Errori in 11 File

### 📋 Pattern di Conversione da Applicare

#### Pattern 1: Confronti String → Enum
```csharp
// ❌ PRIMA
if (config.TipoFonte == "SQL")
if (State.TipoFonte == "HandlerIntegrato")

// ✅ DOPO
if (config.TipoFonte == TipoFonteData.SQL)
if (State.TipoFonte == TipoFonteData.HandlerIntegrato)
```

#### Pattern 2: Switch String → Enum
```csharp
// ❌ PRIMA
config.TipoFonte switch
{
    "SQL" => ExecuteSql(),
    "HandlerIntegrato" => ExecuteHandler(),
    _ => throw new Exception()
}

// ✅ DOPO
config.TipoFonte switch
{
    TipoFonteData.SQL => ExecuteSql(),
    TipoFonteData.HandlerIntegrato => ExecuteHandler(),
    _ => throw new Exception()
}
```

#### Pattern 3: Nullable Enum
```csharp
// ❌ PRIMA
if (string.IsNullOrWhiteSpace(State.TipoFonte))

// ✅ DOPO
if (!State.TipoFonte.HasValue)

// ❌ PRIMA
State.TipoFonte ?? "SQL"

// ✅ DOPO
State.TipoFonte ?? TipoFonteData.SQL
```

#### Pattern 4: Conversione Enum → String (quando serve TipoTask)
```csharp
// ConfigurazioneFaseCentro.TipoTask è ancora string
// Serve conversione esplicita

// ❌ PRIMA
TipoTask = State.TipoFonte,  // errore: TipoFonteData → string

// ✅ DOPO - Usa il converter manualmente
TipoTask = State.TipoFonte.HasValue 
    ? new TipoFonteDataConverter().ConvertToProvider(State.TipoFonte.Value) as string
    : null,

// ✅ OPPURE - Pattern matching
TipoTask = State.TipoFonte switch
{
    TipoFonteData.SQL => "SQL",
    TipoFonteData.HandlerIntegrato => "HandlerIntegrato",
    _ => null
},
```

#### Pattern 5: Operatore ?. su Enum
```csharp
// ❌ PRIMA
config.TipoFonte?.ToUpperInvariant()  // errore: TipoFonteData non ha metodi string

// ✅ DOPO
config.TipoFonte.ToString()
```

---

## 📝 File da Aggiornare (Priorità)

### 🔴 Alta Priorità - UI Wizard (Blocca creazione configurazioni)

#### 1. `Step4_Mapping.razor` - 10 errori
**Linee da aggiornare:**
- Linea 30, 52, 116, 182: `State.TipoFonte == "SQL"` → `State.TipoFonte == TipoFonteData.SQL`
- Linea 350: confronto enum
- Linea 369: `TipoTask = State.TipoFonte` → Conversione esplicita (vedi Pattern 4)
- Linea 372, 373, 374, 380: confronti enum

**Esempio fix linea 369:**
```csharp
// ❌ PRIMA
TipoTask = State.TipoFonte,

// ✅ DOPO
TipoTask = State.TipoFonte switch
{
    TipoFonteData.SQL => "SQL",
    TipoFonteData.HandlerIntegrato => "HandlerIntegrato",
    _ => null
},
```

### 🟡 Media Priorità - Services (Impatta task e operazioni)

#### 2. `ServiceConfigurazioneFontiDati.cs` - 2 errori
**Linee 296, 328:**
```csharp
// ❌ PRIMA
m.TipoTask ??= config.TipoFonte;

// ✅ DOPO
m.TipoTask ??= config.TipoFonte switch
{
    TipoFonteData.SQL => "SQL",
    TipoFonteData.HandlerIntegrato => "HandlerIntegrato",
    _ => "SQL"
};
```

#### 3. `ServiceTaskManagement.cs` - 2 errori
**Linea 44, 226:** Stessa conversione enum → string


```

#### 5. `ServiceMail.cs` - 2 errori
**Linee 155-156:** Stessi confronti enum

#### 6. `ConfigurationStepValidator.cs` - 5 errori
**Linee 42-44, 114, 142:** Switch e confronti da aggiornare

#### 7. `ConfigurationWizardStateService.cs` - 2 errori
**Linea 155:** Conversione nullable
```csharp
// ❌ PRIMA
TipoFonte = TipoFonte,  // nullable → non-nullable

// ✅ DOPO
TipoFonte = TipoFonte ?? TipoFonteData.SQL,
```

**Linea 199:** Confronto enum

### 🟢 Bassa Priorità - Infrastructure & Altri

#### 8. `ProductionJobInfrastructure.cs` - 3 errori
**Linea 69, 461, 526:**
```csharp
// ❌ Linea 69
var tipoFonte = t.IdConfigurazioneDatabaseNavigation.TipoFonte?.ToLowerInvariant();

// ✅ DOPO
var tipoFonte = t.IdConfigurazioneDatabaseNavigation.TipoFonte.ToString().ToLowerInvariant();

// ❌ Linea 461
return config.TipoFonte?.ToUpperInvariant() switch { ... }

// ✅ DOPO
return config.TipoFonte switch { ... }

// ❌ Linea 526
if (config.TipoFonte != "SQL")

// ✅ DOPO
if (config.TipoFonte != TipoFonteData.SQL)
```

#### 9. `UnifiedDataSourceHandler.cs` - 2 errori
**Linee 61-62:**
```csharp
// ❌ PRIMA
config.TipoFonte switch
{
    "SQL" => await ExecuteSqlQueryAsync(config, context, ct),
    "HandlerIntegrato" => await ExecuteCustomHandlerAsync(config, context, ct),
    _ => throw new NotSupportedException(...)
};

// ✅ DOPO
config.TipoFonte switch
{
    TipoFonteData.SQL => await ExecuteSqlQueryAsync(config, context, ct),
    TipoFonteData.HandlerIntegrato => await ExecuteCustomHandlerAsync(config, context, ct),
    _ => throw new NotSupportedException(...)
};
```

#### 10. `PageSchedaLavorazione.razor` - 1 errore
**Linea 561:** `c.TipoFonte == "EmailCSV"` → `c.TipoFonte == TipoFonteData.EmailCSV`

#### 11. `PageEditProcedura.razor` - 1 errore
**Linea 129:** `GetTipoColor(context.TipoFonte)` - Parametro già corretto, aggiornare firma metodo

---

## 🔧 Come Procedere

### Opzione A: Fix Manuale Sistematico
1. Aprire ogni file nell'ordine di priorità
2. Applicare i pattern di conversione
3. Testare build dopo ogni 2-3 file
4. Commit incrementali

### Opzione B: Cerca e Sostituisci (ATTENZIONE)
⚠️ **Non raccomandato** - Rischio di sostituzioni errate in commenti/stringhe

### Opzione C: Copilot Batch Fix
1. Aprire un file alla volta
2. Usare Copilot per fix automatici con i pattern sopra
3. Verificare manualmente ogni modifica

---

## ✅ Test di Verifica Post-Fix

1. **Build**: `dotnet build` senza errori
2. **Wizard**: Creare nuova configurazione SQL e HandlerIntegrato
3. **Lista**: Visualizzare configurazioni con chip colore corretto
4. **Task**: Generare e gestire task
5. **Database**: Verificare che i valori rimangano `'SQL'`, `'HandlerIntegrato'` (non cambiati)

---

## 💡 Note Importanti

1. **Database NON cambia**: Il converter gestisce la conversione automaticamente
2. **TipoTask rimane string**: Nella tabella `ConfigurazioneFaseCentro` (legacy)
4. **Commit frequenti**: Questo è un refactoring large-scale

---

## 📊 Progress Tracker

- [x] Entities & Database (4/4)
- [x] DTO Layer (1/1)
- [x] Helpers & Base UI (5/5)
- [ ] Wizard UI (1/2) - Step4 rimanente
- [ ] Services (0/6) - Tutti da fare
- [ ] Infrastructure (0/2)
- [ ] Altri componenti (0/2)

**Totale: 11/22 (50%)**

---

Buona fortuna con il completamento! 🚀
