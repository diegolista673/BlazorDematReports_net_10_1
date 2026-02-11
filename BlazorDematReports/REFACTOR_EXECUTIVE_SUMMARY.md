# Executive Summary: Refactoring Handler Mail

## Situazione Attuale ? ANALIZZATA

### Handler Mail Esistenti
- **Hera16EwsHandler** ? Servizio mail HERA16
- **Ader4Handler** ? Servizio mail ADER4/Equitalia

### Architettura Corrente
```
IMailImportHandler (interfaccia specifica)
    ??? Hera16EwsHandler
    ??? Ader4Handler
    ??? MailImportHandlerWrapper (wrapper dedicato)

ILavorazioneHandler (interfaccia generica)
    ??? Z0072370_28AUTHandler
    ??? ANT_ADER4_SORTER_1_2Handler
    ??? LavorazioneHandlerWrapper (wrapper generico)
```

**Problema**: Due sistemi paralleli per gestire handler, duplicazione di codice e complessità.

---

## Obiettivo Refactoring

### Architettura Target
```
ILavorazioneHandler (interfaccia unica)
    ??? Hera16EwsHandler (migrato)
    ??? Ader4Handler (migrato)
    ??? Z0072370_28AUTHandler
    ??? LavorazioneHandlerWrapper (unico wrapper)
```

**Benefici**:
- ? Una sola interfaccia per tutti gli handler
- ? Un solo wrapper generico
- ? Discovery automatico unificato
- ? Riduzione complessità
- ? UI consistente nel wizard

---

## Impatto Stimato

### Complessità
- **Tecnica**: ?? (MEDIA)
- **Rischio**: ? (BASSO)
- **Tempo**: 9-14 ore

### File da Modificare
**Eliminare** (2 file):
- `IMailImportHandler.cs`
- `MailImportHandlerWrapper.cs`

**Migrare** (2 file):
- `Hera16EwsHandler.cs`
- `Ader4Handler.cs`

**Estendere** (2 file):
- `ILavorazioneHandler.cs` (aggiungere metadata)
- `HandlerDiscoveryService.cs` (metadata extraction)

**Aggiornare UI** (1 file):
- `Step2_ConfigurazioneSpecifica.razor` (già parzialmente fatto)

---

## Piano Esecuzione (7 Step)

### Fase 1: Preparazione ? COMPLETATA
- [x] STEP 1: Analisi dipendenze ? **MAIL_HANDLER_DEPENDENCIES.md**
- [ ] STEP 2: Estendere ILavorazioneHandler con metadata

### Fase 2: Migrazione
- [ ] STEP 3: Migrare Hera16EwsHandler
- [ ] STEP 4: Migrare Ader4Handler
- [ ] STEP 5: Eliminare MailImportHandlerWrapper

### Fase 3: Discovery e UI
- [ ] STEP 6: Aggiornare HandlerDiscoveryService
- [ ] STEP 7: Migliorare UI wizard

### Fase 4: Cleanup
- [ ] Eliminare file obsoleti
- [ ] Aggiornare documentazione
- [ ] Testing completo

---

## Esempio Conversione

### Prima (Handler Mail Specifico)
```csharp
public sealed class Hera16EwsHandler : IMailImportHandler
{
    public string ServiceCode => JobConstants.MailServiceCodes.Hera16;
    
    public Task<int> ExecuteAsync(IServiceProvider sp, MailImportExecutionContext ctx, CancellationToken ct)
        => sp.GetRequiredService<UnifiedMailProduzioneService>().ProcessHera16Async(ct);
}
```

### Dopo (Handler Standard con Metadata)
```csharp
[HandlerCode("HERA16")]
[Description("Import dati HERA16 da allegati email")]
public sealed class Hera16EwsHandler : ILavorazioneHandler
{
    private readonly UnifiedMailProduzioneService _mailService;
    
    public Hera16EwsHandler(UnifiedMailProduzioneService mailService)
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
        var result = await _mailService.ProcessHera16Async(CancellationToken.None);
        // Convertire result in DataTable se necessario
        return ConvertToDataTable(result);
    }
}
```

