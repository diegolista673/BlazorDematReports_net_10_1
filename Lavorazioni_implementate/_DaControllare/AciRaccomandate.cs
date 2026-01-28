using System;
using System.Data;
using System.Linq;
using NLog;
using LibraryUtility;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("AciRaccomandate")]
    public class AciRaccomandate : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public AciRaccomandate(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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

            this.TableData = new DataTable("AciRaccomandate");

            try
            {
                WebTopSearch wts = new WebTopSearch();
                wts.UrlLavorazione = _lavorazioniConfigManager.UrlWebtopAciRaccomandate;
                wts.User = _lavorazioniConfigManager.UserWebtopAciRaccomandate;
                wts.Password = _lavorazioniConfigManager.PasswordWebtopAciRaccomandate;
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
                                                         .Sum(r => int.Parse(r.Field<string>("Numero documenti").Trim()))

                                     };

                    //Crea la Tabella finale
                    this.TableData.Columns.Add("Operatore", typeof(string));
                    this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                    this.TableData.Columns.Add("Documenti", typeof(int));

                    foreach (var row in newGrouped)
                    {


                        //Since it will catch runs of any kind of whitespace(e.g.tabs, newlines, etc.) and replace them with a single space.
                        string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim();
                        var operatorName = oper.Replace(" ", ".").ToLower();

                        //var operatorName = row.Operatore.Replace(" ", ".").ToLower();
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);


                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == IDCentro))
                        {
                            this.TableData.Rows.Add(operatorName, StartDataLavorazione.Date, row.Documenti);
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
