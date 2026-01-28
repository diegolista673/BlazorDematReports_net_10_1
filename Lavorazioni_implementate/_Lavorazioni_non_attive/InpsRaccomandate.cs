using LibraryUtility;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("InpsRaccomandate")]
    public class InpsRaccomandate : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public InpsRaccomandate(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            LavorazioneImplementataByCentro = true;
            FillTable(this.IDCentro);
            return this.TableData;

        }


        private DataTable FillTable(int centro)
        {

            this.TableData = new DataTable("InpsRaccomandate");

            try
            {
                WebTopSearch wts = new WebTopSearch();
                wts.UrlLavorazione = _lavorazioniConfigManager.UrlWebtopInps;

                if (centro == 1)
                {
                    wts.User = _lavorazioniConfigManager.UserWebtopInps;
                    wts.Password = _lavorazioniConfigManager.PasswordWebtopInps;
                }

                if (centro == 2)
                {
                    wts.User = _lavorazioniConfigManager.UserWebtopInpsGenova;
                    wts.Password = _lavorazioniConfigManager.PasswordWebtopInpsGenova;
                }

                if (centro == 3)
                {
                    wts.User = _lavorazioniConfigManager.UserWebtopInpsPomezia;
                    wts.Password = _lavorazioniConfigManager.PasswordWebtopInpsPomezia;
                }

                if (centro == 4)
                {
                    wts.User = _lavorazioniConfigManager.UserWebtopInpsMelzo;
                    wts.Password = _lavorazioniConfigManager.PasswordWebtopInpsMelzo;
                }


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
                        //Since it will catch runs of any kind of whitespace(e.g.tabs, newlines, etc.) and replace them with a single space.
                        string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim();
                        var operatorName = oper.Replace(" ", ".").ToLower();

                        //var operatorName = row.Operatore.Replace(" ", ".").ToLower();
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
                base.ErrorMessage = ex.Message;
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
