using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione delle procedure clienti.
    /// </summary>
    public interface IServiceProcedureClienti : IServiceBase<ProcedureCliente>
    {
        /// <summary>
        /// Restituisce la lista di tutte le procedure clienti.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="ProcedureCliente"/>.</returns>
        Task<List<ProcedureCliente>> GetProcedureClienteAsync();

        /// <summary>
        /// Restituisce la lista delle procedure clienti associate all'utente corrente.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="ProcedureCliente"/>.</returns>
        Task<List<ProcedureCliente>> GetProcedureClienteByUserAsync();

        /// <summary>
        /// Restituisce la lista delle procedure clienti filtrata per centro.
        /// </summary>
        /// <param name="idCentro">Identificativo del centro.</param>
        /// <returns>Lista di oggetti <see cref="ProcedureCliente"/>.</returns>
        Task<List<ProcedureCliente>> GetProcedureClienteByCentroAsync(int idCentro);

        /// <summary>
        /// Elimina una procedura cliente tramite il suo identificativo.
        /// </summary>
        /// <param name="idProceduraCliente">Identificativo della procedura cliente da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteProceduraClienteAsync(int idProceduraCliente);

        /// <summary>
        /// Aggiunge una nuova procedura cliente tramite DTO.
        /// </summary>
        /// <param name="procedureClienteDto">DTO della procedura cliente da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddProceduraClienteAsync(ProcedureClienteDto procedureClienteDto);

        /// <summary>
        /// Aggiorna una procedura cliente tramite DTO.
        /// </summary>
        /// <param name="procedureClienteDto">DTO della procedura cliente da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateProcedureClienteAsync(ProcedureClienteDto procedureClienteDto);

        /// <summary>
        /// Restituisce la lista di tutte le procedure clienti e le mappa su oggetti DTO tramite centro di appartenenza dell'operatore.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="ProcedureClienteDto"/>.</returns>
        Task<List<ProcedureClienteDto>> GetProcedureClienteDtoByUserAsync();
    }
}
