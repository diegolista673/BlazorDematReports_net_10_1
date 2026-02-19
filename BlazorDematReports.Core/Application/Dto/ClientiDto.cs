using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO per rappresentare i clienti.
    /// Contiene informazioni sull'identificativo, nome, operatore, data di creazione e centro di lavorazione.
    /// </summary>
    public partial class ClientiDto
    {
        /// <summary>
        /// Identificativo univoco del cliente.
        /// </summary>
        public int IdCliente { get; set; }

        /// <summary>
        /// Nome del cliente.
        /// </summary>
        [Required]
        public string? NomeCliente { get; set; }

        /// <summary>
        /// Identificativo dell'operatore associato al cliente.
        /// </summary>
        public int IdOperatore { get; set; }

        /// <summary>
        /// Data di creazione del cliente.
        /// </summary>
        public DateTime DataCreazioneCliente { get; set; }

        /// <summary>
        /// Identificativo del centro di lavorazione associato al cliente.
        /// </summary>
        [Required]
        public int? IdCentroLavorazione { get; set; }

        /// <summary>
        /// Nome del centro di lavorazione associato al cliente.
        /// </summary>
        public string? Centro { get; set; }
    }
}
