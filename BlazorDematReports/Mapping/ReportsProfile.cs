using AutoMapper;
using Entities.Models;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature dei vari report e dati correlati
    /// </summary>
    public class ReportsProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per i report e entità correlate
        /// </summary>
        public ReportsProfile()
        {
            // Report documents mapping
            CreateMap<ReportDocumenti, ConfigReportDocumenti>();

            CreateMap<ReportProduzioneCompleta, ReportProduzioneCompleta>()
                    .ForMember(dest => dest.TempoLavOreCent, opt => opt.Condition(src => (src.TempoLavOreCent > 0)))
                    .ForMember(dest => dest.Documenti, opt => opt.Condition(src => (src.Documenti > 0)))
                    .ForMember(dest => dest.Fogli, opt => opt.Condition(src => (src.Fogli > 0)))
                    .ForMember(dest => dest.Pagine, opt => opt.Condition(src => (src.Pagine > 0)))
                    .ForMember(dest => dest.PagineSenzaBianco, opt => opt.Condition(src => (src.PagineSenzaBianco > 0)))
                    .ForMember(dest => dest.Scarti, opt => opt.Condition(src => (src.Scarti > 0)));
        }
    }
}