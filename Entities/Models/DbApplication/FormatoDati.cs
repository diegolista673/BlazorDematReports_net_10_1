namespace Entities.Models.DbApplication;

public partial class FormatoDati
{
    public int IdformatoDati { get; set; }

    public string FormatoDatiProduzione { get; set; } = null!;

    public virtual ICollection<ProcedureLavorazioni> ProcedureLavorazionis { get; set; } = new List<ProcedureLavorazioni>();
}
