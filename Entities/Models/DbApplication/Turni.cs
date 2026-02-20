namespace Entities.Models.DbApplication;

public partial class Turni
{
    public int IdTurno { get; set; }

    public string Turno { get; set; } = null!;

    public virtual ICollection<ProduzioneOperatori> ProduzioneOperatoris { get; set; } = new List<ProduzioneOperatori>();
}
