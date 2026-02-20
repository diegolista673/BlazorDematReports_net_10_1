namespace Entities.Models.DbApplication;

public partial class Ruoli
{
    public int IdRuolo { get; set; }

    public string Ruolo { get; set; } = null!;

    public virtual ICollection<Operatori> Operatoris { get; set; } = new List<Operatori>();
}
