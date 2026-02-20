namespace Entities.Models.DbApplication;

public partial class OperatoriNormalizzati
{
    public int IdNorm { get; set; }

    public string OperatoreDaNormalizzare { get; set; } = null!;

    public string OperatoreNormalizzato { get; set; } = null!;
}
