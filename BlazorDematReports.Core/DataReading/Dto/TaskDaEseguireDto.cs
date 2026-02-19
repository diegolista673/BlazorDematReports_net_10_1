namespace BlazorDematReports.Core.DataReading.Dto
{
    /// <summary>
    /// DTO che rappresenta un task da eseguire per la lavorazione dei dati.
    /// </summary>
    public partial class TaskDaEseguireDto
    {
        /// <summary>
        /// Identificativo del task da eseguire.
        /// </summary>
        public int IdTaskDaEseguire { get; set; }

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
        public string? Descrizione { get; set; }

        /// <summary>
        /// Identificativo del task Hangfire.
        /// </summary>
        public string? IdTaskHangFire { get; set; }

        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int IdProceduraLavorazione { get; set; }

        /// <summary>
        /// Orario di esecuzione del task.
        /// </summary>
        public TimeSpan? TimeTask { get; set; }

        /// <summary>
        /// Ora di esecuzione del task.
        /// </summary>
        public int? Hour { get; set; }

        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        public int IdFaseLavorazione { get; set; }

        /// <summary>
        /// Nome della fase di lavorazione.
        /// </summary>
        public string? FaseLavorazione { get; set; }

        /// <summary>
        /// Data di inizio del task.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Data di fine del task.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Numero di giorni precedenti da considerare per l'esecuzione.
        /// </summary>
        public int GiorniPrecedenti { get; set; } = 1;

        /// <summary>
        /// Stato del task.
        /// </summary>
        public string? Stato { get; set; }

        /// <summary>
        /// Data dello stato del task.
        /// </summary>
        public DateTime DataStato { get; set; }

        /// <summary>
        /// Nome della lavorazione.
        /// </summary>
        public string? Lavorazione { get; set; }

        /// <summary>
        /// Titolo della query associata al task (legacy - deprecato).
        /// </summary>
        public string? TitoloQuery { get; set; }

        /// <summary>
        /// Query SQL da eseguire (legacy - deprecato).
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// Identificativo del centro di lavorazione.
        /// </summary>
        public int IdCentro { get; set; }

        /// <summary>
        /// Espressione Cron per la pianificazione del task.
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Indica se il task è abilitato.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Ultima esecuzione del task in formato UTC.
        /// </summary>
        public DateTime? LastRunUtc { get; set; }

        /// <summary>
        /// Ultimo errore verificatosi durante l'esecuzione del task.
        /// </summary>
        public string? LastError { get; set; }

        /// <summary>
        /// Numero di fallimenti consecutivi dell'esecuzione del task.
        /// </summary>
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// FK a ConfigurazioneFontiDati per sistema unificato.
        /// Contiene la configurazione completa per SQL, Email, Handler o Pipeline.
        /// </summary>
        public int? IdConfigurazioneDatabase { get; set; }
    }
}
