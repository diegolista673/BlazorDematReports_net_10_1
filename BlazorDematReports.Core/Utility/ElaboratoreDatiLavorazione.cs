using BlazorDematReports.Core.Utility.Interfaces;
using BlazorDematReports.Core.Utility.Models;
using Entities.Models.DbApplication;


namespace BlazorDematReports.Core.Utility
{
    /// <summary>
    /// Servizio per l'elaborazione dei dati di lavorazione.
    /// <para>
    /// Normalizza i nomi degli operatori, raggruppa e aggrega i dati originali, 
    /// e determina la validità e l'appartenenza degli operatori al centro di lavorazione.
    /// </para>
    /// </summary>
    public class ElaboratoreDatiLavorazione : IElaboratoreDatiLavorazione
    {
        private readonly INormalizzatoreOperatori _normalizzatoreOperatori;
        private readonly IGestoreOperatoriDatiLavorazione _gestoreOperatoriDatiLavorazione;
        private IEnumerable<Operatori>? _elencoOperatori;
        private IEnumerable<OperatoreMondo>? _elencoOperatoriEsterni;

        // Contenitore per i lookup per ricerche efficienti e firme più pulite
        private readonly record struct Lookups(
            ILookup<string, Operatori> Demat,
            ILookup<string, OperatoreMondo> MondoById,
            ILookup<string, OperatoreMondo> MondoBySUtente);

        /// <summary>
        /// Inizializza una nuova istanza di <see cref="ElaboratoreDatiLavorazione"/>.
        /// </summary>
        public ElaboratoreDatiLavorazione(
            INormalizzatoreOperatori normalizzatoreOperatori,
            IGestoreOperatoriDatiLavorazione gestoreOperatoriDatiLavorazione)
        {
            _normalizzatoreOperatori = normalizzatoreOperatori;
            _gestoreOperatoriDatiLavorazione = gestoreOperatoriDatiLavorazione;
        }


        /// <summary>
        /// Elabora i dati di lavorazione originali, normalizzando gli operatori, raggruppando e aggregando i dati,
        /// e restituendo una lista di <see cref="DatiElaborati"/> pronti per la persistenza o l'analisi.
        /// </summary>
        /// <param name="ct">Token di cancellazione.</param>
        public async Task<List<DatiElaborati>> ElaboraDatiLavorazioneAsync(
            List<DatiLavorazione> datiOriginali,
            int idCentro,
            int idProceduraLavorazione,
            int idFaseLavorazione,
            CancellationToken ct = default)
        {
            if (datiOriginali == null || !datiOriginali.Any())
                return new List<DatiElaborati>();

            ct.ThrowIfCancellationRequested();

            try
            {
                // Caricamento ottimizzato degli elenchi di operatori - cache se possibile
                EnsureCachesLoaded();

                ct.ThrowIfCancellationRequested();

                // Creazione di lookup per ricerche più efficienti
                var lookups = BuildLookups();

                var datiRaggruppati = new List<DatiElaborati>(datiOriginali.Count);
                var operatoriAggiunti = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var gruppo in datiOriginali.GroupBy(d => (d.Operatore, d.DataLavorazione)))
                {
                    ct.ThrowIfCancellationRequested();

                    var record = await ProcessGroupAsync(
                        gruppo,
                        idCentro,
                        idProceduraLavorazione,
                        idFaseLavorazione,
                        lookups,
                        operatoriAggiunti);

                    if (record != null)
                        datiRaggruppati.Add(record);
                }

                ct.ThrowIfCancellationRequested();

                // Secondo raggruppamento per aggregare ulteriormente i dati
                return RaggruppaESommaDatiFinali(
                    datiRaggruppati, idProceduraLavorazione, idFaseLavorazione, idCentro);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Errore durante l'elaborazione dei dati: {ex.Message}", ex);
            }
        }

        // Helper: carica le cache degli operatori
        private void EnsureCachesLoaded()
        {
            if (_elencoOperatori == null)
                _elencoOperatori = _gestoreOperatoriDatiLavorazione.GetOperatoriDemat();

            if (_elencoOperatoriEsterni == null)
                _elencoOperatoriEsterni = _gestoreOperatoriDatiLavorazione.GetOperatoriMondo();

            if (_elencoOperatori == null || _elencoOperatoriEsterni == null)
                throw new InvalidOperationException("Impossibile caricare gli elenchi degli operatori necessari per l'elaborazione");
        }

