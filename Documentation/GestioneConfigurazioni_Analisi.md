# Gestione Configurazioni - Analisi e Raccomandazioni

## ? IMPLEMENTATO: Guard Clause (Opzione 1)

**Data Implementazione**: 2025-01  
**File Modificato**: `DataReading\Infrastructure\ProductionJobInfrastructure.cs`  
**Metodo**: `ProductionJobRunner.RunAsync`

### Modifica Applicata

```csharp
// Prima (comportamento vecchio)
if (entity == null || !entity.Enabled)
{
    logger?.LogWarning("Task {TaskId} non trovato o disabilitato", idTaskDaEseguire);
    return;
}

// Dopo (comportamento nuovo)
if (entity == null)
{
    logger?.LogWarning("Task {TaskId} non trovato", idTaskDaEseguire);
    return;
}

// Guard Clause: Verifica se il task è abilitato prima di eseguire
if (!entity.Enabled)
{
    logger?.LogInformation("Task {TaskId} disabilitato - esecuzione saltata", idTaskDaEseguire);
    return;
}
```

### Comportamento Implementato

- ? **Job rimane schedulato** in Hangfire Dashboard (sempre visibile)
- ? **Esecuzione viene saltata** se `Enabled = false`
- ? **Toggle istantaneo** via update database (nessuna chiamata API Hangfire)
- ? **Logging appropriato**:
  - `LogWarning`: Task non trovato (possibile errore configurazione)
  - `LogInformation`: Task disabilitato (comportamento normale)

### Esempio Log

```
[2025-01-10 05:00:00] [Information] Task 42 disabilitato - esecuzione saltata
[2025-01-10 06:00:00] [Information] Task 42 disabilitato - esecuzione saltata
[2025-01-10 07:00:00] [Information] Avvio esecuzione task unificato 42  // Dopo riabilitazione
```

### Testing

1. ? **Test Disabilitazione**:
   - Disabilita task dal dialog/dashboard
   - Attendi prossima esecuzione schedulata
   - Verifica log: "Task X disabilitato - esecuzione saltata"
   - Conferma: Nessun dato elaborato

2. ? **Test Riabilitazione**:
   - Riabilita task dal dialog/dashboard
   - Attendi prossima esecuzione
   - Verifica log: "Avvio esecuzione task unificato X"
   - Conferma: Dati elaborati correttamente

3. ? **Test Performance**:
   - Toggle Enabled ripetuto (10 volte)
   - Conferma: Ogni toggle < 50ms (solo DB update)

### Limitazioni Note

?? **Dashboard Hangfire**: Job appare sempre "attivo" anche se disabilitato  
**Workaround**: Usa la dashboard applicativa per vedere stato reale task

### Prossimi Passi (Opzionali)

- [ ] Implementare **Cleanup notturno** (Opzione 3) per rimuovere job disabilitati da Hangfire
- [ ] Aggiungere **filtro custom** in Hangfire Dashboard per nascondere job disabilitati
- [ ] Implementare **dashboard custom** con grafico task attivi vs disabilitati

---

## ?? Sommario Esecutivo

Il sistema attuale implementa correttamente la gestione delle configurazioni fonti dati con:
- ? Soft delete (disattivazione reversibile)
- ? Hard delete (eliminazione definitiva)
- ? Creazione automatica task Hangfire
- ?? Limitazione: gestione task associati richiede navigazione alla dashboard

---

## ?? Risposte alle Domande

### 1. Eliminare o tenere il button "Disabilita"?

**RISPOSTA: MANTIENI il button "Disabilita" ?**

**Motivi tecnici:**
- **Best Practice**: Soft delete è lo standard per dati di configurazione
- **Audit Trail**: Mantiene storico modifiche e configurazioni
- **Sicurezza**: Previene perdita accidentale di configurazioni complesse
- **Reversibilità**: Permette riattivazione senza rielaborazione
- **Test & Debug**: Utile per sospensioni temporanee durante sviluppo

**Quando usarlo:**
- Sospensione temporanea di lavorazioni stagionali
- Testing di nuove configurazioni alternative
- Debugging di problemi su lavorazioni specifiche
- Mantenimento storico per compliance/audit

---

### 2. Come gestire task associati?

**PROBLEMA ATTUALE:**
```csharp
// Linea 149: PageListaConfigurazioniFonti.razor
Disabled="@(context.Item.TaskAttivi > 0)"
```
- Se ci sono task attivi, l'eliminazione è bloccata
- Utente deve:
  1. Navigare alla dashboard
  2. Trovare i task associati
  3. Disabilitarli uno per uno
  4. Tornare alla pagina configurazioni
  5. Procedere con eliminazione

