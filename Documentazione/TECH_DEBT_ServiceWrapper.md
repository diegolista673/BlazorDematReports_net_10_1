# TECH DEBT: ServiceWrapper Antipattern

## 🔴 Problema identificato

`ServiceWrapper` implementa il **Service Locator antipattern** riconosciuto come problematico da:
- Martin Fowler (Dependency Injection vs Service Locator)
- Microsoft .NET Architecture Guidelines
- SOLID Principles

---

## ❌ Problemi architetturali

| Issue | Gravità | Descrizione |
|---|---|---|
| **Service Locator** | 🔴 Critica | Nasconde dipendenze, complica testing |
| **God Object** | 🟡 Media | Aggrega 23 servizi — troppo responsabilità |
| **Lazy&lt;T&gt; inutile** | 🟡 Media | Lazy Loading non serve in Scoped lifetime |
| **Viola SOLID** | 🟡 Media | SRP, ISP (Interface Segregation), DIP |
| **Testing complexity** | 🟡 Media | Mock 23 servizi anche se ne usi 1 |

---

## 📊 Dettaglio ServiceWrapper

### Servizi aggregati (23 totali)

```csharp
public interface IServiceWrapper
{
    IServiceCentri ServiceCentri { get; }
    IServiceOperatori ServiceOperatori { get; }
    IServiceClienti ServiceClienti { get; }
    IServiceProcedureClienti ServiceProcedureClienti { get; }
    IServiceProcedureLavorazioni ServiceProcedureLavorazioni { get; }
    IServiceFormatoDati ServiceFormatoDati { get; }
    IServiceRepartiProduzione ServiceRepartiProduzione { get; }
    IServiceFasiLavorazioni ServiceFasiLavorazioni { get; }
    IServiceProduzioneOperatori ServiceProduzioneOperatori { get; }
    IServiceTipologieTotali ServiceTipologieTotali { get; }
    IServiceLavorazioniFasiTipoTotale ServiceLavorazioniFasiTipoTotale { get; }
    IServiceTurni ServiceTurni { get; }
    IServiceProduzioneSistema ServiceProduzioneSistema { get; }
    IServiceConfigReportDocumenti ServiceConfigReportDocumenti { get; }
    IServiceOperatoriNormalizzati ServiceOperatoriNormalizzati { get; }
    IServiceTaskDataReadingAggiornamento ServiceTaskDataReadingAggiornamento { get; }
    IServiceQueryProcedureLavorazioni ServiceQueryProcedureLavorazioni { get; }
    IServiceCentriVisibili ServiceCentriVisibili { get; }
    IServiceTipoTurni ServiceTipoTurni { get; }
    IServiceTaskDaEseguire ServiceTaskDaEseguire { get; }
    IServiceRuoli ServiceRuoli { get; }
    IServiceConfigurazioneFontiDati ServiceConfigurazioneFontiDati { get; }
    IServiceMail ServiceMail { get; }
    IServiceTaskManagement ServiceTaskManagement { get; }
}
```

### Lazy&lt;T&gt; non serve in Scoped

```csharp
// ServiceWrapper è AddScoped (ricreato ogni request)
builder.Services.AddScoped<IServiceWrapper, ServiceWrapper>();

// Lazy<T> NON offre benefici:
_serviceCentri = new Lazy<IServiceCentri>(() => 
    new ServiceCentri(mapper, configUser, contextFactory, ...));
    
// ❌ ServiceCentri verrà distrutto alla fine della request
// ❌ Il Lazy<> aggiunge overhead senza risparmiare nulla
```

**Lazy Loading serve solo per Singleton costosi**, non per Scoped leggeri.

---

## ✅ Soluzione: Dependency Injection diretta

### Esempio refactoring

```razor
<!-- PRIMA (antipattern) -->
@page "/operatori"
@inherits BaseComponentPage<PageOperatori>

@code {
    // ❌ Dipendenza nascosta
    protected override async Task OnInitializedAsync()
    {
        var operatori = await ServiceWrapper.ServiceOperatori.GetAllAsync();
        var centri = await ServiceWrapper.ServiceCentri.GetAllAsync();
        var ruoli = await ServiceWrapper.ServiceRuoli.GetAllAsync();
        // ↑ Non sai dalla signature che servizi usa
    }
}

<!-- DOPO (best practice) -->
@page "/operatori"
@inject IServiceOperatori ServiceOperatori
@inject IServiceCentri ServiceCentri
@inject IServiceRuoli ServiceRuoli
@inject ISnackbar Snackbar

@code {
    // ✅ Dipendenze ESPLICITE
    protected override async Task OnInitializedAsync()
    {
        var operatori = await ServiceOperatori.GetAllAsync();
        var centri = await ServiceCentri.GetAllAsync();
        var ruoli = await ServiceRuoli.GetAllAsync();
        // ↑ Chiaro dalla signature, mockabile singolarmente
    }
}
```

