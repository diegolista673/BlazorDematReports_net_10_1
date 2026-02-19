using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta una riga della tabella dei task.
    /// </summary>
    public partial class TabellaTaskDto
    {
        /// <summary>
        /// Identificativo del task.
        /// </summary>
        public int IdTask { get; set; }

        /// <summary>
        /// Nome del task.
        /// </summary>
        public string? TaskName { get; set; }

        /// <summary>
        /// Tipo del task.
        /// </summary>
        public int TipoTask { get; set; }

        /// <summary>
        /// Descrizione del task.
        /// </summary>
        [Required]
        public string? Descrizione { get; set; }

        /// <summary>
        /// Indica se il task è attivo.
        /// </summary>
        public bool Attivo { get; set; }
    }
}
