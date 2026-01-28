namespace Entities.Models.DbApplication;

public partial class Equitalia3Produzione
{
    public int IdEquitalia3 { get; set; }

    public int? PreAccettazione { get; set; }

    public int? Ripartizione { get; set; }

    public int? Scansione { get; set; }

    public int? Restituzione { get; set; }

    public DateTime? DataLavorazione { get; set; }

    public string IdEvento { get; set; } = null!;
}
