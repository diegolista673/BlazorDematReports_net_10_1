using NLog;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
//    [ProcessingLavorazioneAttribute("Hera")]
//    public class Hera : Lavorazione
//    {
//        private readonly Logger logger;
//        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
//        private readonly NormalizzaOperatore _normalizzaOperatore;

//        public Hera(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
//        {
//            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
//            _lavorazioniConfigManager = lavorazioniConfigManager;
//            _normalizzaOperatore = normalizzaOperatore;
//        }


//        public override DataTable GetDatiDemat()
//        {
//            switch (this.IDCentro)
//            {
//                //Verona
//                case 1:
//                    LavorazioneImplementataByCentro = true;
//                    FillTable();
//                    break;
//                //Genova
//                case 2:
//                    LavorazioneImplementataByCentro = true;
//                    FillTable();
//                    break;

//                default:
//                    LavorazioneImplementataByCentro = false;
//                    break;
//            }

//            return this.TableData;

//        }



//        private DataTable FillTable()
//        {
//            this.TableData = new DataTable("Hera");
//            var table = new DataTable();

//            try
//            {
//                SqlCommand command;
//                SqlDataAdapter adapter = new SqlDataAdapter();

//                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                var startDataClas = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataClas = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                //Scansione
//                if (IDFaseLavorazione == 4)
//                {

//                    string query = @"select operatore_scan as Operatore, convert(date, data_scansione) as DataLavorazione, COUNT(distinct(codice_mercato + codice_offerta + tipo_documento)) as Documenti,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento)) as Fogli,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento))*2 as Pagine
//                                     from HERA16
//                                     where convert(date,data_scansione) >= @startDataScan and convert(date,data_scansione) <= @endDataScan 
//                                     group by operatore_scan,convert(date, data_scansione)
//                                     order by convert(date, data_scansione)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }
//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
//                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(table);
//                    }
//                }


//                //Data Entry
//                if (IDFaseLavorazione == 5)
//                {

//                    string query = @"select operatore_index as Operatore, convert(date, data_index) as DataLavorazione, COUNT(distinct(codice_mercato + codice_offerta + tipo_documento)) as Documenti,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento)) as Fogli,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento))*2 as Pagine
//                                     from HERA16 
//                                     where convert(date,data_index) >= @startDataDe and convert(date,data_index) <= @endDataDe and operatore_index <> '-' and operatore_index <> 'engine' and tipo_documento not in('BRIT','DR01','XXXX')
//                                     group by operatore_index,convert(date, data_index)
//                                     order by convert(date, data_index)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
//                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(table);
//                    }
//                }


//                //Classificazione
//                if (IDFaseLavorazione == 8)
//                {

//                    string query = @"SELECT operatore_classificazione as Operatore,convert(date, data_classificazione) as DataLavorazione, COUNT(distinct(codice_mercato+codice_offerta + tipo_documento)) as Documenti,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento)) as Fogli,COUNT(distinct(codice_mercato+codice_offerta + tipo_documento))*2 as Pagine
//                                    from HERA16 
//                                    WHERE convert(date,data_classificazione) >= @startDataClas and convert(date,data_classificazione) <= @endDataClas and operatore_classificazione <>'-' and operatore_classificazione <> 'engine' 
//                                    group by operatore_classificazione,convert(date, data_classificazione)
//                                    order by convert(date, data_classificazione)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataClas", startDataClas);
//                        command.Parameters.AddWithValue("@endDataClas", endDataClas);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(table);
//                    }
//                }


//                if (table.Rows.Count > 0)
//                {
//                    //group by utente
//                    var newGrouped = from row in table.AsEnumerable()
//                                     group row by new
//                                     {
//                                         Operatore = row.Field<string>("Operatore"),
//                                         DataLavorazione = row.Field<DateTime>("DataLavorazione")
//                                     } into grp
//                                     select new
//                                     {
//                                         Operatore = grp.Key.Operatore,
//                                         DataLavorazione = grp.Key.DataLavorazione,
//                                         Documenti = grp.Sum(r => r.Field<int>("Documenti")),
//                                         Fogli = grp.Sum(r => r.Field<int>("Fogli")),
//                                         Pagine = grp.Sum(r => r.Field<int>("Pagine"))
//                                     };

//                    //Crea la Tabella finale
//                    this.TableData.Columns.Add("Operatore", typeof(string));
//                    this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
//                    this.TableData.Columns.Add("Documenti", typeof(int));
//                    this.TableData.Columns.Add("Fogli", typeof(int));
//                    this.TableData.Columns.Add("Pagine", typeof(int));

