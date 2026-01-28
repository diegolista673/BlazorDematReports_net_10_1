using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di Turni e TipoTurni
    /// </summary>
    public class TurniProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per Turni e TipoTurni
        /// </summary>
        public TurniProfile()
        {
            CreateMap<TurniDto, Turni>();
            CreateMap<RuoliDto, Ruoli>();
            CreateMap<Ruoli, RuoliDto>();
        }
    }
}