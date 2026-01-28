namespace Entities.Models.DbApplication;

public partial class LinkLavorazioni
{
    public int Idlink { get; set; }

    public string? LinkLavorazione { get; set; }

    public string? LoginLinkLavorazione { get; set; }

    public string? PwdLinkLavorazione { get; set; }

    public string? LinkDatiProduzione { get; set; }

    public string? LoginDatiProduzione { get; set; }

    public string? PwdDatiProduzione { get; set; }

    public string? Commessa { get; set; }

    public int IdProceduraLavorazione { get; set; }

    public string NomeLink { get; set; } = null!;

    public virtual ProcedureLavorazioni IdProceduraLavorazioneNavigation { get; set; } = null!;
}
