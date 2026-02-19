using BlazorDematReports.Core.Application.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Core.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione dei clienti e delle relative operazioni sui dati.
    /// </summary>
    public interface IServiceClienti : IServiceBase<Clienti>
    {
        /// <summary>
        /// Restituisce un cliente tramite il suo identificativo.
        /// </summary>
        /// <param name="IdCliente">Identificativo del cliente.</param>
        /// <returns>Oggetto <see cref="Clienti"/> o null se non trovato.</returns>
        Task<Clienti?> GetClienteByIdAsync(int IdCliente);

        /// <summary>
        /// Restituisce la lista dei clienti associati all'utente corrente.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Clienti"/>.</returns>
        Task<List<Clienti>> GetClientiByUserAsync();

        /// <summary>
        /// Aggiunge un nuovo cliente tramite DTO.
        /// </summary>
        /// <param name="clienteDto">DTO del cliente da aggiungere.</param>
        /// <returns>Task asincrono.</returns>
        Task AddClienteAsync(ClientiDto clienteDto);

        /// <summary>
        /// Elimina un cliente tramite il suo identificativo.
        /// </summary>
        /// <param name="cliente">Identificativo del cliente da eliminare.</param>
        /// <returns>Task asincrono.</returns>
        Task DeleteClienteAsync(int cliente);

        /// <summary>
        /// Aggiorna un cliente tramite DTO.
        /// </summary>
        /// <param name="arg">DTO del cliente da aggiornare.</param>
        /// <returns>Task asincrono.</returns>
        Task UpdateClienteAsync(ClientiDto arg);

        /// <summary>
        /// Restituisce la lista di clienti e li mappa su oggetti DTO, recuperando anche i centri visibili dall'operatore.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="ClientiDto"/>.</returns>
        Task<List<ClientiDto>> GetClientiDtoByUserAsync();

        /// <summary>
        /// Restituisce la lista dei clienti in base al ruolo e al centro.
        /// </summary>
        /// <param name="idCentro">Identificativo del centro.</param>
        /// <returns>Lista di oggetti <see cref="Clienti"/>.</returns>
        Task<List<Clienti>> GetClientiByIDCentroAsync(int idCentro);
    }
}
