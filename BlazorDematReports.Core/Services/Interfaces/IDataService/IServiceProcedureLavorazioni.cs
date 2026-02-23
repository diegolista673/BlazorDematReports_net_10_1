
using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Services.Interfaces.IDataService
{
    public interface IServiceProcedureLavorazioni : IServiceBase<ProcedureLavorazioni>
    {


        /// <summary>
        /// Get tutte procedure di lavorazione
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioni>> GetProcedureLavorazioniAsync();

        /// <summary>
        /// Get procedura di lavorazione tramite ID
        /// </summary>
        /// <param name="idPproceduraLavorazione"></param>
        /// <returns></returns>
        Task<ProcedureLavorazioni?> GetProceduraLavorazioneByIdAsync(int idPproceduraLavorazione);


        /// <summary>
        /// Get procedure di lavorazione tramite Centro di appartenenza
        /// </summary>
        /// <param name="iDcentro"></param>
        /// <returns></returns>
        Task<List<ProcedureLavorazioni>> GetProceduraLavorazioneByIdCentroAsync(int IdCentro);


        /// <summary>
        /// Get procedure di lavorazione tramite Centro di appartenenza dell operatore
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioni>> GetProcedureLavorazioniByUserAsync();


        /// <summary>
        /// Get all procedure di lavorazione and maps them to DTO object by user
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniDtoByUserAsync();



        /// <summary>
        /// Get tutte le procedure di lavorazione and maps them to DTO objects tramite Centro di appartenenza dell operatore
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>> GetProcedureLavorazioniDtoByCentroAsync(int idCentro);

        /// <summary>
        /// Get tutte le procedure di lavorazione and maps them to DTO objects 
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>> GetProcedureLavorazioniDtoAsync();

        /// <summary>
        /// Get procedura di lavorazione and maps them to DTO objects 
        /// </summary>
        /// <returns></returns>
        Task<ProcedureLavorazioniDto?> GetProceduraLavorazioneDtoAsync(int idProceduraLavorazione);


        /// <summary>
        ///Aggiorna procedura di lavorazione e elimina e/o aggiorna le fasi di lavorazione e flag datareading in tabella lavorazioniFasiDataReading 
        /// </summary>
        /// <param name="procedureLavorazioniDto"></param>
        /// <returns></returns>
        Task UpdateProceduraLavorazioneAndFasiDataReadingASync(ProcedureLavorazioniDto procedureLavorazioniDto);

        /// <summary>
        /// Delete procedura di lavorazione by ID operatore
        /// </summary>
        /// <param name="idProceduraLavorazione"></param>
        /// <returns></returns>
        Task DeleteProceduraLavorazioneAsync(int idProceduraLavorazione);

        /// <summary>
        /// Add procedura di lavorazione by procedureLavorazioniDto
        /// </summary>
        /// <param name="procedureLavorazioniDto"></param>
        /// <returns></returns>
        Task<int> AddProceduraLavorazioneAsync(ProcedureLavorazioniDto procedureLavorazioniDto);

        /// <summary>
        /// Get tutte le procedure di lavorazione and maps them to DTO objects versione semplificata senza tutte le relazioni tra tabelle 
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>?> GetAllProcedureLavorazioniDtoByUserAsync();

        /// <summary>
        /// Get singola procedure di lavorazione and maps them to DTO objects by idProceduraLavorazione
        /// </summary>
        /// <param name="idProceduraLavorazione"></param>
        /// <returns></returns>
        Task<ProcedureLavorazioniDto?> GetSingleProceduraLavorazioneDtoByIDAsync(int idProceduraLavorazione);

        /// <summary>
        /// Get procedure di lavorazione senza le relazioni ad altre tabelle
        /// </summary>
        Task<List<ProcedureLavorazioni>> GetTableProcedureLavorazioniByUserAsync();

        /// <summary>
        /// Get procedure di lavorazione and maps them to DTO objects by idCentro
        /// </summary>
        /// <param name="idCentro"></param>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniDtoByIDCentroAsync(int idCentro);

        /// <summary>
        /// Get procedure di lavorazione and maps them to DTO objects versione con relazione tabella LavorazionefasiDataReadings
        /// </summary>
        /// <returns></returns>
        Task<List<ProcedureLavorazioniDto>?> GetProcedureLavorazioniFasiDtoByUserAsync();

        /// <summary>
        /// Get singola procedura di lavorazione and maps them to DTO objects versione con relazione tabella LavorazionefasiDataReadings
        /// </summary>
        /// <param name="nomeProcedura"></param>
        /// <returns></returns>
        Task<ProcedureLavorazioniDto?> GetSingleProceduraLavorazioneDtoAsync(string nomeProcedura);

    }

}
