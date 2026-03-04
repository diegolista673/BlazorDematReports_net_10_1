# README - Scaffolding Entity Framework

## ⚠️ Importante: Customizzazioni Manuali

Alcune entity sono state modificate manualmente dopo lo scaffolding e **NON devono essere sovrascritte** completamente.

### Entity Customizzate

#### `ConfigurazioneFontiDati.cs`

**Modifica:** La proprietà `TipoFonte` è stata cambiata da `string` a `TipoFonteData` (enum).

```csharp
// ❌ Versione scaffoldato (SBAGLIATA):
public string TipoFonte { get; set; } = null!;

// ✅ Versione corretta (da mantenere):
public TipoFonteData TipoFonte { get; set; }
```

**Motivo:** Il database contiene stringhe (`"SQL"`, `"HandlerIntegrato"`), ma nel codice usiamo l'enum per type-safety. La conversione è gestita automaticamente dal `TipoFonteDataConverter` configurato in `DematReportsContextExtension.cs`.

---

## Procedura di Scaffolding Sicura

### Opzione 1: Scaffolding Parziale (Raccomandato)

Rigenerare solo le entity necessarie, escludendo quelle customizzate:

```powershell
# Scaffolding di una singola tabella
dotnet ef dbcontext scaffold "YourConnectionString" Microsoft.EntityFrameworkCore.SqlServer `
    --context DematReportsContext `
    --table NomeTabella `
    --output-dir Models/DbApplication `
    --force
```

### Opzione 2: Scaffolding Completo + Fix Automatico

Se necessario rigenerare tutto:

#### 1. Backup delle customizzazioni
```powershell
Copy-Item "Entities\Models\DbApplication\ConfigurazioneFontiDati.cs" `
          "Entities\Models\DbApplication\ConfigurazioneFontiDati.cs.backup"
```

#### 2. Esegui lo scaffolding
```powershell
dotnet ef dbcontext scaffold "YourConnectionString" Microsoft.EntityFrameworkCore.SqlServer `
    --context DematReportsContext `
    --output-dir Models/DbApplication `
    --force
```

#### 3. Esegui lo script di fix
```powershell
.\Entities\Models\DbApplication\fix-scaffolding.ps1
```

---

## Script di Fix Automatico

Lo script `fix-scaffolding.ps1` esegue automaticamente:

1. ✅ Ripristina `TipoFonte` come `TipoFonteData` (enum)
2. ✅ Aggiunge `using Entities.Enums;`
3. ✅ Ripristina il banner di warning
4. ✅ Verifica che la classe sia `partial`

---

## Verifica Manuale

Dopo lo scaffolding, verifica che:

1. ✅ `ConfigurazioneFontiDati.cs` contiene:
   - `using Entities.Enums;`
   - `public TipoFonteData TipoFonte { get; set; }`
   - Banner di warning nel commento iniziale

2. ✅ Il progetto compila senza errori:
   ```powershell
   dotnet build
   ```

3. ✅ L'applicazione si avvia correttamente

---

## Troubleshooting

### Errore: "Cannot convert from 'TipoFonteData' to 'string'"

**Causa:** La proprietà `TipoFonte` è stata rigenerata come `string`.

**Soluzione:**
```powershell
.\Entities\Models\DbApplication\fix-scaffolding.ps1
```

### Errore: "Converter for model type 'TipoFonteData' cannot be used"

**Causa:** Il converter è configurato ma la proprietà è `string`.

**Soluzione:** Vedi soluzione precedente.

---

## Prevenzione Futura

Per evitare problemi con lo scaffolding:

1. **Documentare sempre** le customizzazioni in questo README
2. **Eseguire backup** prima dello scaffolding
3. **Testare immediatamente** dopo lo scaffolding
4. **Automatizzare il fix** con lo script PowerShell

---

## Contatti

Per domande sullo scaffolding, contattare il team di sviluppo.
