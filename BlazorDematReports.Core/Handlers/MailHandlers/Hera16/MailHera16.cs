using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorDematReports.Core
{
    using Microsoft.VisualBasic;
    using System;
    using System.Collections.Generic;


    public class MailHera16
    {
        public string? Oggetto { get; set; } = null;
        public string? Body { get; set; } = null;
        public string? IDEvento { get; set; } = null;
        public DateTime DataRicezione { get; set; }
        public bool Esito { get; set; }
        public List<string> Allegati { get; set; } = new List<string>();

        public DateTime DataRiferimento { get; set; }
        public bool AllegatoPresente { get; set; }
        public bool Pervenuta { get; set; }
        public string? MessaggioErrore { get; set; } = null;

        private string? _BodyAnswer = null;
        private string? _SubjectAnswer = null;


        public MailHera16()
        {
        }

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
                                _BodyAnswer = _BodyAnswer + "\t - " + allegato + Environment.NewLine;
                        }
                    }
                }

                return _BodyAnswer;
            }
        }

        public string SubjectAnswer
        {
            get
            {
                if (Pervenuta == false)
                    _SubjectAnswer = "DatiMailCsvHera16 - servizio automatico - INFO - Nessuna email ricevuta";
                else if (Pervenuta == true)
                {
                    if (Esito == false)
                        _SubjectAnswer = "DatiMailCsvHera16 - servizio automatico - ERRORE - EMAIL DUPLICATA";
                    else if ((Esito == true & AllegatoPresente == false))
                        _SubjectAnswer = "DatiMailCsvHera16 - servizio automatico - INFO - Nessun allegato alla mail";
                    else if (Esito == true)
                        _SubjectAnswer = "DatiMailCsvHera16 - servizio automatico - INFO - Mail lavorata ";
                }

                return _SubjectAnswer;
            }
        }


    }

}
