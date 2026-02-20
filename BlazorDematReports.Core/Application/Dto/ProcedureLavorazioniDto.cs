using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace BlazorDematReports.Core.Application.Dto
{
    /// <summary>
    /// DTO che rappresenta una procedura di lavorazione con tutte le informazioni associate.
    /// Include dati anagrafici, configurazioni, fasi di lavorazione e gestione file.
    /// </summary>
    public partial class ProcedureLavorazioniDto
    {
        /// <summary>
        /// Identificativo univoco della procedura di lavorazione.
        /// </summary>
        public int? IdproceduraLavorazione { get; set; }

        /// <summary>
        /// Identificativo della procedura cliente associata.
        /// </summary>
        public int IdproceduraCliente { get; set; }

        /// <summary>
        /// Data di inserimento della procedura.
        /// </summary>
        public DateTime DataInserimento { get; set; }

        /// <summary>
        /// Identificativo dell'operatore che ha creato la procedura.
        /// </summary>
        public int? Idoperatore { get; set; }

        /// <summary>
        /// Nome della procedura di lavorazione. Campo obbligatorio.
        /// </summary>
        [Required]
        public string? NomeProcedura { get; set; }

        /// <summary>
        /// Note aggiuntive sulla procedura di lavorazione.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Identificativo del formato dati di produzione utilizzato.
        /// </summary>
        public int IdformatoDatiProduzione { get; set; }

        /// <summary>
        /// Array di byte contenente l'immagine del logo associato alla procedura.
        /// </summary>
        public byte[]? FileImg { get; set; }

        /// <summary>
        /// Identificativo del reparto di appartenenza.
        /// </summary>
        public int Idreparti { get; set; }

        /// <summary>
        /// Flag che indica se è abilitata la lavorazione in altro reparto.
        /// </summary>
        public int? FlagAbilitaLavorazioneAltroReparto { get; set; }

        /// <summary>
        /// Identificativo del centro di lavorazione. Campo obbligatorio.
        /// </summary>
        [Required]
        public int? Idcentro { get; set; }

        /// <summary>
        /// Flag che indica se i documenti sono stati lavorati.
        /// </summary>
        public bool? FlagDocLavorati { get; set; }

        /// <summary>
        /// Numero di giorni per l'elaborazione.
        /// </summary>
        public int? NumGiorniElaborazione { get; set; }

        /// <summary>
        /// Nome del servizio di elaborazione utilizzato.
        /// </summary>
        public string? ServizioElaborazione { get; set; }

        /// <summary>
        /// Stringa Base64 contenente il logo della procedura.
        /// </summary>
        public string? LogoBase64 { get; set; }

        /// <summary>
        /// Nome programma della procedura di lavorazione.
        /// </summary>
        public string? NomeProceduraProgramma { get; set; }

        /// <summary>
        /// Flag che indica se è abilitato il data reading.
        /// </summary>
        public bool? DataReading { get; set; }

        /// <summary>
        /// Nome della procedura cliente. Campo obbligatorio.
        /// </summary>
        [Required]
        public string? ProceduraCliente { get; set; }

        /// <summary>
        /// Formato dei dati di produzione. Campo obbligatorio.
        /// </summary>
        [Required]
        public string? FormatoDatiProduzione { get; set; }

        /// <summary>
        /// Nome del centro di lavorazione.
        /// </summary>
        public string? Centro { get; set; }

        /// <summary>
        /// Nome del reparto di lavorazione. Campo obbligatorio.
        /// </summary>
        [Required]
        public string? Reparto { get; set; }

        /// <summary>
        /// Nome della fase di lavorazione. Campo obbligatorio.
        /// </summary>
        [Required]
        public string? Fase { get; set; }

        /// <summary>
        /// Collezione delle fasi di lavorazione con data reading associate alla procedura.
        /// </summary>
        public virtual ICollection<LavorazioniFasiDataReadingDto>? LavorazioniFasiDataReadingsDto { get; set; }

        /// <summary>
        /// Collezione delle query associate alla procedura di lavorazione.
        /// </summary>
        public virtual ICollection<QueryProcedureLavorazioniDto>? QueryProcedureLavorazioniDto { get; set; }

        /// <summary>
        /// File del logo caricato tramite l'interfaccia utente.
        /// </summary>
        public IBrowserFile? LogoFile { get; set; }

        /// <summary>
        /// Identificativo del cliente associato alla procedura.
        /// </summary>
        public int? IdCliente { get; set; }

        /// <summary>
        /// Nome del cliente associato alla procedura.
        /// </summary>
        public string? NomeCliente { get; set; }

        /// <summary>
        /// Flag che indica se è abilitata la multi-fase per il grafico.
        /// </summary>
        public bool? FlagAbilitaMultiFaseGrafico { get; set; }

        /// <summary>
        /// Campo privato per memorizzare la dimensione del file.
        /// </summary>
        private double? size;

        /// <summary>
        /// Dimensione del file logo in MB. Viene calcolata automaticamente se LogoFile è presente.
        /// </summary>
        public double? FileSizeMB
        {
            get
            {
                if (this.LogoFile != null)
                    return size = (double)LogoFile.Size / (1024 * 1024);
                else
                    return null;
            }

            set { size = value; }
        }

        /// <summary>
        /// Flag che indica se la procedura è attiva.
        /// </summary>
        public bool? Attiva { get; set; }

        /// <summary>
        /// Stato della lavorazione basato sul flag Attiva.
        /// </summary>
        public string StatoLavorazione => Attiva == true ? "ATTIVA" : "NON ATTIVA";
    }
}
