namespace DataReading.Models
{
    /// <summary>
    /// Rappresenta la struttura di un job Hangfire serializzato in JSON.
    /// </summary>
    public class JsonJobHangfire
    {
        /// <summary>
        /// Tipo della classe che contiene il metodo da eseguire.
        /// </summary>
        public string? t { get; set; }

        /// <summary>
        /// Nome del metodo da eseguire.
        /// </summary>
        public string? m { get; set; }

        /// <summary>
        /// Tipi dei parametri del metodo.
        /// </summary>
        public List<string>? p { get; set; }

        /// <summary>
        /// Argomenti da passare al metodo.
        /// </summary>
        public List<string>? a { get; set; }
    }
}
