using AutoMapper;
using BlazorDematReports.Core.DataReading.Dto;
using Entities.Models.DbApplication;
using BlazorDematReports.Core.Utility.Models;


namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di ProduzioneSistema e dati correlati
    /// </summary>
    public class ProduzioneSistemaProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per ProduzioneSistema e dati correlati
        /// </summary>
        public ProduzioneSistemaProfile()
        {
            CreateMap<ProduzioneSistemaDTO, ProduzioneSistema>()
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.Operatore, opt => opt.MapFrom(src => src.Operatore))
                .ForMember(dest => dest.OperatoreNonRiconosciuto, opt => opt.MapFrom(src => src.OperatoreNonRiconosciuto))
                .ForMember(dest => dest.IdProceduraLavorazione, opt => opt.MapFrom(src => src.IdProceduraLavorazione))
                .ForMember(dest => dest.IdFaseLavorazione, opt => opt.MapFrom(src => src.IdFaseLavorazione))
                .ForMember(dest => dest.DataLavorazione, opt => opt.MapFrom(src => src.DataLavorazione))
                .ForMember(dest => dest.DataAggiornamento, opt => opt.MapFrom(src => src.DataAggiornamento))
                .ForMember(dest => dest.Documenti, opt => opt.MapFrom(src => src.Documenti))
                .ForMember(dest => dest.Fogli, opt => opt.MapFrom(src => src.Fogli))
                .ForMember(dest => dest.Pagine, opt => opt.MapFrom(src => src.Pagine))
                .ForMember(dest => dest.Scarti, opt => opt.MapFrom(src => src.Scarti))
                .ForMember(dest => dest.FlagInserimentoAuto, opt => opt.MapFrom(src => src.FlagInserimentoAuto))
                .ForMember(dest => dest.FlagInserimentoManuale, opt => opt.MapFrom(src => src.FlagInserimentoManuale))
                .ForMember(dest => dest.PagineSenzaBianco, opt => opt.MapFrom(src => src.PagineSenzaBianco))
                .ForMember(dest => dest.IdCentro, opt => opt.MapFrom(src => src.IdCentro))
                .ForMember(dest => dest.IdProduzioneSistema, opt => opt.Ignore())
                .ForMember(dest => dest.IdCentroNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdFaseLavorazioneNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdOperatoreNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdProceduraLavorazioneNavigation, opt => opt.Ignore());

            CreateMap<ProduzioneSistema, BlazorDematReports.Dto.ProduzioneSistemaDto>()
                .ForMember(dest => dest.Operatore, opt => opt.MapFrom(src => src.IdOperatoreNavigation != null ? src.IdOperatoreNavigation.Operatore : null))
                .ForMember(dest => dest.Lavorazione, opt => opt.MapFrom(src => src.IdProceduraLavorazioneNavigation != null ? src.IdProceduraLavorazioneNavigation.NomeProcedura : null))
                .ForMember(dest => dest.Fase, opt => opt.MapFrom(src => src.IdFaseLavorazioneNavigation != null ? src.IdFaseLavorazioneNavigation.FaseLavorazione : null));

            CreateMap<ProduzioneSistemaDTO, DatiElaborati>();
            CreateMap<DatiElaborati, ProduzioneSistemaDTO>();
        }
    }
}