namespace Entities.Models.DbApplication;

public partial class ConfigReportDocumenti
{
    public int Idconfig { get; set; }

    public string? NomeProcedura { get; set; }

    public string FaseLavorazione { get; set; } = null!;

    public int IdCentro { get; set; }
}
