# ⚠️ Stato Attuale: Riferimenti BlazorDematReports.Core

## 🎯 Problema

Dopo aver spostato i file da `ClassLibraryLavorazioni` a `BlazorDematReports.Core`, i namespace **non sono stati aggiornati** nei file copiati.

## ❌ Errori Identificati (260+)

### Categoria 1: Namespace Non Aggiornati nei File Utility/

| File | Namespace Attuale | Namespace Target |
|------|-------------------|------------------|
| `Utility/BaseLavorazione.cs` | `LibraryLavorazioni.Utility` | `BlazorDematReports.Core.Utility` |
| `Utility/Interfaces/*.cs` | `LibraryLavorazioni.Utility.Interfaces` | `BlazorDematReports.Core.Utility.Interfaces` |
| `Utility/Models/*.cs` | `LibraryLavorazioni.Utility.Models` | `BlazorDematReports.Core.Utility.Models` |

### Categoria 2: Namespace Non Aggiornati nei File Interfaces/

| File | Namespace Attuale | Namespace Target |
|------|-------------------|------------------|
| `Interfaces/ILavorazioneHandler.cs` | `LibraryLavorazioni.Lavorazioni.Interfaces` | `BlazorDematReports.Core.Interfaces` |
| `Interfaces/IUnifiedHandler*.cs` | `LibraryLavorazioni.Shared.Interfaces` | `BlazorDematReports.Core.Interfaces` |

### Categoria 3: Namespace Non Aggiornati nei File Models/

| File | Namespace Attuale | Namespace Target |
|------|-------------------|------------------|
| `Models/LavorazioneExecutionContextBase.cs` | `LibraryLavorazioni.Lavorazioni.Models` | `BlazorDematReports.Core.Models` |
| `Models/UnifiedExecutionContext.cs` | `LibraryLavorazioni.Shared.Models` | `BlazorDematReports.Core.Models` |

### Categoria 4: Package Mancanti

| Package | Versione | File che lo richiedono |
|---------|----------|----------------------|
| `Oracle.ManagedDataAccess.Core` | 23.26.100 | ✅ **AGGIUNTO** |
| `LumenWorksCsvReader` | 4.0.0 | ✅ **AGGIUNTO** |
| `NLog` | 6.1.0 | ✅ **AGGIUNTO** |
| `ClosedXML` | 0.105.0 | ✅ **AGGIUNTO** |

---

## ✅ Cosa È Stato Fatto

1. ✅ **Handler** namespace aggiornati:
   - Ant_Ader4_Sorter_1_2Handler.cs
   - DefaultLavorazioneHandler.cs
   - PraticheSuccessioneHandler.cs
   - Rdmkt_RSPHandler.cs
   - Z0072370_28AutHandler.cs
   - Z0082041_SoftlineHandler.cs
   - Ader4Handler.cs
   - Ader4EmailService.cs
   - Hera16EwsHandler.cs

2. ✅ **Services/Email** namespace aggiornati:
   - BaseEwsEmailService.cs
   - EwsEmailServiceConfig.cs
   - EmailDailyFlagService.cs
   - EmailProcessingResult.cs

3. ✅ **UnifiedDataSourceHandler** namespace aggiornato

4. ✅ **LavorazioniCodes.cs** creato in `Core/Constants/`

5. ✅ **Package NuGet aggiunti** al Core:
   - Oracle.ManagedDataAccess.Core
   - LumenWorksCsvReader
   - NLog
   - ClosedXML

---

## 🔄 Cosa DEVE Essere Fatto

### Step 1: Aggiorna Namespace in Utility/ (30 file)

**Comando PowerShell batch:**
```powershell
Get-ChildItem -Path "BlazorDematReports.Core\Utility" -Filter "*.cs" -Recurse | ForEach-Object {
    (Get-Content $_.FullName) -replace 'namespace LibraryLavorazioni\.Utility', 'namespace BlazorDematReports.Core.Utility' | Set-Content $_.FullName
    (Get-Content $_.FullName) -replace 'using LibraryLavorazioni\.Utility', 'using BlazorDematReports.Core.Utility' | Set-Content $_.FullName
}
```

**File da aggiornare:**
- BaseLavorazione.cs
- LavorazioneDefault.cs
- GestoreOperatoriDatiLavorazione.cs
- ElaboratoreDatiLavorazione.cs
- NormalizzatoreOperatori.cs
- LavorazioniConfigManager.cs
- Interfaces/IElaboratoreDatiLavorazione.cs
- Interfaces/IGestoreOperatoriDatiLavorazione.cs
- Interfaces/INormalizzatoreOperatori.cs
- Interfaces/ILavorazioniConfigManager.cs
- Interfaces/IFinalizzatoreDati.cs
- Models/DatiLavorazione.cs
- Models/DatiElaborati.cs
- Models/OperatoreMondo.cs
- ConnectionAttribute.cs
- ProceduraLavorazioneAttribute.cs

