namespace Entities.Models.DbApplication;

public partial class PraticheSuccessione
{
    public int IdProduzione { get; set; }

    public string Operatore { get; set; } = null!;

    public string DataLavorazione { get; set; } = null!;

    public int Documenti { get; set; }

    public int Fogli { get; set; }

    public int Pagine { get; set; }

    public int IdFaseLavorazione { get; set; }

    public int IdCentro { get; set; }
}
