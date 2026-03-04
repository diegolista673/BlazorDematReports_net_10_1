using System;
using System.Collections.Generic;

namespace Entities.Models.DbApplication;

public partial class VwConfigurazioniFontiDatiCompletum
{
    public int IdConfigurazione { get; set; }

    public string CodiceConfigurazione { get; set; } = null!;

    public string NomeConfigurazione { get; set; } = null!;

    public string TipoFonte { get; set; } = null!;

    public string? TestoQuery { get; set; }

    public string? MailServiceCode { get; set; }

    public string? HandlerClassName { get; set; }

    public string? ConnectionStringName { get; set; }

    public bool? ConfigAttiva { get; set; }

    public DateTime? CreatoIl { get; set; }

    public string? CreatoDa { get; set; }

    public int? IdFaseCentro { get; set; }

    public int? IdProceduraLavorazione { get; set; }

    public string? NomeProcedura { get; set; }

    public int? IdFaseLavorazione { get; set; }

    public string? FaseLavorazione { get; set; }

    public int? IdCentro { get; set; }

    public string? Centro { get; set; }

    public string? ParametriExtra { get; set; }

    public string? MappingColonne { get; set; }

    public string? TestoQueryOverride { get; set; }

    public int? TaskAttivi { get; set; }

    public int? PipelineSteps { get; set; }
}
