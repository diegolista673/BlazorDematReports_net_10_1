using BlazorDematReports.Core.DataReading.Models;
using System.Data;

namespace BlazorDematReports.Core.DataReading.Interfaces
{
    /// <summary>
    /// Interfaccia per l'esecuzione di query SQL su un database.
    /// Le validazioni sono state migrate a SqlValidationService.
    /// </summary>
    public interface IQueryService
    {

        /// <summary>
        /// Esegue una query di produzione e restituisce risultati tipizzati con schema standard.
        /// Colonne obbligatorie: Operatore, DataLavorazione, Documenti, Fogli, Pagine.
        /// <para>
        /// Parametri data iniettati automaticamente (non occorre dichiararli nella firma del metodo):
        /// <list type="bullet">
        ///   <item><c>@startDate</c> — <c>DateTime2</c>, inizio giornata (<c>00:00:00.0000000</c>).
        ///   <c>@endDate</c> — <c>DateTime2</c>, fine giornata (<c>23:59:59.9999999</c>).
        ///   Usare per colonne <c>date/datetime/datetime2</c>:
        ///   <c>col &gt;= @startDate AND col &lt;= @endDate</c></item>
        ///   <item><c>@startDateStr</c> / <c>@endDateStr</c> — <c>VarChar(10)</c> formato <c>yyyy-MM-dd</c>.
        ///   Usare per colonne sorgente di tipo <c>varchar</c>:
        ///   <c>col &gt;= @startDateStr AND col &lt;= @endDateStr</c></item>
        /// </list>
        /// </para>
        /// <para>
        /// Verifica appartenenza al centro tramite colonne opzionali nel SELECT (priorità a <c>IdCentro</c>):
        /// <list type="bullet">
        ///   <item><c>IdCentro</c> — confrontato con <paramref name="idCentroAtteso"/>.</item>
        ///   <item><c>NomeCentro</c> — confrontato con <paramref name="nomeCentroAtteso"/> (case-insensitive).
        ///   Usare quando la query filtra per nome sede (es. <c>department = 'GENOVA'</c>).</item>
        /// </list>
        /// Se nessuna colonna è presente, assume appartenenza (comportamento legacy).
        /// </para>
        /// </summary>
        /// <param name="connectionString">Stringa di connessione al database.</param>
        /// <param name="queryString">Query SQL da eseguire.</param>
        /// <param name="startDate">Data di inizio: normalizzata a <c>00:00:00.0000000</c>, passata come <c>@startDate</c> (DateTime2) e <c>@startDateStr</c> (VarChar).</param>
        /// <param name="endDate">Data di fine inclusiva: portata a <c>23:59:59.9999999</c>, passata come <c>@endDate</c> (DateTime2). <c>@endDateStr</c> resta in formato <c>yyyy-MM-dd</c> (VarChar).</param>
        /// <param name="idCentroAtteso">ID numerico del centro del task.</param>
        /// <param name="nomeCentroAtteso">Nome testuale del centro (es. 'GENOVA'), usato per verificare la colonna <c>NomeCentro</c>.</param>
        /// <param name="cancellationToken">Token per la cancellazione dell'operazione.</param>
        /// <returns>Lista di <see cref="ProductionQueryResult"/> con i dati di produzione.</returns>
        Task<List<ProductionQueryResult>> ExecuteProductionQueryAsync(string connectionString, string queryString, DateTime startDate, DateTime endDate, int idCentroAtteso, string? nomeCentroAtteso = null, CancellationToken cancellationToken = default);
    }
}