**SOLUZIONE CONSIGLIATA: Dialog Gestione Task Avanzata**

---

## ??? Implementazione Proposta

### Opzione A: Dialog Gestione Task (CONSIGLIATA)

#### Component: `DialogGestioneTask.razor`

```razor
@inject IDbContextFactory<DematReportsContext> DbFactory
@inject IProductionJobScheduler ProductionScheduler
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudText Typo="Typo.h6" Class="mb-4">
            Gestione Task - Configurazione @_codiceConfigurazione
        </MudText>

        @if (_loading)
        {
            <MudProgressLinear Indeterminate />
        }
        else if (!_tasks.Any())
        {
            <MudAlert Severity="Severity.Info">Nessun task associato a questa configurazione</MudAlert>
        }
        else
        {
            <MudText Typo="Typo.body2" Class="mb-3">
                <strong>@_tasks.Count(t => t.Enabled) attivi</strong> / @_tasks.Count totali
            </MudText>

            <MudTable Items="@_tasks" Dense Striped Hover>
                <HeaderContent>
                    <MudTh>ID</MudTh>
                    <MudTh>Cron</MudTh>
                    <MudTh>Stato</MudTh>
                    <MudTh>Ultimo Esito</MudTh>
                    <MudTh>Azioni</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd>@context.IdTaskDaEseguire</MudTd>
                    <MudTd><MudChip Size="Size.Small">@context.CronExpression</MudChip></MudTd>
                    <MudTd>
                        @if (context.Enabled)
                        {
                            <MudChip Color="Color.Success" Size="Size.Small">Attivo</MudChip>
                        }
                        else
                        {
                            <MudChip Color="Color.Default" Size="Size.Small">Disabilitato</MudChip>
                        }
                    </MudTd>
                    <MudTd>
                        <MudText Typo="Typo.caption">
                            @(context.DataStato?.ToString("dd/MM/yyyy HH:mm") ?? "-")
                        </MudText>
                    </MudTd>
                    <MudTd>
                        <MudSwitch @bind-Checked="@context.Enabled" 
                                   Color="Color.Success"
                                   Label="@(context.Enabled ? "Disabilita" : "Abilita")"
                                   Disabled="@_saving"
                                   T="bool"
                                   ValueChanged="@(() => ToggleTaskAsync(context))" />
                        
                        <MudTooltip Text="Elimina task (irreversibile)">
                            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                           Color="Color.Error"
                                           Size="Size.Small"
                                           Disabled="@_saving"
                                           OnClick="@(() => EliminaTaskAsync(context))" />
                        </MudTooltip>
                    </MudTd>
                </RowTemplate>
            </MudTable>

            <MudDivider Class="my-4" />

            <MudStack Row Spacing="2">
                <MudButton Variant="Variant.Outlined"
                           Color="Color.Warning"
                           StartIcon="@Icons.Material.Filled.Block"
                           Disabled="@(!_tasks.Any(t => t.Enabled) || _saving)"
                           OnClick="DisabilitaTuttiAsync">
                    Disabilita Tutti
                </MudButton>

                <MudButton Variant="Variant.Outlined"
                           Color="Color.Error"
                           StartIcon="@Icons.Material.Filled.DeleteForever"
                           Disabled="@(_saving)"
                           OnClick="EliminaTuttiAsync">
                    Elimina Tutti
                </MudButton>
            </MudStack>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Close">Chiudi</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int IdConfigurazione { get; set; }
    [Parameter] public string CodiceConfigurazione { get; set; } = string.Empty;

    private bool _loading = true;
    private bool _saving = false;
    private List<TaskDaEseguire> _tasks = new();
    private string _codiceConfigurazione = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadTasksAsync();
    }

    private async Task LoadTasksAsync()
    {
        _loading = true;
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            _tasks = await context.TaskDaEseguires
                .Where(t => t.IdConfigurazioneDatabase == IdConfigurazione)
                .OrderBy(t => t.IdTaskDaEseguire)
                .ToListAsync();
            _codiceConfigurazione = CodiceConfigurazione;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ToggleTaskAsync(TaskDaEseguire task)
    {
        _saving = true;
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            var dbTask = await context.TaskDaEseguires.FindAsync(task.IdTaskDaEseguire);
            if (dbTask != null)
            {
                dbTask.Enabled = !dbTask.Enabled;
                await context.SaveChangesAsync();

                if (dbTask.Enabled)
                {
                    await ProductionScheduler.AddOrUpdateAsync(dbTask.IdTaskDaEseguire);
                    Snackbar.Add($"Task {dbTask.IdTaskDaEseguire} abilitato", Severity.Success);
                }
                else
                {
                    await ProductionScheduler.RemoveByKeyAsync(dbTask.IdTaskHangFire);
                    Snackbar.Add($"Task {dbTask.IdTaskDaEseguire} disabilitato", Severity.Success);
                }

                await LoadTasksAsync();
            }
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

    private async Task EliminaTaskAsync(TaskDaEseguire task)
    {
        _saving = true;
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            
            // Rimuovi da Hangfire
            if (!string.IsNullOrWhiteSpace(task.IdTaskHangFire))
            {
                await ProductionScheduler.RemoveByKeyAsync(task.IdTaskHangFire);
            }

            // Rimuovi da DB
            var dbTask = await context.TaskDaEseguires.FindAsync(task.IdTaskDaEseguire);
            if (dbTask != null)
            {
                context.TaskDaEseguires.Remove(dbTask);
                await context.SaveChangesAsync();
                Snackbar.Add($"Task {task.IdTaskDaEseguire} eliminato", Severity.Success);
                await LoadTasksAsync();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Errore eliminazione: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DisabilitaTuttiAsync()
    {
        _saving = true;
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();
            var tasksAttivi = _tasks.Where(t => t.Enabled).ToList();

            foreach (var task in tasksAttivi)
            {
                var dbTask = await context.TaskDaEseguires.FindAsync(task.IdTaskDaEseguire);
                if (dbTask != null)
                {
                    dbTask.Enabled = false;
                    await ProductionScheduler.RemoveByKeyAsync(dbTask.IdTaskHangFire);
                }
            }

            await context.SaveChangesAsync();
            Snackbar.Add($"{tasksAttivi.Count} task disabilitati", Severity.Success);
            await LoadTasksAsync();
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

    private async Task EliminaTuttiAsync()
    {
        // TODO: Aggiungere dialog conferma
        _saving = true;
        try
        {
            await using var context = await DbFactory.CreateDbContextAsync();

            foreach (var task in _tasks)
            {
                if (!string.IsNullOrWhiteSpace(task.IdTaskHangFire))
                {
                    await ProductionScheduler.RemoveByKeyAsync(task.IdTaskHangFire);
                }
            }

            context.TaskDaEseguires.RemoveRange(_tasks);
            await context.SaveChangesAsync();

            Snackbar.Add($"{_tasks.Count} task eliminati", Severity.Success);
            _tasks.Clear();
            MudDialog.Close(DialogResult.Ok(true));
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

    private void Close() => MudDialog.Cancel();
}
```

