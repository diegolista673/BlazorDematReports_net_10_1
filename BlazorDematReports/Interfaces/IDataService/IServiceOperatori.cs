using BlazorDematReports.Dto;
using Entities.Models.DbApplication;

namespace BlazorDematReports.Interfaces.IDataService
{
    /// <summary>
    /// Interfaccia per la gestione degli operatori.
    /// Fornisce metodi asincroni per CRUD e query specifiche sugli operatori e i relativi DTO.
    /// </summary>
    public interface IServiceOperatori : IServiceBase<Operatori>
    {
        /// <summary>
        /// Recupera la lista di tutti gli operatori.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Operatori"/>.</returns>
        Task<List<Operatori>> GetOperatoriAsync();

        /// <summary>
        /// Recupera un operatore tramite il suo ID.
        /// </summary>
        /// <param name="IdOperatore">ID dell'operatore.</param>
        /// <returns>Oggetto <see cref="Operatori"/> se trovato, altrimenti null.</returns>
        Task<Operatori?> GetOperatoriByIdAsync(int IdOperatore);

        /// <summary>
        /// Recupera tutti gli operatori associati a un centro specifico.
        /// </summary>
        /// <param name="iDcentro">ID del centro.</param>
        /// <returns>Lista di oggetti <see cref="Operatori"/>.</returns>
        Task<List<Operatori>> GetOperatoriByIdCentroAsync(int iDcentro);

        /// <summary>
        /// Elimina un operatore tramite ID e salva le modifiche nel database.
        /// NB: Assicurarsi che la regola di eliminazione a cascata sia impostata su SQL Server
        /// nella relazione CentriVisibili tramite Table Designer -> Insert and Update specification -> Delete rule -> Cascade.
        /// </summary>
        /// <param name="idOperatore">ID dell'operatore da eliminare.</param>
        Task DeleteOperatoreAsync(int idOperatore);

        /// <summary>
        /// Aggiunge un nuovo operatore utilizzando un DTO e salva le modifiche nel database.
        /// </summary>
        /// <param name="oper">DTO dell'operatore da aggiungere.</param>
        Task AddOperatoreAsync(OperatoriDto oper);

        /// <summary>
        /// Aggiunge un nuovo operatore e salva le modifiche nel database.
        /// </summary>
        /// <param name="oper">operatore da aggiungere.</param>
        Task AddOperatoreAsync(Operatori oper);

        /// <summary>
        /// Aggiorna un operatore esistente utilizzando un DTO e salva le modifiche nel database.
        /// </summary>
        /// <param name="oper">DTO dell'operatore da aggiornare.</param>
        Task UpdateOperatoreAsync(OperatoriDto oper);

        /// <summary>
        /// Recupera tutti gli operatori come DTO, includendo anche i centri visibili associati.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="OperatoriDto"/>.</returns>
        Task<List<OperatoriDto>> GetOperatoriDtoAsync();

        /// <summary>
        /// Recupera gli operatori in base al ruolo dell'utente loggato.
        /// Se l'utente è Admin, restituisce tutti gli operatori; altrimenti solo quelli del centro associato all'utente.
        /// </summary>
        /// <returns>Lista di oggetti <see cref="Operatori"/>.</returns>
        Task<List<Operatori>> GetOperatoriByUserAsync();

    }
}
