using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per Turni, TipoTurni e Ruoli (entità semplici).
/// </summary>
[Mapper]
public partial class TurniMapper
{
    /// <summary>Turni ↔ TurniDto (nomi identici).</summary>
    public partial TurniDto TurnoToDto(Turni entity);

    [MapperIgnoreTarget(nameof(Turni.ProduzioneOperatoris))]
    public partial Turni DtoToTurno(TurniDto dto);

    /// <summary>TipoTurni ↔ TipoTurniDto (nomi identici).</summary>
    public partial TipoTurniDto TipoTurnoToDto(TipoTurni entity);
    public partial TipoTurni DtoToTipoTurno(TipoTurniDto dto);

    /// <summary>Ruoli ↔ RuoliDto (nomi identici).</summary>
    public partial RuoliDto RuoloToDto(Ruoli entity);

    [MapperIgnoreTarget(nameof(Ruoli.Operatoris))]
    public partial Ruoli DtoToRuolo(RuoliDto dto);
}