        // Helper: costruisce i lookup per ricerche efficienti
        private Lookups BuildLookups()
        {
            // La chiave Demat è normalizzata per garantire la stessa rappresentazione usata nella ricerca
            var operatoriDematLookup = _elencoOperatori!.ToLookup(
                op => _normalizzatoreOperatori.NormalizzaOperatore(op.Operatore),
                StringComparer.OrdinalIgnoreCase);

            var operatoriMondoByIdUtente = _elencoOperatoriEsterni!.ToLookup(op => op.ID_UTENTE, StringComparer.OrdinalIgnoreCase);

            var operatoriMondoBySUtente = _elencoOperatoriEsterni!.ToLookup(op => op.SUTENTE, StringComparer.OrdinalIgnoreCase);

            return new Lookups(operatoriDematLookup, operatoriMondoByIdUtente, operatoriMondoBySUtente);
        }



        // Helper: elabora un singolo gruppo Operatore+Data
        private async Task<DatiElaborati?> ProcessGroupAsync(
            IGrouping<(string? Operatore, DateTime DataLavorazione), DatiLavorazione> gruppo,
            int idCentro,
            int idProceduraLavorazione,
            int idFaseLavorazione,
            Lookups lookups,
            Dictionary<string, int> operatoriAggiunti)
        {
            string operatoreOriginale = gruppo.Key.Operatore ?? string.Empty;
            string operatoreNormalizzato = _normalizzatoreOperatori.NormalizzaOperatore(operatoreOriginale);

            // Flag che indica se dalle fonti emerge che questi record sono tutti del centro selezionato
            bool appartieneAlCentroSelezionato = gruppo.Any(d => d.AppartieneAlCentroSelezionato);

            var operatoriCorrispondenti = lookups.Demat[operatoreNormalizzato].ToList();

            // Operatore nel centro selezionato?
            var operatoreInCentro = operatoriCorrispondenti
                .FirstOrDefault(op => op.Idcentro == idCentro);

            // Operatore in altri centri?
            var operatoreInAltriCentri = operatoreInCentro == null
                ? operatoriCorrispondenti.FirstOrDefault(op => op.Idcentro != idCentro)
                : null;

            // Ricerca ottimizzata operatore esterno
            OperatoreMondo? operatoreMondo = null;
            int idOperatoreEsterno = 0;

            if (operatoreInCentro == null && operatoreInAltriCentri == null)
            {
                // La ricerca usa il nome normalizzato perché le chiavi del lookup Mondo sono state normalizzate in SetOperatoriEsterniMondoAsync
                operatoreMondo = lookups.MondoById[operatoreNormalizzato].FirstOrDefault() ??
                                 lookups.MondoBySUtente[operatoreNormalizzato].FirstOrDefault();

                // Se trovato esterno e appartiene al centro selezionato, crea l'IdOperatore Demat una sola volta nel batch
                if (operatoreMondo != null && operatoreMondo.IdCentro == idCentro)
                {
                    if (!operatoriAggiunti.TryGetValue(operatoreMondo.ID_UTENTE, out idOperatoreEsterno))
                    {
                        string nomeFormattato = $"{operatoreMondo.ID_UTENTE}({operatoreMondo.SUTENTE})";
                        idOperatoreEsterno = await _gestoreOperatoriDatiLavorazione.AddOperatoreDematAsync(
                            nomeFormattato,
                            idCentro);

                        operatoriAggiunti.Add(operatoreMondo.ID_UTENTE, idOperatoreEsterno);

                        // Aggiorniamo la cache degli operatori dopo l'aggiunta
                        _elencoOperatori = _gestoreOperatoriDatiLavorazione.GetOperatoriDemat();
                    }
                }
            }

            bool operatoreEsternoDelCentroSelezionato = operatoreMondo != null && operatoreMondo.IdCentro == idCentro;

            // Calcolo valori aggregati
            var (documentiTotali, fogliTotali, pagineTotali) = ComputeAggregates(gruppo);

            // Se la fonte garantisce il centro ma l'operatore non è riconosciuto, forziamo il caso Not_Found_Oper
            bool forzaNotFoundOper = appartieneAlCentroSelezionato && operatoreInCentro == null && operatoreMondo == null && operatoreInAltriCentri == null;

            // Creo oggetto DatiElaborati in base alla casistica corretta
            return CreaRecordDatiElaborati(
                operatoreInCentro, operatoreMondo, operatoreInAltriCentri,
                operatoreEsternoDelCentroSelezionato, operatoreNormalizzato,
                gruppo.Key.DataLavorazione, idProceduraLavorazione, idFaseLavorazione, idCentro,
                documentiTotali, fogliTotali, pagineTotali, idOperatoreEsterno,
                forzaNotFoundOper);
        }

