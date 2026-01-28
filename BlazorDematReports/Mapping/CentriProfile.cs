using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di CentriLavorazione e entità correlate
    /// </summary>
    public class CentriProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per CentriLavorazione e entità correlate
        /// </summary>
        public CentriProfile()
        {
            // Mappatura bidirezionale tra CentriLavorazioneDto e CentriLavorazione
            CreateMap<CentriLavorazioneDto, CentriLavorazione>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.Centro))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CentriLavorazione, CentriLavorazioneDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.Centro))
                .ForMember(dest => dest.Idcentro, opt => opt.MapFrom(src => src.Idcentro));

            // Mappatura tra CentriVisibiliDto e CentriLavorazione (in entrambe le direzioni)
            CreateMap<CentriVisibiliDto, CentriLavorazione>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.Centro))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CentriLavorazione, CentriVisibiliDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.Centro))
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.Idcentro));

            // Mappatura tra CentriVisibiliDto e CentriVisibili (in entrambe le direzioni)
            CreateMap<CentriVisibiliDto, CentriVisibili>()
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.IdCentro))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.FlagVisibile, opt => opt.MapFrom(src => src.FlagVisibile))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CentriVisibili, CentriVisibiliDto>()
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.IdCentro))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.FlagVisibile, opt => opt.MapFrom(src => src.FlagVisibile))
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdCentroNavigation != null ? src.IdCentroNavigation.Centro : null));
        }
    }
}