using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class QueryProcedureLavorazioni
{
    public int IdQuery { get; set; }

    public int IdproceduraLavorazione { get; set; }

    public string Titolo { get; set; } = null!;

    public string Descrizione { get; set; } = null!;

    public string? Connessione { get; set; }

    public DateTime DataCreazioneQuery { get; set; }

    public string? Note { get; set; }

    public bool? FlagAppartieneAlCentroSelezionato { get; set; }

    public virtual ProcedureLavorazioni IdproceduraLavorazioneNavigation { get; set; } = null!;
}
