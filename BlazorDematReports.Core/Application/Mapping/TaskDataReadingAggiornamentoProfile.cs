using AutoMapper;
using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di TaskDataReadingAggiornamento
    /// </summary>
    public class TaskDataReadingAggiornamentoProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per TaskDataReadingAggiornamento e TaskDataReadingAggiornamentoDto
        /// </summary>
        public TaskDataReadingAggiornamentoProfile()
        {
            // Entity -> DTO (lettura)
            CreateMap<TaskDataReadingAggiornamento, TaskDataReadingAggiornamentoDto>()
                .ForMember(dest => dest.Lavorazione, opt => opt.MapFrom(src => src.Lavorazione))
                .ForMember(dest => dest.IdLavorazione, opt => opt.MapFrom(src => src.IdLavorazione))
                .ForMember(dest => dest.FaseLavorazione, opt => opt.MapFrom(src => src.FaseLavorazione))
                .ForMember(dest => dest.IdFase, opt => opt.MapFrom(src => src.IdFase))
                .ForMember(dest => dest.DataInizioLavorazione, opt => opt.MapFrom(src => src.DataInizioLavorazione))
                .ForMember(dest => dest.DataFineLavorazione, opt => opt.MapFrom(src => src.DataFineLavorazione))
                .ForMember(dest => dest.DataAggiornamento, opt => opt.MapFrom(src => src.DataAggiornamento))
                .ForMember(dest => dest.Risultati, opt => opt.MapFrom(src => src.Risultati))
                .ForMember(dest => dest.EsitoLetturaDato, opt => opt.MapFrom(src => src.EsitoLetturaDato ?? false))
                .ForMember(dest => dest.DescrizioneEsito, opt => opt.MapFrom(src => src.DescrizioneEsito ?? string.Empty));

            // DTO -> Entity (scrittura)
            CreateMap<TaskDataReadingAggiornamentoDto, TaskDataReadingAggiornamento>()
                .ForMember(dest => dest.Lavorazione, opt => opt.MapFrom(src => src.Lavorazione))
                .ForMember(dest => dest.IdLavorazione, opt => opt.MapFrom(src => src.IdLavorazione))
                .ForMember(dest => dest.FaseLavorazione, opt => opt.MapFrom(src => src.FaseLavorazione))
                .ForMember(dest => dest.IdFase, opt => opt.MapFrom(src => src.IdFase))
                .ForMember(dest => dest.DataInizioLavorazione, opt => opt.MapFrom(src => src.DataInizioLavorazione))
                .ForMember(dest => dest.DataFineLavorazione, opt => opt.MapFrom(src => src.DataFineLavorazione))
                .ForMember(dest => dest.DataAggiornamento, opt => opt.MapFrom(src => src.DataAggiornamento))
                .ForMember(dest => dest.Risultati, opt => opt.MapFrom(src => src.Risultati))
                .ForMember(dest => dest.EsitoLetturaDato, opt => opt.MapFrom(src => src.EsitoLetturaDato))
                .ForMember(dest => dest.DescrizioneEsito, opt => opt.MapFrom(src => src.DescrizioneEsito))
                .ForMember(dest => dest.IdAggiornamento, opt => opt.Ignore()); // Ignora l'ID durante la mappatura
        }
    }
}