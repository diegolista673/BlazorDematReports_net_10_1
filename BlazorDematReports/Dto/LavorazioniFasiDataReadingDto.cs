using BlazorDematReports.Core.DataReading.Dto;

namespace BlazorDematReports.Dto
{
    /// <summary>
    /// DTO che rappresenta una fase di lavorazione con informazioni di data reading e i relativi task da eseguire.
    /// </summary>
    public partial class LavorazioniFasiDataReadingDto
    {
        /// <summary>
        /// Inizializza una nuova istanza della classe LavorazioniFasiDataReadingDto.
        /// </summary>
        public LavorazioniFasiDataReadingDto()
        {
            TaskDaEseguireDto = new List<TaskDaEseguireDto>();
        }

        /// <summary>
        /// Identificativo della lavorazione fase data reading.
        /// </summary>
        public int IdlavorazioneFaseDateReading { get; set; }
        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        public int IdFaseLavorazione { get; set; }
        /// <summary>
        /// Indica se la fase è abilitata per il data reading.
        /// </summary>
        public bool FlagDataReading { get; set; }
        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int IdProceduraLavorazione { get; set; }
        /// <summary>
        /// Nome della fase di lavorazione.
        /// </summary>
        public string? FaseLavorazione { get; set; }
        /// <summary>
        /// Nome della lavorazione.
        /// </summary>
        public string? Lavorazione { get; set; }
        /// <summary>
        /// Nome del centro di lavorazione.
        /// </summary>
        public string? Centro { get; set; }
        /// <summary>
        /// Indica se la fase è abilitata per il grafico documenti.
        /// </summary>
        public bool FlagGraficoDocumenti { get; set; }
        /// <summary>
        /// Lista dei task da eseguire associati alla fase di lavorazione.
        /// </summary>
        public virtual List<TaskDaEseguireDto> TaskDaEseguireDto { get; set; }
    }
}
