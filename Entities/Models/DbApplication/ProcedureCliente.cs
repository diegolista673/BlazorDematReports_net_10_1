namespace Entities.Models.DbApplication;

public partial class ProcedureCliente
{
    public int IdproceduraCliente { get; set; }

    public int Idcliente { get; set; }

    public string ProceduraCliente { get; set; } = null!;

    public int Idcentro { get; set; }

    public string? Commessa { get; set; }

    public DateTime DataInserimento { get; set; }

    public int Idoperatore { get; set; }

    public string? DescrizioneProcedura { get; set; }

    public virtual Clienti IdclienteNavigation { get; set; } = null!;

    public virtual Operatori IdoperatoreNavigation { get; set; } = null!;

    public virtual ICollection<ProcedureLavorazioni> ProcedureLavorazionis { get; set; } = new List<ProcedureLavorazioni>();
}
