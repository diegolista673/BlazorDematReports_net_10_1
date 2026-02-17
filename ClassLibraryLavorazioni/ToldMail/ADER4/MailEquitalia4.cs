namespace WorkerServiceAderEquitalia4
{
    using Microsoft.VisualBasic;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Classe per rappresentare le informazioni e lo stato di una mail Equitalia 4.
    /// Contiene dettagli come oggetto, corpo, allegati e risultati dell'elaborazione.
    /// </summary>
    public class MailEquitalia4
    {
        /// <summary>
        /// Oggetto della mail.
        /// </summary>
        public string Oggetto { get; set; }

        /// <summary>
        /// Corpo della mail.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Identificativo dell'evento associato alla mail.
        /// </summary>
        public string IDEvento { get; set; }

        /// <summary>
        /// Data di ricezione della mail.
        /// </summary>
        public DateTime DataRicezione { get; set; }

        /// <summary>
        /// Esito dell'elaborazione della mail.
        /// </summary>
        public bool Esito { get; set; }

        /// <summary>
        /// Elenco degli allegati presenti nella mail.
        /// </summary>
        public List<string> Allegati { get; set; } = new List<string>();

        /// <summary>
        /// Totale delle pre-accettazioni.
        /// </summary>
        public int PreAccettazione { get; set; }

        /// <summary>
        /// Totale delle ripartizioni.
        /// </summary>
        public int Ripartizione { get; set; }

        /// <summary>
        /// Totale delle scansioni effettuate tramite Captiva.
        /// </summary>
        public int ScansioneCaptiva { get; set; }

        /// <summary>
        /// Totale delle restituzioni.
        /// </summary>
        public int Restituzione { get; set; }

        /// <summary>
        /// Data di riferimento della mail.
        /// </summary>
        public DateTime DataRiferimento { get; set; }

        /// <summary>
        /// Indica se la mail contiene allegati.
        /// </summary>
        public bool AllegatoPresente { get; set; }

        /// <summary>
        /// Indica se la mail è stata ricevuta.
        /// </summary>
        public bool Pervenuta { get; set; }

        /// <summary>
        /// Messaggio di errore associato all'elaborazione della mail.
        /// </summary>
        public string MessaggioErrore { get; set; }

        private string _BodyAnswer;
        private string _SubjectAnswer;

        /// <summary>
        /// Totale delle scansioni effettuate tramite Sorter.
        /// </summary>
        public int ScansioneSorter { get; set; }

        /// <summary>
        /// Totale degli scarti delle scansioni effettuate tramite Sorter.
        /// </summary>
        public int ScartiScansioneSorter { get; set; }

        /// <summary>
        /// Totale delle scansioni di buste effettuate tramite Sorter.
        /// </summary>
        public int ScansioneSorterBuste { get; set; }

        /// <summary>
        /// Totale degli scarti delle scansioni di buste effettuate tramite Sorter.
        /// </summary>
        public int ScartiScansioneSorterBuste { get; set; }

        /// <summary>
        /// Costruttore predefinito della classe.
        /// </summary>
        public MailEquitalia4()
        {
        }

        /// <summary>
        /// Restituisce il corpo della risposta basato sullo stato della mail.
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
                                _BodyAnswer = _BodyAnswer + Constants.vbTab + " - " + allegato + Environment.NewLine;
                        }
                    }
                }

                return _BodyAnswer;
            }
        }

        /// <summary>
        /// Restituisce l'oggetto della risposta basato sullo stato della mail.
        /// </summary>
        public string SubjectAnswer
        {
            get
            {
                if (Pervenuta == false)
                    _SubjectAnswer = "Ader_Equitalia_4 - servizio automatico - INFO - Nessuna email ricevuta";
                else if (Pervenuta == true)
                {
                    if (Esito == false)
                        _SubjectAnswer = "Ader_Equitalia_4 - servizio automatico - ERRORE - EMAIL DUPLICATA";
                    else if ((Esito == true & AllegatoPresente == false))
                        _SubjectAnswer = "Ader_Equitalia_4 - servizio automatico - INFO - Nessun allegato alla mail";
                    else if (Esito == true)
                        _SubjectAnswer = "Ader_Equitalia_4 - servizio automatico - INFO - Mail lavorata ";
                }

                return _SubjectAnswer;
            }
        }
    }
}
