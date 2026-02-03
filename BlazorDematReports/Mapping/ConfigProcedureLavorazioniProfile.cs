using AutoMapper;
using BlazorDematReports.Dto;
using DataReading.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Configura le mappature per ProcedureLavorazioni e entità correlate
    /// </summary>
    public class ConfigProcedureLavorazioniProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per ProcedureLavorazioni e entità correlate
        /// </summary>
        public ConfigProcedureLavorazioniProfile()
        {
            // Mappature per la lettura (Entity -> DTO)
            CreateMap<TaskDaEseguire, TaskDaEseguireDto>()
                .ForMember(dest => dest.IdProceduraLavorazione, opt => opt.MapFrom(src => src.IdLavorazioneFaseDateReadingNavigation.IdProceduraLavorazione))
                .ForMember(dest => dest.IdFaseLavorazione, opt => opt.MapFrom(src => src.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazione))
                .ForMember(dest => dest.FaseLavorazione, opt => opt.MapFrom(src => src.IdLavorazioneFaseDateReadingNavigation.IdFaseLavorazioneNavigation.FaseLavorazione))
                .ForMember(dest => dest.CronExpression, opt => opt.MapFrom(src => src.CronExpression))
                .ForMember(dest => dest.Enabled, opt => opt.MapFrom(src => src.Enabled))
                .ForMember(dest => dest.LastRunUtc, opt => opt.MapFrom(src => src.LastRunUtc))
                .ForMember(dest => dest.LastError, opt => opt.MapFrom(src => src.LastError))
                .ForMember(dest => dest.ConsecutiveFailures, opt => opt.MapFrom(src => src.ConsecutiveFailures))
                .ForMember(dest => dest.IdConfigurazioneDatabase, opt => opt.MapFrom(src => src.IdConfigurazioneDatabase));


            CreateMap<LavorazioniFasiDataReading, LavorazioniFasiDataReadingDto>()
                .ForMember(dest => dest.Lavorazione, opt => opt.MapFrom(src => src.IdProceduraLavorazioneNavigation.NomeProcedura))
                .ForMember(dest => dest.FaseLavorazione, opt => opt.MapFrom(src => src.IdFaseLavorazioneNavigation.FaseLavorazione))
                .ForMember(dest => dest.TaskDaEseguireDto, opt => opt.MapFrom(src => src.TaskDaEseguires));

            CreateProjection<ProcedureLavorazioni, ProcedureLavorazioniDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.IdproceduraClienteNavigation!.IdclienteNavigation!.IdCentroLavorazioneNavigation.Centro))
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src => src.IdproceduraClienteNavigation!.IdclienteNavigation!.IdCliente))
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.IdproceduraClienteNavigation!.IdclienteNavigation!.NomeCliente))
                .ForMember(dest => dest.ProceduraCliente, opt => opt.MapFrom(src => src.IdproceduraClienteNavigation!.ProceduraCliente))
                .ForMember(dest => dest.FormatoDatiProduzione, opt => opt.MapFrom(src => src.IdformatoDatiProduzioneNavigation!.FormatoDatiProduzione))
                .ForMember(dest => dest.Reparto, opt => opt.MapFrom(src => src.IdrepartiNavigation!.Reparti))
                .ForMember(dest => dest.QueryProcedureLavorazioniDto, opt => opt.MapFrom(src => src.QueryProcedureLavorazionis))
                .ForMember(dest => dest.LavorazioniFasiDataReadingsDto, opt => opt.MapFrom(src => src.LavorazioniFasiDataReadings));

            // Mappature per la scrittura (DTO -> Entity)
            CreateMap<TaskDaEseguireDto, TaskDaEseguire>()
                .ForMember(dest => dest.IdTaskDaEseguire, opt => opt.Ignore())
                .ForMember(dest => dest.IdLavorazioneFaseDateReading, opt => opt.Ignore())
                .ForMember(dest => dest.IdLavorazioneFaseDateReadingNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.Enabled, opt => opt.MapFrom(src => src.Enabled))
                .ForMember(dest => dest.CronExpression, opt => opt.MapFrom(src => src.CronExpression))
                .ForMember(dest => dest.LastRunUtc, opt => opt.MapFrom(src => src.LastRunUtc))
                .ForMember(dest => dest.LastError, opt => opt.MapFrom(src => src.LastError))
                .ForMember(dest => dest.ConsecutiveFailures, opt => opt.MapFrom(src => src.ConsecutiveFailures))
                .ForMember(dest => dest.IdConfigurazioneDatabase, opt => opt.MapFrom(src => src.IdConfigurazioneDatabase));

            CreateMap<LavorazioniFasiDataReadingDto, LavorazioniFasiDataReading>()
                .ForMember(dest => dest.TaskDaEseguires, opt => opt.MapFrom(src => src.TaskDaEseguireDto))
                .ForMember(dest => dest.IdProceduraLavorazioneNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdFaseLavorazioneNavigation, opt => opt.Ignore());

            CreateMap<ProcedureLavorazioniDto, ProcedureLavorazioni>()
                .ForMember(dest => dest.Attiva, opt => opt.Ignore())
                .ForMember(dest => dest.IdformatoDatiProduzioneNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdoperatoreNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdproceduraClienteNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.IdrepartiNavigation, opt => opt.Ignore())
                .ForMember(dest => dest.LavorazioniFasiTipoTotales, opt => opt.Ignore())
                .ForMember(dest => dest.ProduzioneOperatoris, opt => opt.Ignore())
                .ForMember(dest => dest.ProduzioneSistemas, opt => opt.Ignore())
                .ForMember(dest => dest.QueryProcedureLavorazionis, opt => opt.Ignore())
                .ForMember(dest => dest.LavorazioniFasiDataReadings, opt => opt.MapFrom(src => src.LavorazioniFasiDataReadingsDto));
        }
    }

    /// <summary>
    /// Resolver personalizzato per la mappatura di TimeTask da TimeSpan a TimeOnly
    /// </summary>
    public class TimeTaskResolver : IValueResolver<TaskDaEseguireDto, TaskDaEseguire, TimeOnly?>
    {
        /// <summary>
        /// Risolve la conversione da TimeSpan a TimeOnly per la proprietà TimeTask.
        /// </summary>
        /// <param name="source">Oggetto sorgente TaskDaEseguireDto.</param>
        /// <param name="destination">Oggetto destinazione TaskDaEseguire.</param>
        /// <param name="destMember">Valore corrente del membro di destinazione.</param>
        /// <param name="context">Contesto di risoluzione AutoMapper.</param>
        /// <returns>Valore TimeOnly convertito o null se il valore sorgente è null.</returns>
        public TimeOnly? Resolve(TaskDaEseguireDto source, TaskDaEseguire destination, TimeOnly? destMember, ResolutionContext context)
        {
            return source.TimeTask.HasValue ? TimeOnly.FromTimeSpan((TimeSpan)source.TimeTask!) : null;
        }
    }
}
