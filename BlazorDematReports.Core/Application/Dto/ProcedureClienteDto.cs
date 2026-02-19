using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta una procedura cliente, con informazioni su cliente, centro, commessa e operatore.
    /// </summary>
    public partial class ProcedureClienteDto
    {
        /// <summary>
        /// Identificativo della procedura cliente.
        /// </summary>
        [Required]
        public int IdproceduraCliente { get; set; }

        /// <summary>
        /// Identificativo del cliente associato.
        /// </summary>
        public int Idcliente { get; set; }

        /// <summary>
        /// Nome della procedura cliente.
        /// </summary>
        [Required]
        public string? ProceduraCliente { get; set; }

        /// <summary>
        /// Identificativo del centro associato.
        /// </summary>
        public int? Idcentro { get; set; }

        /// <summary>
        /// Nome della commessa associata.
        /// </summary>
        public string? Commessa { get; set; }

        /// <summary>
        /// Data di inserimento della procedura cliente.
        /// </summary>
        public DateTime DataInserimento { get; set; }

        /// <summary>
        /// Identificativo dell'operatore associato.
        /// </summary>
        public int Idoperatore { get; set; }

        /// <summary>
        /// Descrizione della procedura.
        /// </summary>
        public string? DescrizioneProcedura { get; set; }

        /// <summary>
        /// Nome del cliente associato.
        /// </summary>
        public string? Cliente { get; set; }

        /// <summary>
        /// Nome del centro associato.
        /// </summary>
        public string? Centro { get; set; }

        /// <summary>
        /// Nome dell'operatore associato.
        /// </summary>
        public string? Operatore { get; set; }
    }
}
