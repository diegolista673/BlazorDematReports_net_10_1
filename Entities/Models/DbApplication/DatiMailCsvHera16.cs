using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class DatiMailCsvHera16
{
    public int IdCounter { get; set; }

    public string? CodiceMercato { get; set; }

    public string? CodiceOfferta { get; set; }

    public string? TipoDocumento { get; set; }

    public DateTime? DataScansione { get; set; }

    public string? OperatoreScan { get; set; }

    public DateTime? DataClassificazione { get; set; }

    public string? OperatoreClassificazione { get; set; }

    public DateTime? DataIndex { get; set; }

    public string? OperatoreIndex { get; set; }

    public DateTime? DataPubblicazione { get; set; }

    public string? CodiceScatola { get; set; }

    public string? ProgrScansione { get; set; }

    public string? NomeFile { get; set; }

    public DateTime? DataCaricamentoFile { get; set; }

    public int? IdentificativoAllegato { get; set; }

    /// <summary>Data/ora in cui il record è stato letto da un handler di produzione.</summary>
    public DateTime? ElaboratoIl { get; set; }
}
