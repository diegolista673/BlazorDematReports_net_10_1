using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class AderEquitalia4ProduzioneVr
{
    public int Idequitalia4 { get; set; }

    public int? PreAccettazione { get; set; }

    public int? Ripartizione { get; set; }

    public int? ScansioneCaptiva { get; set; }

    public int? Restituzione { get; set; }

    public DateTime? DataLavorazione { get; set; }

    public string IdEvento { get; set; } = null!;

    public int? ScansioneSorter { get; set; }

    public int? ScartiScansioneSorter { get; set; }

    public int? ScansioneSorterBuste { get; set; }

    public int? ScartiScansioneSorterBuste { get; set; }
}