### Step 2: Aggiorna Namespace in Interfaces/ (4 file)

**Namespace attesi:**
```csharp
namespace BlazorDematReports.Core.Interfaces
```

**File:**
- ILavorazioneHandler.cs
- IUnifiedHandler.cs
- IUnifiedHandlerService.cs
- IUnifiedHandlerRegistry.cs

### Step 3: Aggiorna Namespace in Models/ (2 file)

**Namespace attesi:**
```csharp
namespace BlazorDematReports.Core.Models
```

**File:**
- LavorazioneExecutionContextBase.cs
- UnifiedExecutionContext.cs

### Step 4: Aggiorna Namespace in Wrappers/ (1 file)

**Namespace atteso:**
```csharp
namespace BlazorDematReports.Core.Wrappers
```

**File:**
- LavorazioneHandlerWrapper.cs

### Step 5: Aggiorna Namespace in Handlers/Registry/ (1 file)

**Namespace atteso:**
```csharp
namespace BlazorDematReports.Core.Handlers.Registry
```

**File:**
- UnifiedHandlerRegistry.cs

---

## 📝 Script Automatico Completo

```powershell
# Fix-All-Namespaces.ps1
$corePath = "C:\Users\SMARTW\source\repos\BlazorDematReports_10\BlazorDematReports.Core"

# Mappings
$namespaceReplacements = @(
    @{Old="LibraryLavorazioni.Utility"; New="BlazorDematReports.Core.Utility"},
    @{Old="LibraryLavorazioni.Utility.Interfaces"; New="BlazorDematReports.Core.Utility.Interfaces"},
    @{Old="LibraryLavorazioni.Utility.Models"; New="BlazorDematReports.Core.Utility.Models"},
    @{Old="LibraryLavorazioni.Lavorazioni.Interfaces"; New="BlazorDematReports.Core.Interfaces"},
    @{Old="LibraryLavorazioni.Lavorazioni.Models"; New="BlazorDematReports.Core.Models"},
    @{Old="LibraryLavorazioni.Shared.Models"; New="BlazorDematReports.Core.Models"},
    @{Old="LibraryLavorazioni.Shared.Interfaces"; New="BlazorDematReports.Core.Interfaces"},
    @{Old="LibraryLavorazioni.Shared.Registry"; New="BlazorDematReports.Core.Handlers.Registry"},
    @{Old="LibraryLavorazioni.Shared.Wrappers"; New="BlazorDematReports.Core.Wrappers"}
)

$files = Get-ChildItem -Path $corePath -Filter "*.cs" -Recurse | 
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' }

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    foreach ($replacement in $namespaceReplacements) {
        $oldNamespace = $replacement.Old
        $newNamespace = $replacement.New
        
        # Replace namespace declaration
        if ($content -match "namespace $([regex]::Escape($oldNamespace))") {
            $content = $content -replace "namespace $([regex]::Escape($oldNamespace))", "namespace $newNamespace"
            $modified = $true
        }
        
        # Replace using statements
        if ($content -match "using $([regex]::Escape($oldNamespace));") {
            $content = $content -replace "using $([regex]::Escape($oldNamespace));", "using $newNamespace;"
            $modified = $true
        }
    }
    
    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "✅ $($file.Name)" -ForegroundColor Green
    }
}
```

---

## 🎯 Strategia Raccomandata

### Opzione A: Automatica (5 min) ⭐ **RACCOMANDATO**
1. Esegui script PowerShell `Fix-All-Namespaces.ps1`
2. Verifica build: `dotnet build BlazorDematReports.Core`
3. Correggi eventuali errori residui manualmente

### Opzione B: Manuale (2 ore)
1. Apri ogni file in Visual Studio
2. Usa Find/Replace per aggiornare namespace
3. Salva e compila

---

## 📊 Stima Tempo

| Approccio | Tempo | Rischio Errori |
|-----------|-------|----------------|
| Script PowerShell | 5 min | Basso |
| Visual Studio Batch Replace | 15 min | Medio |
| Manuale File-by-File | 2 ore | Alto |

---

## ✅ Checklist Post-Fix

- [ ] Utility/ namespace aggiornati
- [ ] Interfaces/ namespace aggiornati
- [ ] Models/ namespace aggiornati
- [ ] Wrappers/ namespace aggiornato
- [ ] Handlers/Registry/ namespace aggiornato
- [ ] Build BlazorDematReports.Core riuscita
- [ ] Build BlazorDematReports (UI) riuscita
- [ ] Nessun warning namespace vecchi

---

**Data:** 2025-01-17  
**Status:** ⚠️ **IN PROGRESS - 260 errori da risolvere**  
**Prossimo Step:** Eseguire script automatico Fix-All-Namespaces.ps1