        // Helper: somma i valori aggregati del gruppo
        private static (int Documenti, int Fogli, int Pagine) ComputeAggregates(IEnumerable<DatiLavorazione> gruppo)
        {
            int documentiTotali = gruppo.Sum(d => d.Documenti) ?? 0;
            int fogliTotali = gruppo.Sum(d => d.Fogli) ?? 0;
            int pagineTotali = gruppo.Sum(d => d.Pagine) ?? 0;
            return (documentiTotali, fogliTotali, pagineTotali);
        }

        /// <summary>
        /// Crea un record DatiElaborati in base alla casistica dell'operatore trovato
        /// </summary>
        private DatiElaborati? CreaRecordDatiElaborati(
            Operatori? operatoreInCentro,
            OperatoreMondo? operatoreMondo,
            Operatori? operatoreInAltriCentri,
            bool operatoreEsternoDelCentroSelezionato,
            string operatoreNormalizzato,
            DateTime dataLavorazione,
            int idProceduraLavorazione,
            int idFaseLavorazione,
            int idCentro,
            int documenti,
            int fogli,
            int pagine,
            int idOperatoreEsterno,  // Aggiunto parametro per ID operatore esterno
            bool forzaNotFoundOper)
        {

            var valoriComuni = new
            {
                DataLavorazione = dataLavorazione,
                DataAggiornamento = DateTime.Now,
                FlagInserimentoAuto = true,
                FlagInserimentoManuale = false,
                IdProceduraLavorazione = idProceduraLavorazione,
                IdFaseLavorazione = idFaseLavorazione,
                IdCentro = idCentro,
                Documenti = documenti,
                Fogli = fogli,
                Pagine = pagine
            };

            // Se forzato, restituisci direttamente Not_Found_Oper mantenendo il centro richiesto
            if (forzaNotFoundOper)
            {
                return new DatiElaborati
                {
                    Operatore = "Not_Found_Oper",
                    IdOperatore = 180,
                    DataLavorazione = valoriComuni.DataLavorazione,
                    DataAggiornamento = valoriComuni.DataAggiornamento,
                    FlagInserimentoAuto = valoriComuni.FlagInserimentoAuto,
                    FlagInserimentoManuale = valoriComuni.FlagInserimentoManuale,
                    IdProceduraLavorazione = valoriComuni.IdProceduraLavorazione,
                    IdFaseLavorazione = valoriComuni.IdFaseLavorazione,
                    IdCentro = valoriComuni.IdCentro,
                    OperatoreNonRiconosciuto = operatoreNormalizzato,
                    Documenti = valoriComuni.Documenti,
                    Fogli = valoriComuni.Fogli,
                    Pagine = valoriComuni.Pagine
                };
            }


            return (operatoreInCentro, operatoreMondo, operatoreEsternoDelCentroSelezionato, operatoreInAltriCentri) switch
            {
                // Caso 1: Operatore trovato nel centro selezionato
                (not null, _, _, _) => new DatiElaborati
                {
                    Operatore = operatoreInCentro!.Operatore,
                    IdOperatore = operatoreInCentro!.Idoperatore,
                    DataLavorazione = valoriComuni.DataLavorazione,
                    DataAggiornamento = valoriComuni.DataAggiornamento,
                    FlagInserimentoAuto = valoriComuni.FlagInserimentoAuto,
                    FlagInserimentoManuale = valoriComuni.FlagInserimentoManuale,
                    IdProceduraLavorazione = valoriComuni.IdProceduraLavorazione,
                    IdFaseLavorazione = valoriComuni.IdFaseLavorazione,
                    IdCentro = valoriComuni.IdCentro,
                    OperatoreNonRiconosciuto = null,
                    Documenti = valoriComuni.Documenti,
                    Fogli = valoriComuni.Fogli,
                    Pagine = valoriComuni.Pagine
                },

                // Caso 2: Operatore esterno del centro selezionato
                (null, not null, true, _) => new DatiElaborati
                {
                    Operatore = operatoreMondo!.ID_UTENTE,
                    IdOperatore = idOperatoreEsterno, // Ora usiamo l'ID effettivo dell'operatore
                    DataLavorazione = valoriComuni.DataLavorazione,
                    DataAggiornamento = valoriComuni.DataAggiornamento,
                    FlagInserimentoAuto = valoriComuni.FlagInserimentoAuto,
                    FlagInserimentoManuale = valoriComuni.FlagInserimentoManuale,
                    IdProceduraLavorazione = valoriComuni.IdProceduraLavorazione,
                    IdFaseLavorazione = valoriComuni.IdFaseLavorazione,
                    IdCentro = valoriComuni.IdCentro,
                    OperatoreNonRiconosciuto = null,
                    Documenti = valoriComuni.Documenti,
                    Fogli = valoriComuni.Fogli,
                    Pagine = valoriComuni.Pagine
                },

                // Caso 3: Operatore trovato ma in altro centro o operatore esterno non del centro selezionato
                (null, _, _, not null) or (null, not null, false, _) => new DatiElaborati
                {
                    Operatore = "Not_Found_Oper",
                    IdOperatore = 180,
                    DataLavorazione = valoriComuni.DataLavorazione,
                    DataAggiornamento = valoriComuni.DataAggiornamento,
                    FlagInserimentoAuto = valoriComuni.FlagInserimentoAuto,
                    FlagInserimentoManuale = valoriComuni.FlagInserimentoManuale,
                    IdProceduraLavorazione = valoriComuni.IdProceduraLavorazione,
                    IdFaseLavorazione = valoriComuni.IdFaseLavorazione,
                    IdCentro = valoriComuni.IdCentro,
                    OperatoreNonRiconosciuto = operatoreNormalizzato,
                    Documenti = valoriComuni.Documenti,
                    Fogli = valoriComuni.Fogli,
                    Pagine = valoriComuni.Pagine
                },

                // Caso 4: Nessuno degli altri casi (Operatore non trovato)
                _ => null
            };
        }

