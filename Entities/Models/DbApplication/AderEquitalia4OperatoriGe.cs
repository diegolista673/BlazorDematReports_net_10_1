using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class AderEquitalia4OperatoriGe
{
    public int IdCount { get; set; }

    public string? Operatore { get; set; }

    public int? TotaleDocumenti { get; set; }

    public DateTime? DataScansione { get; set; }

    public string IdEvento { get; set; } = null!;

    public int? Scarti { get; set; }

    public string? TipoScansione { get; set; }
}