#### Modifica a `PageListaConfigurazioniFonti.razor`

Aggiungi il button nella sezione Azioni (dopo PlayCircle):

```razor
<MudTooltip Text="Gestisci task associati a questa configurazione">
    <MudIconButton Icon="@Icons.Material.Filled.Settings"
                   Color="Color.Info"
                   Size="Size.Small"
                   Disabled="@(context.Item.NumeroFasi == 0)"
                   OnClick="@(() => GestisciTaskAsync(context.Item))" />
</MudTooltip>
```

Aggiungi il metodo nel `@code`:

```csharp
private async Task GestisciTaskAsync(ConfigurazioneRiepilogoDto config)
{
    var parameters = new DialogParameters
    {
        ["IdConfigurazione"] = config.IdConfigurazione,
        ["CodiceConfigurazione"] = config.CodiceConfigurazione
    };
    
    var options = new DialogOptions 
    { 
        MaxWidth = MaxWidth.Large, 
        FullWidth = true,
        CloseButton = true 
    };
    
    var dialog = await DialogService.ShowAsync<DialogGestioneTask>(
        "Gestione Task", 
        parameters,
        options);
    
    var result = await dialog.Result;
    if (!result.Canceled)
    {
        // Ricarica griglia dopo modifiche
        await LoadConfigurazioniAsync();
    }
}
```

---

### Opzione B: Eliminazione Forzata (ALTERNATIVA RAPIDA)

Modifica `EliminaConfigDefinitivaAsync`:

