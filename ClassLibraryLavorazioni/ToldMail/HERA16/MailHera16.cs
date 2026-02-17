using Microsoft.VisualBasic;

namespace LibraryLavorazioni.LavorazioniViaMail.HERA16
{
    /// <summary>
    /// Classe per rappresentare le informazioni e lo stato di una mail Hera16.
    /// Contiene dettagli come oggetto, corpo, allegati e risultati dell'elaborazione per il sistema Hera16.
    /// </summary>
    public class MailHera16
    {
        /// <summary>
        /// Oggetto della mail ricevuta.
        /// </summary>
        public string? Oggetto { get; set; } = null;
        
        /// <summary>
        /// Corpo testuale della mail ricevuta.
        /// </summary>
        public string? Body { get; set; } = null;
        
        /// <summary>
        /// Identificativo univoco dell'evento associato alla mail.
        /// </summary>
        public string? IDEvento { get; set; } = null;
        
        /// <summary>
        /// Data e ora di ricezione della mail.
        /// </summary>
        public DateTime DataRicezione { get; set; }
        
        /// <summary>
        /// Esito dell'elaborazione della mail (true = successo, false = errore).
        /// </summary>
        public bool Esito { get; set; }
        
        /// <summary>
        /// Elenco dei nomi degli allegati presenti nella mail.
        /// </summary>
        public List<string> Allegati { get; set; } = new List<string>();

        /// <summary>
        /// Data di riferimento per l'elaborazione dei dati contenuti nella mail.
        /// </summary>
        public DateTime DataRiferimento { get; set; }
        
        /// <summary>
        /// Indica se la mail contiene allegati da elaborare.
        /// </summary>
        public bool AllegatoPresente { get; set; }
        
        /// <summary>
        /// Indica se è stata ricevuta almeno una mail nel periodo di elaborazione.
        /// </summary>
        public bool Pervenuta { get; set; }
        
        /// <summary>
        /// Messaggio di errore dettagliato in caso di elaborazione fallita.
        /// </summary>
        public string? MessaggioErrore { get; set; } = null;

        /// <summary>
        /// Campo privato per la memorizzazione del corpo della risposta.
        /// </summary>
        private string? _BodyAnswer = null;
        
        /// <summary>
        /// Campo privato per la memorizzazione dell'oggetto della risposta.
        /// </summary>
        private string? _SubjectAnswer = null;

        /// <summary>
        /// Inizializza una nuova istanza della classe <see cref="MailHera16"/>.
        /// </summary>
        public MailHera16()
        {
        }

        /// <summary>
        /// Genera il corpo della mail di risposta basato sullo stato dell'elaborazione.
        /// Il contenuto varia in base ai valori di Pervenuta, Esito e AllegatoPresente.
        /// </summary>
        public string BodyAnswer
        {
            get
            {
                if (Pervenuta == false)
                    _BodyAnswer = "Nessuna email ricevuta" + Environment.NewLine;
                else if (Pervenuta == true)
                {
                    if (Esito == false)
                    {
                        _BodyAnswer = "Mail lavorata :" + Environment.NewLine;
                        _BodyAnswer = _BodyAnswer + Environment.NewLine + Oggetto + "  - Data Ricezione : " + DataRicezione + "  - IDEvento : " + IDEvento + Environment.NewLine + Environment.NewLine;
                        _BodyAnswer = _BodyAnswer + "EMAIL DUPLICATA, controllare l'email e gli eventuali Allegati - " + Environment.NewLine;
                        _BodyAnswer = _BodyAnswer + Environment.NewLine + MessaggioErrore + Environment.NewLine;
                    }
                    else if ((Esito == true & AllegatoPresente == false))
                        _BodyAnswer = "Nessun allegato alla mail del " + DataRicezione + "  - IDEvento : " + IDEvento + Environment.NewLine + Environment.NewLine;
                    else if (Esito == true)
                    {
                        _BodyAnswer = "Mail lavorata :" + Environment.NewLine;
                        _BodyAnswer = _BodyAnswer + Environment.NewLine + Oggetto + "  - Data Ricezione : " + DataRicezione + "  - IDEvento : " + IDEvento + Environment.NewLine + Environment.NewLine;

                        if (Allegati.Count > 0)
                        {
                            _BodyAnswer = _BodyAnswer + "Allegati : " + Environment.NewLine;

                            foreach (var allegato in Allegati)
                                _BodyAnswer = _BodyAnswer + "\t" + " - " + allegato + Environment.NewLine;
                        }
                    }
                }

                return _BodyAnswer;
            }
        }

        /// <summary>
        /// Genera l'oggetto della mail di risposta basato sullo stato dell'elaborazione.
        /// L'oggetto varia in base ai valori di Pervenuta, Esito e AllegatoPresente.
        /// </summary>
        public string SubjectAnswer
        {
            get
            {
                if (Pervenuta == false)
                    _SubjectAnswer = "Hera16 - servizio automatico - INFO - Nessuna email ricevuta";
                else if (Pervenuta == true)
                {
                    if (Esito == false)
                        _SubjectAnswer = "Hera16 - servizio automatico - ERRORE - EMAIL DUPLICATA";
                    else if ((Esito == true & AllegatoPresente == false))
                        _SubjectAnswer = "Hera16 - servizio automatico - INFO - Nessun allegato alla mail";
                    else if (Esito == true)
                        _SubjectAnswer = "Hera16 - servizio automatico - INFO - Mail lavorata ";
                }

                return _SubjectAnswer;
            }
        }
    }
}
