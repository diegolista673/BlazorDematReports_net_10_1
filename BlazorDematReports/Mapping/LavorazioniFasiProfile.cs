using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature delle fasi di lavorazione e task correlati
    /// </summary>
    public class LavorazioniFasiProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per le fasi di lavorazione e task correlati
        /// </summary>
        public LavorazioniFasiProfile()
        {
            // FasiLavorazione mappings
            CreateMap<FasiLavorazioneDto, FasiLavorazione>();
            CreateMap<FasiLavorazione, FasiLavorazioneDto>();

            // LavorazioniFasiDataReading mappings
            CreateMap<LavorazioniFasiDataReadingDto, LavorazioniFasiDataReading>()
                .ForMember(dest => dest.TaskDaEseguires, opt => opt.MapFrom(src => src.TaskDaEseguireDto));

            CreateMap<LavorazioniFasiDataReading, LavorazioniFasiDataReadingDto>()
                .ForMember(dest => dest.Lavorazione, opt => opt.MapFrom(src => src.IdProceduraLavorazioneNavigation.NomeProcedura))
                .ForMember(dest => dest.FaseLavorazione, opt => opt.MapFrom(src => src.IdFaseLavorazioneNavigation.FaseLavorazione))
                .ForMember(dest => dest.TaskDaEseguireDto, opt => opt.MapFrom(src => src.TaskDaEseguires));

            // RIMOSSE le mappe duplicate di TaskDaEseguire <-> TaskDaEseguireDto.
            // La mappa canonica è definita in ConfigProcedureLavorazioniProfile.

            // QueryProcedureLavorazioni mappings
            CreateMap<QueryProcedureLavorazioniDto, QueryProcedureLavorazioni>();
            CreateMap<QueryProcedureLavorazioni, QueryProcedureLavorazioniDto>();

            // LavorazioniFasiTipoTotale mappings
            CreateMap<LavorazioniFasiTipoTotaleDto, LavorazioniFasiTipoTotale>();

            CreateMap<LavorazioniFasiTipoTotale, LavorazioniFasiTipoTotaleDto>()
                .ForMember(dest => dest.NomeProcedura, opt => opt.MapFrom(src => src.IdProceduraLavorazioneNavigation.NomeProcedura))
                .ForMember(dest => dest.Fase, opt => opt.MapFrom(src => src.IdFaseNavigation.FaseLavorazione))
                .ForMember(dest => dest.TipologiaTotale, opt => opt.MapFrom(src => src.IdTipologiaTotaleNavigation.TipoTotale));
        }
    }
}