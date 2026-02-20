namespace Entities.Models.DbApplication;

public partial class LavorazioniFasiDataReading
{
    public int IdlavorazioneFaseDateReading { get; set; }

    public int IdFaseLavorazione { get; set; }

    public bool FlagDataReading { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public bool FlagGraficoDocumenti { get; set; }

    public virtual FasiLavorazione IdFaseLavorazioneNavigation { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;

    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
