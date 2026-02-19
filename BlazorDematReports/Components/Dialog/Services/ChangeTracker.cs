using BlazorDematReports.Core.DataReading.Dto;
using BlazorDematReports.Dto;

namespace BlazorDematReports.Components.Dialog.Services;

/// <summary>
/// Gestisce il rilevamento delle modifiche.
/// </summary>
public class ChangeTracker
{
    private List<TaskDaEseguireDto> _originalTasks = new();
    public bool HasUnsavedChanges { get; private set; }

    public void Initialize(List<TaskDaEseguireDto> currentTasks)
    {
        _originalTasks = currentTasks.Select(CloneTask).ToList();
        HasUnsavedChanges = false;
    }

    public void DetectChanges(List<TaskDaEseguireDto> currentTasks)
    {
        HasUnsavedChanges = currentTasks.Count != _originalTasks.Count ||
                           !currentTasks.All(current => _originalTasks.Any(original => TasksAreEquivalent(current, original)));
    }

    public void MarkAsSaved() => HasUnsavedChanges = false;

    private static TaskDaEseguireDto CloneTask(TaskDaEseguireDto task) => new()
    {
        IdTaskDaEseguire = task.IdTaskDaEseguire,
        IdTask = task.IdTask,
        TaskName = task.TaskName,
        Descrizione = task.Descrizione,
        TipoTask = task.TipoTask,
        TimeTask = task.TimeTask,
        GiorniPrecedenti = task.GiorniPrecedenti,
        Enabled = task.Enabled,
        IdFaseLavorazione = task.IdFaseLavorazione,
        IdProceduraLavorazione = task.IdProceduraLavorazione,
        FaseLavorazione = task.FaseLavorazione,
        Lavorazione = task.Lavorazione,
        IdCentro = task.IdCentro,
        TitoloQuery = task.TitoloQuery,
        // NUOVO SISTEMA: usa IdConfigurazioneDatabase invece di campi legacy
        IdConfigurazioneDatabase = task.IdConfigurazioneDatabase
    };

    private static bool TasksAreEquivalent(TaskDaEseguireDto task1, TaskDaEseguireDto task2) =>
        task1.IdTask == task2.IdTask &&
        task1.TaskName == task2.TaskName &&
        task1.TimeTask == task2.TimeTask &&
        task1.GiorniPrecedenti == task2.GiorniPrecedenti &&
        task1.IdConfigurazioneDatabase == task2.IdConfigurazioneDatabase;
}