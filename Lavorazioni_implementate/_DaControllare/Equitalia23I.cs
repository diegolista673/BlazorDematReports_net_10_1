using System;
using System.Data;
using System.Linq;
using NLog;
using LibraryUtility;


namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Equitalia23I")]
    public class Equitalia23I : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Equitalia23I(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            switch (this.IDCentro)
            {
                //Verona
                case 1:
                    LavorazioneImplementataByCentro = true;
                    FillTable();
                    break;

                //Genova
                case 2:
                    LavorazioneImplementataByCentro = true;
                    FillTable();
                    break;

                default:
                    LavorazioneImplementataByCentro = false;
                    break;
            }

            return this.TableData;

        }
 


        //filtrare gli operatori per il sito di appartenenza tramite elenco operatori passato alla lavorazione
        private DataTable FillTable()
        {
            this.TableData = new DataTable("Equitalia23I");

            try
            {
                
                WebTopSearch wts = new WebTopSearch();
                wts.UrlLavorazione = _lavorazioniConfigManager.UrlWebtopEquitalia23I;
                wts.User = _lavorazioniConfigManager.UserWebtopEquitalia23I;
                wts.Password = _lavorazioniConfigManager.PasswordWebtopEquitalia23I;
                wts.DataLavorazione = StartDataLavorazione;
                wts.NomeLavorazione = NomeProcedura;

                var table = wts.FillTable();

                if (table.Rows.Count > 0)
                {

                    //group by utente
                    var newGrouped = from row in table.AsEnumerable()
                                     group row by new
                                     {
                                         Operatore = row.Field<string>("Utente"),
                                     } into grp
                                     select new
                                     {
                                         Operatore = grp.Key.Operatore,
                                         Documenti = grp.Where(r => int.TryParse(r.Field<string>("Numero documenti"), out int dummy))
                                                         .Sum(r => int.Parse(r.Field<string>("Numero documenti").Trim())),
                                         Fogli = grp.Where(r => int.TryParse(r.Field<string>("Numero documenti"), out int dummy))
                                                         .Sum(r => int.Parse(r.Field<string>("Numero documenti").Trim())),
                                         Pagine = grp.Where(r => int.TryParse(r.Field<string>("Numero documenti"), out int dummy))
                                                         .Sum(r => int.Parse(r.Field<string>("Numero documenti").Trim())) * 2

                                     };

                    //Crea la Tabella finale
                    this.TableData.Columns.Add("Operatore", typeof(string));
                    this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    this.TableData.Columns.Add("Documenti", typeof(int));
                    this.TableData.Columns.Add("Fogli", typeof(int));
                    this.TableData.Columns.Add("Pagine", typeof(int));

                    foreach (var row in newGrouped)
                    {
                        var operatorName = row.Operatore.Replace(" ", ".").ToLower();
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, StartDataLavorazione.Date, row.Documenti, row.Fogli, row.Pagine);
                        }

                    }

                }

                EsitoLetturaDato = true;
                return this.TableData;
            }
            catch (Exception ex)
            {
                EsitoLetturaDato = false;
                Error = true;
                ErrorMessage = ex.Message;
                logger.Error(ex.Message);

                if (this.LavorazioneInRichiestaSingola == true)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    return TableData;
                }
            }


        }




    }
}
