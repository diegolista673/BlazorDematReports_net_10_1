using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta un operatore con informazioni su centro, ruolo, azienda e visibilità.
    /// </summary>
    public partial class OperatoriDto
    {
        /// <summary>
        /// Identificativo dell'operatore.
        /// </summary>
        public int Idoperatore { get; set; }

        /// <summary>
        /// Nome dell'operatore.
        /// </summary>
        [Required]
        public string? Operatore { get; set; }

        /// <summary>
        /// Identificativo del centro associato all'operatore.
        /// </summary>
        [Required]
        public int? Idcentro { get; set; }

        /// <summary>
        /// Ruolo dell'operatore.
        /// </summary>
        public string? Ruolo { get; set; }

        /// <summary>
        /// Nome dell'azienda di appartenenza.
        /// </summary>
        [Required]
        public string? Azienda { get; set; }

        /// <summary>
        /// Password dell'operatore (se gestita lato DTO).
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Nome del centro associato.
        /// </summary>
        public string? Centro { get; set; }

        /// <summary>
        /// Nome del centro visibile.
        /// </summary>
        public string? CentroVisibile { get; set; }

        /// <summary>
        /// Indica se l'operatore è attivo.
        /// </summary>
        public bool FlagOperatoreAttivo { get; set; }

        /// <summary>
        /// Identificativo del ruolo associato all'operatore.
        /// </summary>
        [Required]
        public int? IdRuolo { get; set; }

        /// <summary>
        /// Collezione dei centri visibili per l'operatore.
        /// </summary>
        public virtual ICollection<CentriVisibiliDto>? CentriVisibiliDto { get; set; }
    }
}
