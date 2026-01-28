using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di Operatori e entità correlate
    /// </summary>
    public class OperatoriProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per Operatori e entità correlate
        /// </summary>
        public OperatoriProfile()
        {
            // Mappatura da DTO a entità con gestione delle proprietà null
            CreateMap<OperatoriDto, Operatori>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappatura da entità a DTO con gestione sicura delle proprietà di navigazione
            CreateMap<Operatori, OperatoriDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdcentroNavigation != null ? src.IdcentroNavigation.Centro : null))
                .ForMember(dest => dest.CentriVisibiliDto, opt => opt.MapFrom(src => src.CentriVisibilis))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappature per CentriVisibili con controlli di null-safety
            CreateMap<CentriVisibili, CentriVisibiliDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdCentroNavigation != null ? src.IdCentroNavigation.Centro : null))
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.IdCentro))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.FlagVisibile, opt => opt.MapFrom(src => src.FlagVisibile))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<CentriVisibiliDto, CentriVisibili>()
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.IdCentro))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.FlagVisibile, opt => opt.MapFrom(src => src.FlagVisibile))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappature per OperatoriNormalizzati con gestione null
            CreateMap<OperatoriNormalizzati, OperatoriNormalizzatiDto>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<OperatoriNormalizzatiDto, OperatoriNormalizzati>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}