---

## 📋 Componenti da migrare

| Componente | Servizi usati | Priorità | Status |
|---|---|---|---|
| `PageProcedureLavorazioni.razor` | ServiceProcedureLavorazioni, ServiceProcedureClienti, ServiceFormatoDati, ServiceRepartiProduzione | Alta | ⏳ TODO |
| `PageListaConfigurazioniFonti.razor` | ServiceConfigurazioneFontiDati | Alta | ⏳ TODO |
| `ConfigurazioneFontiWizard.razor` | ServiceConfigurazioneFontiDati | Alta | ⏳ TODO |
| `PageCaricaDati.razor` | ServiceProduzioneSistema, ServiceProduzioneOperatori | Media | ⏳ TODO |
| `PageGestioneOperatori.razor` | ServiceOperatori, ServiceCentri | Media | ⏳ TODO |
| `PageProduzioneOperatore.razor` | ServiceProduzioneOperatori | Media | ⏳ TODO |
| `PageProcedureClienti.razor` | ServiceProcedureClienti, ServiceClienti | Bassa | ⏳ TODO |
| `Step3_SelezioneProcedura.razor` | ServiceProcedureLavorazioni | Bassa | ✅ MIGRATO |
| `ProcedureGeneralForm.razor` | ServiceFormatoDati, ServiceRepartiProduzione | Bassa | ⏳ TODO |
| `ProcedurePhasesViewer.razor` | ServiceFasiLavorazioni | Bassa | ⏳ TODO |

**Progresso migrazione:** 1/10 (10%) ✅

---

## 🎯 Piano di migrazione graduale

### Fase 1: Freeze (attuale)
- ✅ Documentato l'antipattern
- ✅ Nuovi componenti NON usano ServiceWrapper
- ⏳ Componenti esistenti lo usano ancora

### Fase 2: Migrazione opportunistica
- Quando modifichi un componente → rimuovi ServiceWrapper
- Aggiungi `@inject IService{Nome}` per servizi necessari
- Sostituisci `ServiceWrapper.Service{Nome}` → `Service{Nome}`
- Test componente modificato

### Fase 3: Rimozione finale
- Una volta migrati tutti i 10 componenti
- Rimuovi `ServiceWrapper.cs`
- Rimuovi `IServiceWrapper.cs`
- Rimuovi registrazione da `Program.cs`

---

## 📈 Benefici post-migrazione

| Metrica | Prima | Dopo | Delta |
|---|---|---|---|
| **Righe codice** | +280 (ServiceWrapper + interface) | 0 | -280 |
| **Dipendenze nascoste** | 23 in 1 wrapper | 2-4 esplicite | -90% |
| **Testing** | Mock 23 servizi | Mock 2-4 usati | -80% setup |
| **Lazy overhead** | 23 Lazy&lt;T&gt; allocations | 0 | -23 allocations |
| **SOLID compliance** | ❌ Viola SRP, ISP, DIP | ✅ Compliant | ✅ |
| **Manutenibilità** | ⚠️ Dipendenze nascoste | ✅ Esplicite | ✅ |

---

## 🔧 Template refactoring componente

### Step-by-step per ogni componente

1. **Identifica servizi usati**
```powershell
Get-Content Component.razor | Select-String "ServiceWrapper\.Service"
# Output: ServiceOperatori, ServiceCentri
```

2. **Aggiungi @inject**
```razor
@inject IServiceOperatori ServiceOperatori
@inject IServiceCentri ServiceCentri
```

3. **Sostituisci chiamate**
```razor
<!-- Prima -->
var data = await ServiceWrapper.ServiceOperatori.GetAllAsync();

<!-- Dopo -->
var data = await ServiceOperatori.GetAllAsync();
```

4. **Rimuovi ServiceWrapper da BaseComponentPage** (opzionale dopo migrazione completa)

5. **Test**
- Verifica build
- Test funzionalità componente
- Verifica nessun null reference

---

## 🚀 Quick wins (componenti piccoli)

Migra prima questi (< 5 minuti ciascuno):

1. **Step3_SelezioneProcedura.razor** — usa solo `ServiceProcedureLavorazioni`
2. **ProcedurePhasesViewer.razor** — usa solo `ServiceFasiLavorazioni`
3. **PageListaConfigurazioniFonti.razor** — usa solo `ServiceConfigurazioneFontiDati`

---

## 📚 References

- [Dependency Injection vs Service Locator (Martin Fowler)](https://martinfowler.com/articles/injection.html)
- [.NET Dependency Injection Guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)
- [Blazor DI Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection)
- [Interface Segregation Principle (SOLID)](https://en.wikipedia.org/wiki/Interface_segregation_principle)

---

## ⚠️ Importante

**Non eliminare ServiceWrapper fino a migrazione completa dei 10 componenti.**  
Breaking changes su UI in produzione = alto rischio.

Migrazione graduale è l'approccio più sicuro. ✅
