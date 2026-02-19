using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta una produzione operatore, con informazioni su lavorazione, fase, turno, tempi e note.
    /// </summary>
    public partial class ProduzioneOperatoriDto
    {
        /// <summary>
        /// Identificativo della produzione.
        /// </summary>
        public int IdProduzione { get; set; }

        /// <summary>
        /// Identificativo dell'operatore.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdOperatore { get; set; }

        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdProceduraLavorazione { get; set; }

        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdFaseLavorazione { get; set; }

        /// <summary>
        /// Identificativo del turno.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public int? IdTurno { get; set; }

        /// <summary>
        /// Identificativo del tipo turno.
        /// </summary>
        public int? IdTipoTurno { get; set; }

        /// <summary>
        /// Tempo lavorato in ore centesimali.
        /// </summary>
        [Required]
        [Range(0.25, int.MaxValue, ErrorMessage = "Please enter a correct value ")]
        public double TempoLavOreCent { get; set; } = 0;

        /// <summary>
        /// Ore lavorate.
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Ore is required")]
        public int Ore { get; set; } = 0;

        /// <summary>
        /// Minuti lavorati.
        /// </summary>
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Minuti is required ")]
        public int Minuti { get; set; } = 0;

        /// <summary>
        /// Nome della lavorazione.
        /// </summary>
        [Required(ErrorMessage = "Lavorazione is required")]
        public string? Lavorazione { get; set; }

        /// <summary>
        /// Nome della fase.
        /// </summary>
        [Required(ErrorMessage = "Fase is required")]
        public string? Fase { get; set; }

        /// <summary>
        /// Nome del turno.
        /// </summary>
        [Required(ErrorMessage = "Turno is required")]
        public string? Turno { get; set; }

        /// <summary>
        /// Tipo del turno.
        /// </summary>
        public string? TipoTurno { get; set; }

        /// <summary>
        /// Nome del reparto.
        /// </summary>
        [Required(ErrorMessage = "Reparto is required")]
        public string? Reparto { get; set; }

        /// <summary>
        /// Data della lavorazione.
        /// </summary>
        [Required(ErrorMessage = "Data is required")]
        public DateTime? DataLavorazione { get; set; } = DateTime.Now.Date;

        /// <summary>
        /// Note aggiuntive.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Identificativo del reparto.
        /// </summary>
        public int? IdReparti { get; set; }

        /// <summary>
        /// Flag per lavorazione con altra utenza.
        /// </summary>
        public int? FlagLlavoratoConAltraUtenza { get; set; }

        /// <summary>
        /// Nome dell'altra utenza.
        /// </summary>
        public string? AltraUtenza { get; set; }

        /// <summary>
        /// Identificativo del centro.
        /// </summary>
        public int? IdCentro { get; set; }

        /// <summary>
        /// Nome dell'operatore.
        /// </summary>
        [Required(ErrorMessage = "Operatore is required")]
        public string? Operatore { get; set; }

        /// <summary>
        /// Indica se è stato lavorato con altra utenza.
        /// </summary>
        public bool CheckLavoratoConAltraUtenza { get; set; }

        /// <summary>
        /// Collezione delle tipologie totali di produzione associate.
        /// </summary>
        public virtual ICollection<TipologieTotaliProduzioneDto>? TipologieTotaliProduzioneDto { get; set; }
    }
}
