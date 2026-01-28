namespace Entities.Models.DbApplication;

public partial class Equitalia3Operatori
{
    public int IdCount { get; set; }

    public string? Operatore { get; set; }

    public int? TotaleDoc { get; set; }

    public DateTime? DataScansione { get; set; }

    public string IdEvento { get; set; } = null!;
}
