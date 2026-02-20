namespace Entities.Models.DbApplication;

public partial class TabellaTask
{
    public int Idtask { get; set; }

    public string TaskName { get; set; } = null!;

    public int TipoTask { get; set; }

    public string Descrizione { get; set; } = null!;

    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
