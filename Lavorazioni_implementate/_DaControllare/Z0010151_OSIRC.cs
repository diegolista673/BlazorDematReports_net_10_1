using System;
using System.Data;
using System.Data.SqlClient;
using NLog;
using LibraryUtility;
using System.Text.RegularExpressions;
using System.Linq;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0010151_OSIRC")]
    public class Z0010151_OSIRC : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public Z0010151_OSIRC(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.Web.NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }

        //old
        //private DataTable FillTableVerona()
        //{
        //    this.TableData = new DataTable("Z0010151_OSIRC");

        //    try
        //    {
        //        SqlCommand command;
        //        SqlDataAdapter adapter = new SqlDataAdapter();

        //        var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
        //        var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

        //        var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
        //        var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


        //        //Data Entry
        //        if (IDFaseLavorazione == 5)
        //        {
        //            string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti, Sum(t.Totali) as Fogli, Sum(t.Totali)*2 as Pagine
        //                             from (
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'esiti' as Tipologia 
        //                                FROM Z0010151_RAC_DATI
        //                                WHERE scatola LIKE '%Z0010151_E%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                                union all
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
        //                                FROM Z0010151_RAC_DATI
        //                                WHERE scatola LIKE '%Z0010151_I%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                                union all
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
        //                                FROM Z0010151_RAC_DATI
        //                                WHERE scatola LIKE '%Z0010151_M%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                                ) as t
        //                             GROUP By t.Operatore,t.DataLavorazione
        //                             order by t.DataLavorazione";


        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }

        //                command = new SqlCommand(query, connection);
        //                command.Parameters.Clear();
        //                command.Parameters.AddWithValue("@startDataDe", startDataDE);
        //                command.Parameters.AddWithValue("@endDataDe", endDataDE);
        //                adapter.SelectCommand = command;
        //                adapter.Fill(this.TableData);
        //            }
        //        }

        //        EsitoLetturaDato = true;
        //        return this.TableData;
        //    }
        //    catch (Exception ex)
        //    {
        //        EsitoLetturaDato = false;
        //        Error = true;
        //        ErrorMessage = ex.Message;
        //        logger.Error(ex.Message);

        //        if (this.LavorazioneInRichiestaSingola == true)
        //        {
        //            throw new Exception(ex.Message);
        //        }
        //        else
        //        {
        //            return TableData;
        //        }
        //    }
        //}

        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0010151_OSIRC");

            try
            {
                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti, Sum(t.Totali) as Fogli, Sum(t.Totali)*2 as Pagine
                                     from (
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'esiti' as Tipologia 
                                        FROM Z0010151_RAC_DATI
                                        WHERE scatola LIKE '%Z0010151_E%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        union all
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
                                        FROM Z0010151_RAC_DATI
                                        WHERE scatola LIKE '%Z0010151_I%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        union all
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
                                        FROM Z0010151_RAC_DATI
                                        WHERE scatola LIKE '%Z0010151_M%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        ) as t
                                     GROUP By t.Operatore,t.DataLavorazione
                                     order by t.DataLavorazione";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        var table = new DataTable();
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDe", startDataDE);
                        command.Parameters.AddWithValue("@endDataDe", endDataDE);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);

                        if (table.Rows.Count > 0)
                        {
                            //group by utente
                            var newGrouped = from row in table.AsEnumerable()
                                             group row by new
                                             {
                                                 Operatore = row.Field<string>("Operatore"),
                                                 DataLavorazione = row.Field<DateTime>("DataLavorazione")
                                             } into grp
                                             select new
                                             {
                                                 Operatore = grp.Key.Operatore,
                                                 DataLavorazione = grp.Key.DataLavorazione,
                                                 Documenti = grp.Sum(r => r.Field<int>("Documenti")),
                                                 Fogli = grp.Sum(r => r.Field<int>("Fogli")),
                                                 Pagine = grp.Sum(r => r.Field<int>("Pagine"))
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
                                string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim().ToLower();
                                var operatorName = oper.Replace(" ", ".").ToLower();
                                operatorName = oper.Replace(@"postel\", "");
                                operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                                if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == this.IDCentro))
                                {
                                    this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine);
                                }
                            }
                        }

                    }
                }

                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    string query = @"select CPT_OPERATORE_SCAN as Operatore,CONVERT(DATE, CPT_DATA_SCAN) as DataLavorazione,SUM(convert(int,isnull(CPT_NUM_DOC,0))) as Documenti, SUM(convert(int,isnull(CPT_NUM_DOC,0))) as Fogli, SUM(convert(int,isnull(CPT_NUM_DOC,0)))*2 as Pagine, SUM(convert(int,isnull(CPT_NUM_SCARTI,0))) as Scarti 
                                    from CPT_BATCH 
                                    where  CONVERT(date, CPT_DATA_SCAN) >= @startDataScan and CONVERT(date, CPT_DATA_SCAN) <= @endDataScan and ( CPT_BATCH_NAME like 'Z0010151_OSIRC_E%' or CPT_BATCH_NAME like 'Z0010151_OSIRC_I%' or CPT_BATCH_NAME like 'Z0010151_OSIRC_M%' )
                                    GROUP By CPT_OPERATORE_SCAN,CONVERT(DATE, CPT_DATA_SCAN)
                                    order by CONVERT(DATE, CPT_DATA_SCAN)";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        var table = new DataTable();
                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);

                        if (table.Rows.Count > 0)
                        {
                            //group by utente
                            var newGrouped = from row in table.AsEnumerable()
                                             group row by new
                                             {
                                                 Operatore = row.Field<string>("Operatore"),
                                                 DataLavorazione = row.Field<DateTime>("DataLavorazione")
                                             } into grp
                                             select new
                                             {
                                                 Operatore = grp.Key.Operatore,
                                                 DataLavorazione = grp.Key.DataLavorazione,
                                                 Documenti = grp.Sum(r => r.Field<int>("Documenti")),
                                                 Fogli = grp.Sum(r => r.Field<int>("Fogli")),
                                                 Pagine = grp.Sum(r => r.Field<int>("Pagine")),
                                                 Scarti = grp.Sum(r => r.Field<int>("Scarti"))
                                             };

                            //Crea la Tabella finale
                            this.TableData.Columns.Add("Operatore", typeof(string));
                            this.TableData.Columns.Add("DataLavorazione", typeof(DateTime));
                            this.TableData.Columns.Add("Documenti", typeof(int));
                            this.TableData.Columns.Add("Fogli", typeof(int));
                            this.TableData.Columns.Add("Pagine", typeof(int));
                            this.TableData.Columns.Add("Scarti", typeof(int));


                            foreach (var row in newGrouped)
                            {
                                //Since it will catch runs of any kind of whitespace(e.g.tabs, newlines, etc.) and replace them with a single space.
                                string oper = Regex.Replace(row.Operatore, @"\s+", " ").Trim().ToLower();
                                var operatorName = oper.Replace(" ", ".").ToLower();
                                operatorName = oper.Replace(@"postel\", "");
                                operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                                if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName && x.Idcentro == this.IDCentro))
                                {
                                    this.TableData.Rows.Add(operatorName, row.DataLavorazione, row.Documenti, row.Fogli, row.Pagine, row.Scarti);
                                }
                            }
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




