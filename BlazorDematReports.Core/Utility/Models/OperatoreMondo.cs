namespace BlazorDematReports.Core.Utility.Models
{
    /// <summary>
    /// Rappresenta un operatore proveniente da un database esterno con informazioni aggiuntive.
    /// </summary>
    public class OperatoreMondo
    {
        /// <summary>
        /// Nome principale dell'operatore.
        /// </summary>
        public string ID_UTENTE { get; set; } = string.Empty;

        /// <summary>
        /// Nome alternativo dell'operatore.
        /// </summary>
        public string SUTENTE { get; set; } = string.Empty;

        /// <summary>
        /// ID del centro associato all'operatore.
        /// </summary>
        public int IdCentro { get; set; }

        /// <summary>
        /// Nome del centro associato all'operatore.
        /// </summary>
        public string Centro { get; set; } = string.Empty;




    }
}