**Cambiamenti**:
1. ? Implementa `ILavorazioneHandler` invece di `IMailImportHandler`
2. ? Dependency injection esplicita (costruttore)
3. ? Metadata method per esporre ServiceCode
4. ? Firma `ExecuteAsync` standard (DateTime, DateTime, string)
5. ? Attributi per discovery automatico

---

## Rischi e Mitigazioni

### ? Configurazioni Esistenti
**Rischio**: Configurazioni DB con `TipoFonte = "EmailCSV"` smettono di funzionare  
**Mitigazione**: `ServiceMail.MailConfigurationQuery` supporta ENTRAMBE le modalità  
**Status**: RISOLTO

### ?? Task Mail in Produzione
**Rischio**: Task mail schedulati falliscono dopo deploy  
**Mitigazione**:
- Testing estensivo pre-deploy
- Deploy in orario di bassa attività
- Rollback plan documentato

### ? Breaking Changes
**Rischio**: Codice esistente si rompe  
**Mitigazione**: Default implementation in `ILavorazioneHandler` garantisce backward compatibility  
**Status**: RISOLTO

---

## Criteri di Successo

### Tecnici
- [ ] `dotnet build` senza errori
- [ ] Tutti i test unitari passano
- [ ] Handler mail funzionano come prima
- [ ] Discovery automatico trova handler mail

### Funzionali
- [ ] Wizard mostra handler mail con icona email
- [ ] Creazione config mail tramite handler funziona
- [ ] Task mail esistenti continuano a eseguire
- [ ] ServiceMail conta correttamente configurazioni

### Qualità
- [ ] Code coverage >= 80%
- [ ] Nessun warning SonarQube
- [ ] Documentazione aggiornata
- [ ] Zero duplicazione codice

---

## Timeline

| Fase | Attività | Tempo | Status |
|------|----------|-------|--------|
| 1 | Analisi dipendenze | 2h | ? COMPLETATO |
| 1 | Estendere interfaccia | 1h | ?? PROSSIMO |
| 2 | Migrare Hera16Handler | 2h | ?? PENDING |
| 2 | Migrare Ader4Handler | 2h | ?? PENDING |
| 2 | Eliminare wrapper | 1h | ?? PENDING |
| 3 | Aggiornare discovery | 2h | ?? PENDING |
| 3 | Migliorare UI | 1h | ?? PENDING |
| 4 | Cleanup + doc | 2h | ?? PENDING |
| **TOTALE** | | **13h** | **8% COMPLETATO** |

---

## Decisione

### ? RACCOMANDAZIONE: PROCEDERE

**Motivazioni**:
1. Impatto tecnico BASSO (solo 2 handler da migrare)
2. Rischio BASSO (backward compatibility garantita)
3. Beneficio ALTO (semplificazione architettura)
4. Analisi COMPLETATA (nessun blocco identificato)

### Prossimi Passi Immediati

1. **Creare branch Git**
   ```bash
   git checkout -b feature/unify-mail-handlers
   ```

2. **Eseguire STEP 2**: Estendere ILavorazioneHandler
   - Aggiungere metodi metadata con default implementation
   - Creare `HandlerMetadata` record
   - Scrivere unit test

3. **Review + Checkpoint**: Build senza errori

---

## Riferimenti

- ?? Piano dettagliato: `REFACTOR_MAIL_HANDLERS_PLAN.md`
- ?? Analisi dipendenze: `MAIL_HANDLER_DEPENDENCIES.md`
- ?? Copilot instructions: `.github/copilot-instructions.md`
- ?? Note sviluppo: `NOTE.txt`

---

**Creato**: 2025-01-XX  
**Ultimo aggiornamento**: 2025-01-XX  
**Responsabile**: Team Development  
**Status**: ? APPROVATO PER ESECUZIONE
