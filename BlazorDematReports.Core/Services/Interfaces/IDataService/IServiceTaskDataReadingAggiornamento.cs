
using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    public interface IServiceTaskDataReadingAggiornamento : IServiceBase<TaskDataReadingAggiornamento>
    {


        /// <summary>
        /// Get tutte le lavorazioni + fase di lavorazione che compaiono nella tabella di produzione sistema filtrate per centro di lavorazione
        /// </summary>
        /// <param name="IdCentro"></param>
        /// <returns></returns>
        Task<List<TaskDataReadingAggiornamento>> GetAggiornamentoLavorazioneByDateAsync(DateTime startDate, DateTime endDate);


        Task<List<TaskDataReadingAggiornamentoDto>> GetAggiornamentoLavorazioneDtoByDateAsync(int IdProceduraLavorazione, DateTime startDate);

        Task<List<TaskDataReadingAggiornamentoDto>> GetAggiornamentoLavorazioneDtoByDateAsync(int IdProceduraLavorazione, DateTime startDate, DateTime endDate);

        Task<TaskDataReadingAggiornamento?> GetAggiornamentoLavorazioneAsync(int IdProceduraLavorazione, int IdFase);

        Task<List<TaskDataReadingAggiornamentoDto>> GetAggiornamentoDtoByDateAsync(DateTime startDate, DateTime endDate);


        /// <summary>
        /// Get ultima data aggiornamento
        /// </summary>
        /// <returns></returns>
        Task<string?> GetUltimoAggiornamentoAsync(int IdProceduraLavorazione, int idFaseLavorazione);

    }

}
