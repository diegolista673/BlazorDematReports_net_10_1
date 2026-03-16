using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;
using Riok.Mapperly.Abstractions;

namespace BlazorDematReports.Core.Application.Mapping;

/// <summary>
/// Mapper Mapperly per TaskDataReadingAggiornamento.
/// </summary>
[Mapper]
public partial class TaskDataReadingAggiornamentoMapper
{
    /// <summary>TaskDataReadingAggiornamento → TaskDataReadingAggiornamentoDto.</summary>
    public partial TaskDataReadingAggiornamentoDto EntityToDto(TaskDataReadingAggiornamento entity);

    /// <summary>TaskDataReadingAggiornamentoDto → TaskDataReadingAggiornamento (ignora ID).</summary>
    [MapperIgnoreTarget(nameof(TaskDataReadingAggiornamento.IdAggiornamento))]
    public partial TaskDataReadingAggiornamento DtoToEntity(TaskDataReadingAggiornamentoDto dto);

    /// <summary>Collection mapping.</summary>
    public partial List<TaskDataReadingAggiornamentoDto> EntitiesToDtos(List<TaskDataReadingAggiornamento> entities);
    public partial List<TaskDataReadingAggiornamento> DtosToEntities(List<TaskDataReadingAggiornamentoDto> dtos);
}
