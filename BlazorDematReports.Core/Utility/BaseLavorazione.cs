using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;

namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Classe base astratta per la gestione delle lavorazioni.
    /// <para>
    /// Fornisce proprietà e metodi comuni per tutte le lavorazioni, tra cui la gestione dei parametri di lavorazione,
    /// la normalizzazione degli operatori e l'inizializzazione dei dati di lavorazione.
    /// Le classi derivate devono implementare la logica specifica di recupero dati tramite <see cref="SetDatiDematAsync"/>.
    /// </para>
    /// </summary>
    public abstract partial class BaseLavorazione
    {
        /// <summary>
        /// Nome della procedura di lavorazione.
        /// </summary>
        public string? NomeProcedura { get; set; }

        /// <summary>
        /// Identificativo della fase di lavorazione.
        /// </summary>
        public int IDFaseLavorazione { get; set; }

        /// <summary>
        /// Identificativo della procedura di lavorazione.
        /// </summary>
        public int IDProceduraLavorazione { get; set; }

        /// <summary>
        /// Identificativo del centro di lavorazione.
        /// </summary>
        public int? IDCentro { get; set; }

        /// <summary>
        /// Data di inizio della lavorazione.
        /// </summary>
        public DateTime StartDataLavorazione { get; set; }

        /// <summary>
        /// Data di fine della lavorazione.
        /// </summary>
        public DateTime? EndDataLavorazione { get; set; }

        /// <summary>
        /// Esito della lettura dati (true se la lettura è andata a buon fine).
        /// </summary>
        public bool EsitoLetturaDato { get; set; }

        /// <summary>
        /// Lista dei dati di lavorazione acquisiti.
        /// </summary>
        public List<DatiLavorazione>? lstDatiLavorazione;

        /// <summary>
        /// Servizio per la normalizzazione degli operatori.
        /// </summary>
        protected readonly INormalizzatoreOperatori _normalizzatoreOperatori;

        /// <summary>
        /// Servizio per la gestione degli operatori di lavorazione.
        /// </summary>
        protected readonly IGestoreOperatoriDatiLavorazione _gestoreOperatoriDati;

        /// <summary>
        /// Servizio per l'elaborazione dei dati di lavorazione.
        /// </summary>
        public IElaboratoreDatiLavorazione ElaboraDatiLavorazione;

        /// <summary>
        /// Costruttore base per l'inizializzazione delle dipendenze comuni alle lavorazioni.
        /// </summary>
        /// <param name="normalizzatoreOperatori">Servizio di normalizzazione operatori.</param>
        /// <param name="gestoreOperatoriDati">Servizio di gestione operatori dati lavorazione.</param>
        /// <param name="elaboratoreDati">Servizio di elaborazione dati lavorazione.</param>
        public BaseLavorazione(
            INormalizzatoreOperatori normalizzatoreOperatori,
            IGestoreOperatoriDatiLavorazione gestoreOperatoriDati,
            IElaboratoreDatiLavorazione elaboratoreDati
        )
        {
            _normalizzatoreOperatori = normalizzatoreOperatori;
            _gestoreOperatoriDati = gestoreOperatoriDati;
            ElaboraDatiLavorazione = elaboratoreDati;
        }

        /// <summary>
        /// Imposta i parametri di base della lavorazione e inizializza i servizi di normalizzazione e gestione operatori.
        /// </summary>
        /// <param name="idProceduraLavorazione">Identificativo della procedura di lavorazione.</param>
        /// <param name="idFaseLavorazione">Identificativo della fase di lavorazione.</param>
        /// <param name="idCentro">Identificativo del centro di lavorazione.</param>
        /// <param name="startDate">Data di inizio lavorazione.</param>
        /// <param name="endDate">Data di fine lavorazione.</param>
        public async Task SetBase(int idProceduraLavorazione, int idFaseLavorazione, int idCentro, DateTime startDate, DateTime endDate)
        {
            IDProceduraLavorazione = idProceduraLavorazione;
            IDFaseLavorazione = idFaseLavorazione;
            StartDataLavorazione = startDate;
            EndDataLavorazione = endDate;
            IDCentro = idCentro;

            await _normalizzatoreOperatori.SetNamesNormalizzatiAsync();
            await _gestoreOperatoriDati.SetOperatoriAsync();
            await _gestoreOperatoriDati.SetOperatoriEsterniMondoAsync();
        }

        /// <summary>
        /// Metodo astratto che deve essere implementato nelle classi derivate per recuperare i dati di lavorazione.
        /// </summary>
        /// <returns>Una lista di <see cref="DatiLavorazione"/> contenente i dati acquisiti dalla fonte dati.</returns>
        public abstract Task<List<DatiLavorazione>> SetDatiDematAsync();
    }
}