        /// <summary>
        /// Raggruppa i dati per operatore e somma i valori
        /// </summary>
        private List<DatiElaborati> RaggruppaESommaDatiFinali(
            List<DatiElaborati> datiRaggruppati,
            int idProceduraLavorazione,
            int idFaseLavorazione,
            int idCentro)
        {
            return datiRaggruppati
                .GroupBy(d => new { d.Operatore, d.IdOperatore, d.OperatoreNonRiconosciuto, d.DataLavorazione.Date })
                .Select(gruppo => new DatiElaborati
                {
                    Operatore = gruppo.Key.Operatore,
                    IdOperatore = gruppo.Key.IdOperatore,
                    // Prendiamo la data di lavorazione più recente
                    DataLavorazione = gruppo.OrderByDescending(d => d.DataLavorazione.Date).First().DataLavorazione,
                    DataAggiornamento = DateTime.Now,
                    FlagInserimentoAuto = true,
                    FlagInserimentoManuale = false,
                    IdProceduraLavorazione = idProceduraLavorazione,
                    IdFaseLavorazione = idFaseLavorazione,
                    IdCentro = idCentro,
                    OperatoreNonRiconosciuto = gruppo.Key.OperatoreNonRiconosciuto,
                    Documenti = gruppo.Sum(d => d.Documenti),
                    Fogli = gruppo.Sum(d => d.Fogli),
                    Pagine = gruppo.Sum(d => d.Pagine)
                })
                .ToList();
        }
    }
}