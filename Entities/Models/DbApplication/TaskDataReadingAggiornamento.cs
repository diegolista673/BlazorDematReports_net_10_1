namespace Entities.Models.DbApplication;

public partial class TaskDataReadingAggiornamento
{
    public int IdAggiornamento { get; set; }

    public string Lavorazione { get; set; } = null!;

    public int IdLavorazione { get; set; }

    public string FaseLavorazione { get; set; } = null!;

    public int IdFase { get; set; }

    public DateTime DataInizioLavorazione { get; set; }

    public DateTime DataAggiornamento { get; set; }

    public int Risultati { get; set; }

    public bool? EsitoLetturaDato { get; set; }

    public string? DescrizioneEsito { get; set; }

    public DateTime? DataFineLavorazione { get; set; }
}
