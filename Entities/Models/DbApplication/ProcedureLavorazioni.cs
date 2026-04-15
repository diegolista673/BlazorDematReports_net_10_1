using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class ProcedureLavorazioni
{
    public int IdproceduraLavorazione { get; set; }

    public int IdproceduraCliente { get; set; }

    public DateTime DataInserimento { get; set; }

    public int? Idoperatore { get; set; }

    public string NomeProcedura { get; set; } = null!;

    public string? Note { get; set; }

    public int IdformatoDatiProduzione { get; set; }

    public int Idreparti { get; set; }

    public int? FlagAbilitaLavorazioneAltroReparto { get; set; }

    public int Idcentro { get; set; }

    public string? NomeProceduraProgramma { get; set; }

    public string? LogoBase64 { get; set; }

    public bool? Attiva { get; set; }



    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual FormatoDati IdformatoDatiProduzioneNavigation { get; set; } = null!;

    public virtual Operatori? IdoperatoreNavigation { get; set; }

    public virtual ProcedureCliente IdproceduraClienteNavigation { get; set; } = null!;

    public virtual RepartiProduzione IdrepartiNavigation { get; set; } = null!;

    public virtual ICollection<LavorazioniFasiDataReading> LavorazioniFasiDataReadings { get; set; } = new List<LavorazioniFasiDataReading>();

    public virtual ICollection<LavorazioniFasiTipoTotale> LavorazioniFasiTipoTotales { get; set; } = new List<LavorazioniFasiTipoTotale>();

    public virtual ICollection<ProduzioneOperatori> ProduzioneOperatoris { get; set; } = new List<ProduzioneOperatori>();

    public virtual ICollection<ProduzioneSistema> ProduzioneSistemas { get; set; } = new List<ProduzioneSistema>();


}
