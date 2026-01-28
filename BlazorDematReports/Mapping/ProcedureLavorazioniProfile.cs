using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di ProcedureLavorazioni e entità correlate
    /// </summary>
    public class ProcedureLavorazioniProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per ProcedureLavorazioni e entità correlate
        /// </summary>
        public ProcedureLavorazioniProfile()
        {
            // Mappatura da DTO a entità con verifica dei valori null
            CreateMap<ProcedureLavorazioniDto, ProcedureLavorazioni>()
                .ForMember(dest => dest.LavorazioniFasiDataReadings, opt => opt.MapFrom(src => src.LavorazioniFasiDataReadingsDto))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappatura da entità a DTO con gestione sicura delle proprietà di navigazione annidate
            CreateMap<ProcedureLavorazioni, ProcedureLavorazioniDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdproceduraClienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation != null
                        ? src.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro
                        : null))
                .ForMember(dest => dest.FormatoDatiProduzione, opt => opt.MapFrom(src =>
                    src.IdformatoDatiProduzioneNavigation != null
                        ? src.IdformatoDatiProduzioneNavigation.FormatoDatiProduzione
                        : null))
                .ForMember(dest => dest.Reparto, opt => opt.MapFrom(src =>
                    src.IdrepartiNavigation != null
                        ? src.IdrepartiNavigation.Reparti
                        : null))
                .ForMember(dest => dest.LavorazioniFasiDataReadingsDto, opt => opt.MapFrom(src => src.LavorazioniFasiDataReadings))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Proiezione ottimizzata con gestione sicura delle proprietà di navigazione annidate
            CreateProjection<ProcedureLavorazioni, ProcedureLavorazioniDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdproceduraClienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation != null
                        ? src.IdproceduraClienteNavigation.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro
                        : null))
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src =>
                    src.IdproceduraClienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation != null
                        ? src.IdproceduraClienteNavigation.IdclienteNavigation.IdCliente
                        : 0))
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src =>
                    src.IdproceduraClienteNavigation != null &&
                    src.IdproceduraClienteNavigation.IdclienteNavigation != null
                        ? src.IdproceduraClienteNavigation.IdclienteNavigation.NomeCliente
                        : null))
                .ForMember(dest => dest.ProceduraCliente, opt => opt.MapFrom(src =>
                    src.IdproceduraClienteNavigation != null
                        ? src.IdproceduraClienteNavigation.ProceduraCliente
                        : null))
                .ForMember(dest => dest.FormatoDatiProduzione, opt => opt.MapFrom(src =>
                    src.IdformatoDatiProduzioneNavigation != null
                        ? src.IdformatoDatiProduzioneNavigation.FormatoDatiProduzione
                        : null))
                .ForMember(dest => dest.Reparto, opt => opt.MapFrom(src =>
                    src.IdrepartiNavigation != null
                        ? src.IdrepartiNavigation.Reparti
                        : null))
                .ForMember(dest => dest.QueryProcedureLavorazioniDto, opt => opt.MapFrom(src => src.QueryProcedureLavorazionis))
                .ForMember(dest => dest.LavorazioniFasiDataReadingsDto, opt => opt.MapFrom(src => src.LavorazioniFasiDataReadings));
        }
    }
}