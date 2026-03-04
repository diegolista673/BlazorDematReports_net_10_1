using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class Clienti
{
    public int IdCliente { get; set; }

    public string NomeCliente { get; set; } = null!;

    public int IdOperatore { get; set; }

    public DateTime DataCreazioneCliente { get; set; }

    public int IdCentroLavorazione { get; set; }

    public virtual CentriLavorazione IdCentroLavorazioneNavigation { get; set; } = null!;

    public virtual ICollection<ProcedureCliente> ProcedureClientes { get; set; } = new List<ProcedureCliente>();
}