//                    foreach (var row in newGrouped)
//                    {
//                        //Since it will catch runs of any kind of whitespace(e.g.tabs, newlines, etc.) and replace them with a single space.
//                        string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim().ToLower();
//                        var operatorName = oper.Replace(" ", ".").ToLower();
//                        operatorName = oper.Replace(@"postel\", "");
//                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

//                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == this.IDCentro))
//                        {
//                            this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine);
//                        }
//                    }
//                }

//                EsitoLetturaDato = true;
//                return TableData;
//            }
//            catch (Exception ex)
//            {
//                EsitoLetturaDato = false;
//                Error = true;
//                ErrorMessage = ex.Message;
//                logger.Error(ex.Message);

//                if (this.LavorazioneInRichiestaSingola == true)
//                {
//                    throw new Exception(ex.Message);
//                }
//                else
//                {
//                    return this.TableData;
//                }
//            }


//        }


//        private DataTable FillTableGenova()
//        {
//            this.TableData = new DataTable("Hera_Genova");

//            try
//            {
//                SqlCommand command;
//                SqlDataAdapter adapter = new SqlDataAdapter();

//                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                var startDataClas = StartDataLavorazione.ToString("yyyyMMdd");
//                var endDataClas = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

//                //Scansione
//                if (IDFaseLavorazione == 4)
//                {

//                    string query = @"select op.OPERATORE as Operatore, convert(date, data_scansione) as DataLavorazione, COUNT(distinct(codice_mercato + codice_offerta + tipo_documento)) as Documenti
//                                     from HERA16 as h
//                                     JOIN ProduzioneGed.dbo.Operatori op ON h.operatore_scan LIKE '%'+ op.operatore +'%'
//                                     where convert(date,data_scansione) >= @startDataScan and convert(date,data_scansione) <= @endDataScan and op.idCentro = @idCentro
//                                     group by operatore_scan,op.OPERATORE,convert(date, data_scansione)
//                                     order by convert(date, data_scansione)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }
//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
//                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(this.TableData);
//                    }
//                }


//                //Data Entry
//                if (IDFaseLavorazione == 5)
//                {

//                    string query = @"select op.OPERATORE as Operatore, convert(date, data_index) as DataLavorazione, COUNT(distinct(codice_mercato + codice_offerta + tipo_documento)) as Documenti
//                                     from HERA16 as h
//                                     JOIN ProduzioneGed.dbo.Operatori op ON h.operatore_index LIKE '%'+ op.operatore +'%'
//                                     where convert(date,data_index) >= @startDataDe and convert(date,data_index) <= @endDataDe and operatore_index <> '-' and operatore_index <> 'engine' and tipo_documento not in('BRIT','DR01','XXXX') and op.IDCentro = @idCentro
//                                     group by operatore_index,op.OPERATORE,convert(date, data_index)
//                                     order by convert(date, data_index)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
//                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(this.TableData);
//                    }
//                }


//                //Classificazione
//                if (IDFaseLavorazione == 8)
//                {

//                    string query = @"SELECT op.OPERATORE as Operatore,convert(date, data_classificazione) as DataLavorazione, COUNT(distinct(codice_mercato+codice_offerta + tipo_documento)) as Documenti
//                                    from HERA16 as h
//                                    JOIN ProduzioneGed.dbo.Operatori op ON h.operatore_classificazione LIKE '%'+ op.operatore +'%'
//                                    WHERE convert(date,data_classificazione) >= @startDataClas and convert(date,data_classificazione) <= @endDataClas and operatore_classificazione <>'-' and operatore_classificazione <> 'engine' and op.idCentro = @idCentro
//                                    group by operatore_classificazione,op.OPERATORE,convert(date, data_classificazione)
//                                    order by convert(date, data_classificazione)";


//                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnHera))
//                    {
//                        if (connection.State != ConnectionState.Open)
//                        {
//                            connection.Open();
//                        }

//                        command = new SqlCommand(query, connection);
//                        command.Parameters.Clear();
//                        command.Parameters.AddWithValue("@startDataClas", startDataClas);
//                        command.Parameters.AddWithValue("@endDataClas", endDataClas);
//                        command.Parameters.AddWithValue("@idCentro", IDCentro);
//                        command.CommandTimeout = 0;
//                        adapter.SelectCommand = command;
//                        adapter.Fill(this.TableData);
//                    }
//                }


//                EsitoLetturaDato = true;
//                return this.TableData;
//            }
//            catch (Exception ex)
//            {
//                EsitoLetturaDato = false;
//                Error = true;
//                ErrorMessage = ex.Message;
//                logger.Error(ex.Message);

//                if (this.LavorazioneInRichiestaSingola == true)
//                {
//                    throw new Exception(ex.Message);
//                }
//                else
//                {
//                    return TableData;
//                }
//            }


//        }

//    }
}




