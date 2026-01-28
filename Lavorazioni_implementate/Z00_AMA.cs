using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;



namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z00_AMA")]
    public class Z00_AMA : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z00_AMA(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override System.Data.DataTable GetDatiDemat()
        {
            LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }


        //old
        //private DataTable FillTableVerona()
        //{
        //    this.TableData = new DataTable("Z00_AMA");

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
        //                            from (
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'esiti' as Tipologia 
        //                                FROM AMA_RAC_DATI
        //                                WHERE scatola LIKE '%AMA_ND_E%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                                union all
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
        //                                FROM AMA_RAC_DATI
        //                                WHERE scatola LIKE '%AMA_ND_I%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                                union all
        //                                SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
        //                                FROM Z0021572_MAS_DATI
        //                                WHERE scatola LIKE '%Z0021572_M%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
        //                                GROUP By op_index, CONVERT(date, data_index)
        //                            ) as t
        //                            GROUP By t.Operatore,t.DataLavorazione
        //                            order by t.DataLavorazione";


        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }

        //                command = new SqlCommand(query, connection);
        //                command.CommandTimeout = 0;
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
            this.TableData = new DataTable("Z00_AMA");

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
                                        FROM AMA_RAC_DATI
                                        WHERE scatola LIKE '%AMA_ND_E%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        union all
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
                                        FROM AMA_RAC_DATI
                                        WHERE scatola LIKE '%AMA_ND_I%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
                                        GROUP By op_index, CONVERT(date, data_index)
                                        union all
                                        SELECT op_index as Operatore,CONVERT(date, data_index) as DataLavorazione, Count (distinct(CAMPO_1 )) as Totali,'inesiti' as Tipologia 
                                        FROM Z0021572_MAS_DATI
                                        WHERE scatola LIKE '%Z0021572_M%' and CONVERT(date, data_index) >= @startDataDe and CONVERT(date, data_index) <= @endDataDe
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
                    //query errata nei riferimaenti tra tabelle
                    //string query = @"select SCT_OPERATORE_SCAN as Operatore, CONVERT(DATE, SCT_DATA_INIZIO_SCAN) as DataLavorazione, SUM(convert(int,isnull(sct_num_doc,0))) as Documenti, SUM(convert(int,isnull(sct_num_doc,0))) as Fogli, SUM(convert(int,isnull(sct_num_doc,0)))*2 as Pagine, SUM(convert(int,isnull(SCT_NUM_SCARTI,0))) as Scarti
                    //                from btc_batch 
                    //                where CONVERT(DATE, SCT_DATA_INIZIO_SCAN) >= @startDataScan and CONVERT(DATE, SCT_DATA_INIZIO_SCAN) <= @endDataScan and 
                    //                btc_sct_id in (
                    //                    select SCT_ID_SCATOLA from sct_scatole where sct_prc_id_zprocedura in( 
                    //                        select prc_id_zprocedura from prc_procedure where prc_zprocedura like '%AMA_ND_I%' or prc_zprocedura like '%AMA_ND_E%' or prc_zprocedura like '%Z0021572_M%'
                    //                    ) 
                    //                )
                    //                GROUP By SCT_OPERATORE_SCAN,CONVERT(DATE, SCT_DATA_INIZIO_SCAN)
                    //                order by CONVERT(DATE, SCT_DATA_INIZIO_SCAN)";


                    string query = @"select SCT_OPERATORE_SCAN  as Operatore, convert(date, SCT_DATA_FINE_SCAN) as DataLavorazione,SUM(convert(int,isnull(SCT_NUM_DOC,0))) as Documenti, SUM(convert(int,isnull(SCT_NUM_DOC,0))) AS Fogli,SUM(convert(int,isnull(SCT_NUM_DOC*2,0))) AS Pagine
                                    from BTC_BATCH 
                                    where BTC_NOME_BATCH like 'AMA_RACCOMANDATE_%' and convert(date, SCT_DATA_FINE_SCAN) >= @startDataScan1 and convert(date, SCT_DATA_FINE_SCAN) <= @endDataScan1
                                    group by SCT_OPERATORE_SCAN, convert(date, SCT_DATA_FINE_SCAN)
                                    union all
                                    select CPT_OPERATORE_SCAN  as Operatore, convert(date, CPT_DATA_SCAN) as DataLavorazione,SUM(convert(int,isnull(CPT_NUM_DOC,0))) as Documenti, SUM(convert(int,isnull(CPT_NUM_DOC,0))) AS Fogli,SUM(convert(int,isnull(CPT_NUM_DOC*2,0))) AS Pagine
                                    from CPT_BATCH 
                                    where CPT_BATCH_NAME like 'AMA_RACCOMANDATE_%' and convert(date, CPT_DATA_SCAN) >= @startDataScan2 and convert(date, CPT_DATA_SCAN) <= @endDataScan2
                                    group by CPT_OPERATORE_SCAN, convert(date, CPT_DATA_SCAN)
                                    order by DataLavorazione";


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
                        command.Parameters.AddWithValue("@startDataScan1", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan1", endDataScan);
                        command.Parameters.AddWithValue("@startDataScan2", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan2", endDataScan);
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




