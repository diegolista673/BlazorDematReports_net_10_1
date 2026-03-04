using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

/// <summary>
/// Staging per-operatore da CSV email. Una riga per (Operatore, TipoRisultato, DataLavorazione).
/// ADER4:  Operatore = colonna 'postazione' del CSV.
/// HERA16: Operatore = OperatoreScansione / OperatoreIndex / OperatoreClassificazione.
/// Letta dagli handler produzione che producono DatiLavorazione con operatore reale.
/// </summary>
public partial class DatiMailCsv
{
    public int Id { get; set; }

    /// <summary>Codice servizio: 'ADER4' o 'HERA16'.</summary>
    public string CodiceServizio { get; set; } = null!;

    public DateOnly DataLavorazione { get; set; }

    /// <summary>Operatore dal CSV: valore 'postazione' (ADER4) o nome operatore (HERA16).</summary>
    public string Operatore { get; set; } = null!;

    /// <summary>
    /// Tipo risultato: 'ScansioneCaptiva' | 'ScansioneSorter' | 'ScansioneSorterBuste' |
    /// 'PreAccettazione' | 'Ripartizione' | 'Restituzione' (ADER4)
    /// 'Scansione' | 'Index' | 'Classificazione' (HERA16)
    /// </summary>
    public string TipoRisultato { get; set; } = null!;

    /// <summary>Totale documenti: SUM(Numero documenti) o COUNT(*) raggruppato per operatore.</summary>
    public int Documenti { get; set; }

    /// <summary>Identificativo evento estratto dall'email.</summary>
    public string? IdEvento { get; set; }

    /// <summary>Centro: 'VERONA' | 'GENOVA' | null.</summary>
    public string? Centro { get; set; }

    public DateTime DataIngestione { get; set; }

    /// <summary>False = pronto per essere letto dagli handler produzione.</summary>
    public bool Elaborata { get; set; }

    public DateTime? ElaborataIl { get; set; }

    public int? ElaborataDaTaskId { get; set; }
}
