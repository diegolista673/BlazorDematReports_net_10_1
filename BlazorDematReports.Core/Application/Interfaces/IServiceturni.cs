using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Application.Interfaces
{
    /// <summary>
    /// Interfaccia per la gestione dei turni e delle relative operazioni sui dati.
    /// </summary>
    public interface IServiceTurni : IServiceBase<Turni>
    {
        /// <summary>
        /// Restituisce la lista di tutti i turni.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Turni"/>.</returns>
        Task<List<Turni>> GetTurniAsync();

        /// <summary>
        /// Aggiunge un nuovo turno tramite DTO.
        /// </summary>
        /// <param name="turnoDto">DTO del turno da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddTurnoAsync(TurniDto turnoDto);

        /// <summary>
        /// Elimina un turno tramite il suo identificativo.
        /// </summary>
        /// <param name="idTurno">Identificativo del turno da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteTurnoAsync(int idTurno);
    }
}
