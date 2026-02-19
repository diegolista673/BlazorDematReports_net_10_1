namespace BlazorDematReports.Core.DataReading
{
    /// <summary>
    /// Eccezione personalizzata per errori relativi alle date di ricerca nei processi di lettura dati.
    /// </summary>
    [Serializable]
    public class SearchDateException : Exception
    {
        /// <summary>
        /// Inizializza una nuova istanza di <see cref="SearchDateException"/>.
        /// </summary>
        public SearchDateException()
        {
        }

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="SearchDateException"/> con un messaggio di errore specificato.
        /// </summary>
        /// <param name="message">Messaggio di errore.</param>
        public SearchDateException(string message) : base(message)
        {
        }

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="SearchDateException"/> con un messaggio di errore e un'eccezione interna specificati.
        /// </summary>
        /// <param name="message">Messaggio di errore.</param>
        /// <param name="innerException">Eccezione interna.</param>
        public SearchDateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
