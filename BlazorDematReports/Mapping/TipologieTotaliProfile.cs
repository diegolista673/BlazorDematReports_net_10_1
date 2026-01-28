using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature delle TipologieTotali
    /// </summary>
    public class TipologieTotaliProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per TipologieTotali
        /// </summary>
        public TipologieTotaliProfile()
        {
            // Mappatura bidirezionale tra TipologieTotaliDto e TipologieTotali con gestione null
            CreateMap<TipologieTotaliDto, TipologieTotali>()
                .ForMember(dest => dest.IdTipoTotale, opt => opt.MapFrom(src => src.IdTipoTotale))
                .ForMember(dest => dest.TipoTotale, opt => opt.MapFrom(src => src.TipoTotale))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<TipologieTotali, TipologieTotaliDto>()
                .ForMember(dest => dest.IdTipoTotale, opt => opt.MapFrom(src => src.IdTipoTotale))
                .ForMember(dest => dest.TipoTotale, opt => opt.MapFrom(src => src.TipoTotale))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappatura da TipologieTotaliProduzione a TipologieTotaliDto con gestione dei nomi di proprietà diversi
            CreateMap<TipologieTotaliProduzione, TipologieTotaliDto>()
                .ForMember(dest => dest.IdTipoTotale, opt => opt.MapFrom(src => src.IdtipologiaTotale))
                .ForMember(dest => dest.TipoTotale, opt => opt.MapFrom(src =>
                    src.IdtipologiaTotaleNavigation != null ? src.IdtipologiaTotaleNavigation.TipoTotale : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}