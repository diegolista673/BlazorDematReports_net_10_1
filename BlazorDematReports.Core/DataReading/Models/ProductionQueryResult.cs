namespace BlazorDematReports.Core.DataReading.Models
{
    /// <summary>
    /// Risultato tipizzato per le query di produzione con schema standard obbligatorio.
    /// Colonne obbligatorie: Operatore, DataLavorazione, Documenti, Fogli, Pagine.
    /// <para>
    /// Colonne opzionali per la verifica dell'appartenenza al centro (mutuamente esclusive, priorità a IdCentro):
    /// <list type="bullet">
    ///   <item><c>IdCentro</c> — ID numerico del centro dichiarato dalla query nel SELECT.</item>
    ///   <item><c>NomeCentro</c> — Nome testuale del centro (es. 'GENOVA'), utile quando la query
    ///   filtra per <c>department</c> o campo equivalente invece di un ID numerico.</item>
    /// </list>
    /// Se nessuna delle due colonne è presente, il sistema assume appartenenza (comportamento legacy).
    /// </para>
    /// <example>
    /// Query con IdCentro numerico:
    /// <code>SELECT 5 AS IdCentro, OP_INDEX AS Operatore, ...</code>
    /// Query con NomeCentro testuale (quando il DB usa nomi di sede):
    /// <code>SELECT 'GENOVA' AS NomeCentro, OP_SCAN AS Operatore, ... WHERE department = 'GENOVA'</code>
    /// </example>
    /// </summary>
    public record ProductionQueryResult
    {
        /// <summary>Nome o codice dell'operatore che ha effettuato la lavorazione.</summary>
        public required string Operatore { get; init; }

        /// <summary>Data in cui è stata effettuata la lavorazione.</summary>
        public required DateTime DataLavorazione { get; init; }

        /// <summary>Numero di documenti elaborati.</summary>
        public required int Documenti { get; init; }

        /// <summary>Numero di fogli elaborati.</summary>
        public required int Fogli { get; init; }

        /// <summary>Numero di pagine elaborate.</summary>
        public required int Pagine { get; init; }


        /// <summary>
        /// Indica se questo record appartiene al centro del task corrente.
        /// Valorizzato da <see cref="QueryService"/> in base alla presenza di <c>IdCentro</c> o <c>NomeCentro</c>.
        /// </summary>
        public bool AppartieneAlCentro { get; init; }
    }
}
