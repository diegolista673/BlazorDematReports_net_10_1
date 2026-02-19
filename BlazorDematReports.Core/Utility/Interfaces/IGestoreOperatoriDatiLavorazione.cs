using Entities.Models.DbApplication;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Utility.Interfaces
{
    public interface IGestoreOperatoriDatiLavorazione
    {
        Task SetOperatoriAsync();

        /// <summary>
        /// Carica gli operatori esterni con informazioni complete incluso il centro.
        /// </summary>
        /// <returns>Lista di operatori esterni completi di centro.</returns>
        Task<List<OperatoreMondo>> SetOperatoriEsterniMondoAsync();


        /// <summary>
        /// Cerca un operatore esterno in base al nome normalizzato.
        /// </summary>
        /// <param name="nomeNormalizzato">Nome normalizzato dell'operatore da cercare.</param>
        /// <returns>L'operatore esterno trovato o null se non esiste.
        /// </returns>
        OperatoreMondo? TrovaOperatoreMondo(string? operatore);


        /// <summary>
        /// Ottiene l'ID del centro DematReports corrispondente al nome del centro Mondo
        /// </summary>
        /// <param name="centro">Nome del centro nel database Mondo</param>
        /// <returns>ID del centro corrispondente in DematReports, o 0 se non trovato</returns>
        Task<int> GetIDCentroDematFromMondo(string centro);


        /// <summary>
        /// Aggiunge un nuovo operatore al database DematReports con l'ID centro corretto
        /// </summary>
        /// <param name="oper">Nome dell'operatore da aggiungere</param>
        /// <param name="idCentro">ID del centro di appartenenza predefinito</param>
        /// <param name="nomeCentroMondo">Nome del centro Mondo (opzionale)</param>
        /// <returns>ID del nuovo operatore inserito</returns>
        Task<int> AddOperatoreDematAsync(string oper, int idCentro);

        /// <summary>
        /// Get elenco operatori DematReports
        /// </summary>
        /// <returns></returns>
        IEnumerable<Operatori>? GetOperatoriDemat();


        /// <summary>
        /// Get elenco operatori Mondo
        /// </summary>
        /// <returns></returns>
        IEnumerable<OperatoreMondo>? GetOperatoriMondo();
    }

}