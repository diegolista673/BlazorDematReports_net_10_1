using AutoMapper;
using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Mapping
{
    /// <summary>
    /// Profilo AutoMapper per le mappature di Clienti e entità correlate
    /// </summary>
    public class ClientiProfile : Profile
    {
        /// <summary>
        /// Configura le mappature per Clienti e entità correlate
        /// </summary>
        public ClientiProfile()
        {
            // Mappatura da ClientiDto a Clienti con gestione delle proprietà null
            CreateMap<ClientiDto, Clienti>()
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.NomeCliente))
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src => src.IdCliente))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.DataCreazioneCliente, opt => opt.MapFrom(src => src.DataCreazioneCliente))
                .ForMember(dest => dest.IdCentroLavorazione, opt => opt.MapFrom(src => src.IdCentroLavorazione))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappatura da Clienti a ClientiDto con gestione delle relazioni di navigazione
            CreateMap<Clienti, ClientiDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdCentroLavorazioneNavigation != null ? src.IdCentroLavorazioneNavigation.Centro : null))
                .ForMember(dest => dest.NomeCliente, opt => opt.MapFrom(src => src.NomeCliente))
                .ForMember(dest => dest.IdCliente, opt => opt.MapFrom(src => src.IdCliente))
                .ForMember(dest => dest.IdOperatore, opt => opt.MapFrom(src => src.IdOperatore))
                .ForMember(dest => dest.DataCreazioneCliente, opt => opt.MapFrom(src => src.DataCreazioneCliente))
                .ForMember(dest => dest.IdCentroLavorazione, opt => opt.MapFrom(src => src.IdCentroLavorazione));

            // Mappatura da ProcedureClienteDto a ProcedureCliente con gestione delle proprietà null
            CreateMap<ProcedureClienteDto, ProcedureCliente>()
                .ForMember(dest => dest.IdproceduraCliente, opt => opt.MapFrom(src => src.IdproceduraCliente))
                .ForMember(dest => dest.Idcliente, opt => opt.MapFrom(src => src.Idcliente))
                .ForMember(dest => dest.ProceduraCliente, opt => opt.MapFrom(src => src.ProceduraCliente))
                .ForMember(dest => dest.Idcentro, opt => opt.MapFrom(src => src.Idcentro))
                .ForMember(dest => dest.Commessa, opt => opt.MapFrom(src => src.Commessa))
                .ForMember(dest => dest.DataInserimento, opt => opt.MapFrom(src => src.DataInserimento))
                .ForMember(dest => dest.Idoperatore, opt => opt.MapFrom(src => src.Idoperatore))
                .ForMember(dest => dest.DescrizioneProcedura, opt => opt.MapFrom(src => src.DescrizioneProcedura))
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            // Mappatura da ProcedureCliente a ProcedureClienteDto con gestione delle relazioni di navigazione
            CreateMap<ProcedureCliente, ProcedureClienteDto>()
                .ForMember(dest => dest.Centro, opt => opt.MapFrom(src =>
                    src.IdclienteNavigation != null && src.IdclienteNavigation.IdCentroLavorazioneNavigation != null
                        ? src.IdclienteNavigation.IdCentroLavorazioneNavigation.Centro
                        : null))
                .ForMember(dest => dest.Operatore, opt => opt.MapFrom(src =>
                    src.IdoperatoreNavigation != null
                        ? src.IdoperatoreNavigation.Operatore
                        : null))
                .ForMember(dest => dest.Cliente, opt => opt.MapFrom(src =>
                    src.IdclienteNavigation != null
                        ? src.IdclienteNavigation.NomeCliente
                        : null))
                .ForMember(dest => dest.IdproceduraCliente, opt => opt.MapFrom(src => src.IdproceduraCliente))
                .ForMember(dest => dest.Idcliente, opt => opt.MapFrom(src => src.Idcliente))
                .ForMember(dest => dest.ProceduraCliente, opt => opt.MapFrom(src => src.ProceduraCliente))
                .ForMember(dest => dest.Idcentro, opt => opt.MapFrom(src => src.Idcentro))
                .ForMember(dest => dest.Commessa, opt => opt.MapFrom(src => src.Commessa))
                .ForMember(dest => dest.DataInserimento, opt => opt.MapFrom(src => src.DataInserimento))
                .ForMember(dest => dest.Idoperatore, opt => opt.MapFrom(src => src.Idoperatore))
                .ForMember(dest => dest.DescrizioneProcedura, opt => opt.MapFrom(src => src.DescrizioneProcedura));
        }
    }
}