namespace Entities.Models.DbApplication;

public partial class ElaborazioneEmailGiornaliera
{
    public int IdElaborazione { get; set; }

    public string CodiceServizio { get; set; } = null!;

    public DateOnly DataElaborazione { get; set; }

    public bool Elaborata { get; set; }

    public DateTime? ElaborataIl { get; set; }

    public string? ElaborataDaTask { get; set; }
}
