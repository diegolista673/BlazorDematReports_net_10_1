using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per QueryProcedureLavorazioni
    /// </summary>
    public class QueryProcedureLavorazioniProfile : Profile
    {
        /// <summary>
        /// Profilo AutoMapper per QueryProcedureLavorazioni
        /// </summary>
        public QueryProcedureLavorazioniProfile()
        {
            // RIMOSSA la mappa duplicata TaskDaEseguire -> TaskDaEseguireDto.
            // Si utilizza la mappa canonica definita in ConfigProcedureLavorazioniProfile.

            CreateMap<QueryProcedureLavorazioni, QueryProcedureLavorazioniDto>()
                .ForMember(dest => dest.NomeProcedura, opt => opt.MapFrom(src => src.IdproceduraLavorazioneNavigation!.NomeProcedura))
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src => src.IdproceduraLavorazioneNavigation!.IdproceduraClienteNavigation!.IdclienteNavigation!.IdCentroLavorazioneNavigation.Centro));
        }
    }
}