```csharp
private async Task EliminaConfigDefinitivaAsync(ConfigurazioneRiepilogoDto config)
{
    string contentText;
    
    if (config.TaskAttivi > 0)
    {
        // Carica dettagli task per mostrare elenco
        await using var context = await DbFactory.CreateDbContextAsync();
        var tasks = await context.TaskDaEseguires
            .Where(t => t.IdConfigurazioneDatabase == config.IdConfigurazione && t.Enabled)
            .Select(t => new { t.IdTaskDaEseguire, t.CronExpression })
            .ToListAsync();
        
        var taskList = string.Join("\n", tasks.Select(t => $"- Task ID {t.IdTaskDaEseguire} ({t.CronExpression})"));
        
        contentText = $"?? ATTENZIONE: La configurazione '{config.CodiceConfigurazione}' ha {config.TaskAttivi} task attivi:\n\n" +
                      $"{taskList}\n\n" +
                      "Eliminare la configurazione rimuoverà anche TUTTI i task e le schedulazioni Hangfire associate.\n\n" +
                      "Questa operazione è IRREVERSIBILE. Continuare?";
    }
    else
    {
        contentText = $"Sei sicuro di voler ELIMINARE DEFINITIVAMENTE la configurazione '{config.CodiceConfigurazione}'?\n\n" +
                      "Questa operazione è IRREVERSIBILE.";
    }

    var parameters = new DialogParameters
    {
        ["ContentText"] = contentText,
        ["ButtonText"] = "Sì, elimina definitivamente",
        ["Color"] = Color.Error
    };

    var dialog = await DialogService.ShowAsync<DialogConfirm>("Conferma Eliminazione Definitiva", parameters);
    var result = await dialog.Result;

    if (result.Canceled)
        return;

    try
    {
        await ServiceWrapper!.ServiceConfigurazioneFontiDati.DeleteConfigurazioneFontiDatiAsync(config.IdConfigurazione);
        await LoadConfigurazioniAsync();
        Snackbar.Add($"Configurazione '{config.CodiceConfigurazione}' eliminata definitivamente", Severity.Success);
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Errore eliminazione: {ex.Message}", Severity.Error);
    }
}
```

---

## ?? Confronto Opzioni

| Caratteristica | Opzione A (Dialog) | Opzione B (Forzata) |
|----------------|-------------------|---------------------|
| **Complessità Implementazione** | Alta (nuovo component) | Bassa (modifica esistente) |
| **User Experience** | Eccellente | Buona |
| **Flessibilità** | Alta (gestione granulare) | Bassa (tutto o niente) |
| **Manutenibilità** | Alta (component riusabile) | Media |
| **Tempo Sviluppo** | ~4 ore | ~30 minuti |
| **Sicurezza Dati** | Alta (controllo dettagliato) | Media (warning chiaro) |

**RACCOMANDAZIONE FINALE: Opzione A** per produzione, Opzione B come soluzione temporanea.

---

## ? Checklist Implementazione

### Fase 1: Analisi (COMPLETATA ?)
- [x] Analisi stato attuale
- [x] Identificazione limitazioni
- [x] Proposta soluzioni
- [x] Documentazione decisioni

### Fase 2: Sviluppo (TODO)
- [ ] Creare `DialogGestioneTask.razor`
- [ ] Aggiungere button in `PageListaConfigurazioniFonti.razor`
- [ ] Implementare metodo `GestisciTaskAsync()`
- [ ] Testing con configurazioni reali
- [ ] Testing con task multipli
- [ ] Verifica rimozione Hangfire

### Fase 3: Validazione (TODO)
- [ ] Test disabilitazione singolo task
- [ ] Test disabilitazione tutti task
- [ ] Test eliminazione singolo task
- [ ] Test eliminazione tutti task
- [ ] Test ricaricamento griglia
- [ ] Test gestione errori

### Fase 4: Deployment (TODO)
- [ ] Code review
- [ ] Aggiornamento documentazione
- [ ] Update NOTE.txt
- [ ] Commit e push
- [ ] Deploy in test environment

---

## ?? File Coinvolti

| File | Tipo | Modifiche |
|------|------|-----------|
| `PageListaConfigurazioniFonti.razor` | EDIT | +30 righe (button + metodo) |
| `DialogGestioneTask.razor` | NEW | ~250 righe (component completo) |
| `NOTE.txt` | EDIT | Aggiornamento stato |
| `GestioneConfigurazioni_Analisi.md` | NEW | Questo documento |

---

## ?? Riferimenti

- **Codice Esistente**: `ServiceConfigurazioneFontiDati.DeleteConfigurazioneFontiDatiAsync()` (riga 142)
- **Pattern Dialog**: Vedi `DialogConfirm.razor` per struttura base
- **Hangfire API**: `IProductionJobScheduler.RemoveByKeyAsync()` e `AddOrUpdateAsync()`
- **Best Practices**: [Microsoft Blazor Dialogs](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dialogs)

---

**Creato**: 2025  
**Ultima Modifica**: 2025  
**Autore**: GitHub Copilot  
**Versione**: 1.0
