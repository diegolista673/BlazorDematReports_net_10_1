using System;
using System.Collections.Generic;
using Entities.Enums;

namespace Entities.Models.DbApplication;

// ╔══════════════════════════════════════════════════════════════════════════════════════╗
// ║ ⚠️  ATTENZIONE - FILE GENERATO DA SCAFFOLDING CON CUSTOMIZZAZIONI MANUALI          ║
// ║                                                                                      ║
// ║ Questa classe è stata modificata manualmente:                                       ║
// ║ • La proprietà TipoFonte è di tipo TipoFonteData (enum), NON string                ║
// ║ • Il converter TipoFonteDataConverter gestisce la conversione per il database      ║
// ║                                                                                      ║
// ║ SE VIENE RIGENERATA CON SCAFFOLDING:                                                ║
// ║ 1. Backup di questo file prima dello scaffolding                                    ║
// ║ 2. Dopo lo scaffolding, ripristinare la riga:                                       ║
// ║    public TipoFonteData TipoFonte { get; set; }                                    ║
// ║    (invece di: public string TipoFonte { get; set; } = null!;)                     ║
// ║ 3. Verificare che l'using Entities.Enums; sia presente                             ║
// ║                                                                                      ║
// ║ Oppure eseguire lo script: Entities/Models/DbApplication/fix-scaffolding.ps1       ║
// ╚══════════════════════════════════════════════════════════════════════════════════════╝

public partial class ConfigurazioneFontiDati
{
    public int IdConfigurazione { get; set; }

    public string CodiceConfigurazione { get; set; } = null!;

    public string? DescrizioneConfigurazione { get; set; }

    // IMPORTANTE: Non rigenerare questa proprietà con scaffold - TipoFonte è un enum (TipoFonteData), non string
    // Il converter TipoFonteDataConverter gestisce la conversione enum ↔ string per il database
    public TipoFonteData TipoFonte { get; set; }

    public string? ConnectionStringName { get; set; }

    public string? HandlerClassName { get; set; }

    public string? CreatoDa { get; set; }

    public DateTime? CreatoIl { get; set; }

    public string? ModificatoDa { get; set; }

    public DateTime? ModificatoIl { get; set; }

    public virtual ICollection<ConfigurazioneFaseCentro> ConfigurazioneFaseCentros { get; set; } = new List<ConfigurazioneFaseCentro>();

    public virtual ICollection<TaskDaEseguire> TaskDaEseguires { get; set; } = new List<TaskDaEseguire>();
}
