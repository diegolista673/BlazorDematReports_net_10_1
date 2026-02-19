using AutoMapper;
using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di ProduzioneOperatori
    /// </summary>
    public class ProduzioneOperatoriProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per ProduzioneOperatori e entità correlate
        /// </summary>
        public ProduzioneOperatoriProfile()
        {
            // Mapping per TipologieTotaliProduzione con gestione null-safety
            CreateMap<TipologieTotaliProduzione, TipologieTotaliProduzioneDto>()
                .ForMember(
                    dest => dest.TipologiaTotale,
                    opt => opt.MapFrom(src =>
                        src.IdtipologiaTotaleNavigation != null
                            ? src.IdtipologiaTotaleNavigation.TipoTotale
                            : null))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mapping inverso con gestione delle proprietà
            CreateMap<TipologieTotaliProduzioneDto, TipologieTotaliProduzione>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mapping completo da ProduzioneOperatori a ProduzioneOperatoriDto
            CreateMap<ProduzioneOperatori, ProduzioneOperatoriDto>()
                .ForMember(
                    dest => dest.Operatore,
                    opt => opt.MapFrom(src =>
                        src.IdOperatoreNavigation != null
                            ? src.IdOperatoreNavigation.Operatore
                            : null))
                .ForMember(
                    dest => dest.AltraUtenza,
                    opt => opt.MapFrom(src => src.AltraUtenza))
                .ForMember(
                    dest => dest.Lavorazione,
                    opt => opt.MapFrom(src =>
                        src.IdProceduraLavorazioneNavigation != null
                            ? src.IdProceduraLavorazioneNavigation.NomeProcedura
                            : null))
                .ForMember(
                    dest => dest.Fase,
                    opt => opt.MapFrom(src =>
                        src.IdFaseLavorazioneNavigation != null
                            ? src.IdFaseLavorazioneNavigation.FaseLavorazione
                            : null))
                .ForMember(
                    dest => dest.Turno,
                    opt => opt.MapFrom(src =>
                        src.IdTurnoNavigation != null
                            ? src.IdTurnoNavigation.Turno
                            : null))
                .ForMember(
                    dest => dest.TipologieTotaliProduzioneDto,
                    opt => opt.MapFrom(src => src.TipologieTotaliProduziones))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mapping da ProduzioneOperatoriDto a ProduzioneOperatori
            CreateMap<ProduzioneOperatoriDto, ProduzioneOperatori>()
                .ForMember(dest => dest.TipologieTotaliProduziones, opt => opt.Ignore()) // Gestione collezioni da implementare in un service dedicato
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Projection ottimizzata per query con gestione null-safety
            CreateProjection<ProduzioneOperatori, ProduzioneOperatoriDto>()
                .ForMember(
                    dest => dest.Operatore,
                    opt => opt.MapFrom(src =>
                        src.IdOperatoreNavigation != null
                            ? src.IdOperatoreNavigation.Operatore
                            : null))
                .ForMember(
                    dest => dest.AltraUtenza,
                    opt => opt.MapFrom(src => src.AltraUtenza))
                .ForMember(
                    dest => dest.Lavorazione,
                    opt => opt.MapFrom(src =>
                        src.IdProceduraLavorazioneNavigation != null
                            ? src.IdProceduraLavorazioneNavigation.NomeProcedura
                            : null))
                .ForMember(
                    dest => dest.Fase,
                    opt => opt.MapFrom(src =>
                        src.IdFaseLavorazioneNavigation != null
                            ? src.IdFaseLavorazioneNavigation.FaseLavorazione
                            : null))
                .ForMember(
                    dest => dest.Turno,
                    opt => opt.MapFrom(src =>
                        src.IdTurnoNavigation != null
                            ? src.IdTurnoNavigation.Turno
                            : null))
                .ForMember(
                    dest => dest.TipologieTotaliProduzioneDto,
                    opt => opt.MapFrom(src => src.TipologieTotaliProduziones));
        }
    }
}