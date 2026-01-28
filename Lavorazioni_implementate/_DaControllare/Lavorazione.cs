using Entities.Models;
using Entities.Models.DbApplication;
using System;
using System.Collections.Generic;
using System.Data;


namespace LibraryLavorazioni
{

    public abstract class Lavorazione
    {
        
        public string NomeProcedura { get; set; }
        public string FaseLavorazione { get; set; }

        public int IDFaseLavorazione { get; set; }
        public int IDProceduraLavorazione { get; set; }
        public int IDCentro { get; set; }

        public bool LavorazioneInRichiestaSingola { get; set; } = false;

        public bool RicercaRaccomandata { get; set; }
        public bool RicercaSLA { get; set; }

        public int J_sla { get; set; }
        public string sla { get; set; }

        public DateTime StartDataLavorazione { get; set; }

        public DateTime? EndDataLavorazione { get; set; }

        //flag che rivela se la lavorazione è stata implementata per il centro specifico
        public bool LavorazioneImplementataByCentro { get; set; } = false;

        //ritorna lo stato in base alla proprietà LavorazioneImplementataByCentro
        public string _statoLavorazione;
        public string StatoLavorazione
        {
            get => _statoLavorazione;
            init => _statoLavorazione = LavorazioneImplementataByCentro == true
                ? "Lavorazione implementata"
                : "Lavorazione non ancora implementata, contattare Edp.Verona@postel.it";

            //get
            //{
            //    return _statoLavorazione;
            //}
            //set
            //{
            //    if (LavorazioneImplementataByCentro == true)
            //    {
            //        _statoLavorazione = "Lavorazione implementata";
            //    }
            //    else
            //    {
            //        _statoLavorazione = "Lavorazione non ancora implementata, contattare Edp.Verona@postel.it";
                    
            //    }
                
            //}
        }


        public List<Operatori> ElencoOperatoriTotale { get; set; }


        /// <summary>
        /// Read dati produzione recuperati dal repository specifico
        /// </summary>
        /// <returns></returns>
        public virtual DataTable GetDatiDemat()
        {
            TableData = new DataTable();
            return TableData;
        }



        public virtual DataTable GetTableCsv(DateTime dataLavorazione) 
        {
            TableData = new DataTable();
            return TableData;

        }

        public virtual DataTable GetTableCsv()
        {
            TableData = new DataTable();
            return TableData;

        }


        public bool EsitoLetturaDato { get; set; }

        public bool Error { get; set; }

        private string descrizioneEsito; 

        public string DescrizioneEsito
        {

            get
            {
                if(Error == true)
                {
                    EsitoLetturaDato = false;
                    descrizioneEsito = TipiLetturaDati.ErroreNelCaricamentoDati + ErrorMessage;
                }
                else
                {
                    if (EsitoLetturaDato == true)
                    {
                        if (TableData.Rows.Count == 0)
                        {
                            descrizioneEsito = TipiLetturaDati.NessunDatoDisponibile;
                        }
                        else
                        {
                            descrizioneEsito = TipiLetturaDati.DatiDisponibili;
                        }
                    }

                    if (LavorazioneImplementataByCentro == false)
                    {
                        EsitoLetturaDato = false;
                        descrizioneEsito = TipiLetturaDati.LavorazioneNonImplementata;
                    }
                }

                return descrizioneEsito;

            }  
        }

        /// <summary>
        /// Tabella lettura dati
        /// </summary>
        public DataTable TableData { get; set; }

        public string ErrorMessage { get; set; }



    }
}
