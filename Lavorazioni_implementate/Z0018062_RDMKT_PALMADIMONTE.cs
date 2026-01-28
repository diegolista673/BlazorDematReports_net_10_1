using LibraryUtility;
using Microsoft.Data.SqlClient;
using NLog;
using System.Data;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{
    [ProcessingLavorazioneAttribute("Z0018062_RDMKT_PALMADIMONTE")]
    public class Z0018062_RDMKT_PALMADIMONTE : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0018062_RDMKT_PALMADIMONTE(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
        {
            logger = NLog.LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            _lavorazioniConfigManager = lavorazioniConfigManager;
            _normalizzaOperatore = normalizzaOperatore;
        }


        public override DataTable GetDatiDemat()
        {
            LavorazioneImplementataByCentro = true;
            FillTable();
            return this.TableData;
        }

        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0018062_RDMKT_PALMADIMONTE");
            DataTable table = new DataTable();

            try
            {

                SqlCommand command;
                SqlDataAdapter adapter = new SqlDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");

                //Scansione
                if (IDFaseLavorazione == 4)
                {
                    table.Clear();

                    string query = @"select OP_SCAN as operatore, convert(date, DATA_SCAN) as DataLavorazione, COUNT(*) as Documenti,isnull(SUM(convert(int,num_pag))/2,0) as Fogli, isnull(SUM(convert(int,num_pag)),0) as Pagine
                                     from Z0018062_RDMKT_PALMADIMONTE_PE_UDA
                                     where convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
                                     group by OP_SCAN,convert(date, DATA_SCAN)
                                     order by DataLavorazione";

                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }

                        command = new SqlCommand(query, connection);
                        command.CommandTimeout = 0;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        adapter.SelectCommand = command;
                        adapter.Fill(table);

                    }

                }


                //Data Entry automatico

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

        //old
        //private DataTable FillTable()
        //{
        //    this.TableData = new DataTable("Z0014933_TELETU");

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

        //            string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti, Sum(Fogli) as Fogli, Sum(Pagine) as Pagine
        //                            from (
        //                            SELECT op_index as Operatore,convert(date, data_index) as DataLavorazione, Count (*) as Totali, 'raccomandate' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_RACC
        //                            WHERE convert(date, data_index) >= @startDataDe and convert(date, data_index) <= @endDataDe
        //                            GROUP By op_index, convert(date, data_index)
        //                            union all
        //                            SELECT op_index as Operatore,convert(date, data_index) as DataLavorazione,Count (*) as Totali, 'DBU_BARCODE' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_DBU
        //                            WHERE convert(date, data_index) >= @startDataDe and convert(date, data_index) <= @endDataDe AND barcode = 1
        //                            GROUP By op_index, convert(date, data_index)
        //                            union all
        //                            SELECT op_index as Operatore,convert(date, data_index) as DataLavorazione,Count (*) as Totali, 'DBU_SENZA_BARCODE' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_DBU
        //                            WHERE convert(date, data_index) >= @startDataDe and convert(date, data_index) <= @endDataDe AND barcode = 0
        //                            GROUP By op_index, convert(date, data_index)
        //                            ) as t
        //                            GROUP By t.Operatore,t.DataLavorazione";


        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }

        //                var table = new DataTable();
        //                command = new SqlCommand(query, connection);
        //                command.CommandTimeout = 0;
        //                command.Parameters.Clear();
        //                command.Parameters.AddWithValue("@startDataDe", startDataDE);
        //                command.Parameters.AddWithValue("@endDataDe", endDataDE);
        //                adapter.SelectCommand = command;
        //                adapter.Fill(table);

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

        //            }
        //        }

        //        //Scansione
        //        if (IDFaseLavorazione == 4)
        //        {
        //            string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti, Sum(Fogli) as Fogli, Sum(Pagine) as Pagine
        //                            from (
        //                            SELECT OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, Count (*) as Totali, 'raccomandate' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_RACC 
        //                            WHERE convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
        //                            GROUP By OP_SCAN, convert(date, DATA_SCAN)
        //                            union all
        //                            SELECT OP_SCAN as Operatore, convert(date, DATA_SCAN) as DataLavorazione, Count (*) as Totali, 'ordinario' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_ORD 
        //                            WHERE convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan
        //                            GROUP By OP_SCAN, convert(date, DATA_SCAN)
        //                            union all 
        //                            SELECT OP_SCAN as Operatore,convert(date, DATA_SCAN) as DataLavorazione, Count (*) as Totali, 'DBU_BARCODE' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine
        //                            FROM Z0014933_TELETU_DBU
        //                            WHERE convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan AND barcode = 1 
        //                            GROUP By OP_SCAN, convert(date, DATA_SCAN) 
        //                            union all
        //                            SELECT OP_SCAN as Operatore, convert(date, DATA_SCAN) as DataLavorazione, Count (*) as Totali, 'DBU_SENZA_BARCODE' as Tipologia, Sum(CONVERT(INT, numero_pagine)) / 2 as Fogli, Sum(CONVERT(INT, numero_pagine)) as Pagine 
        //                            FROM Z0014933_TELETU_DBU 
        //                            WHERE convert(date, DATA_SCAN) >= @startDataScan and convert(date, DATA_SCAN) <= @endDataScan AND barcode = 0 
        //                            GROUP By OP_SCAN, convert(date, DATA_SCAN) 
        //                            ) as t
        //                            GROUP By t.Operatore,t.DataLavorazione";


        //            using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnCaptiva206))
        //            {
        //                if (connection.State != ConnectionState.Open)
        //                {
        //                    connection.Open();
        //                }

        //                var table = new DataTable();
        //                command = new SqlCommand(query, connection);
        //                command.CommandTimeout = 0;
        //                command.Parameters.Clear();
        //                command.Parameters.AddWithValue("@startDataScan", startDataScan);
        //                command.Parameters.AddWithValue("@endDataScan", endDataScan);
        //                adapter.SelectCommand = command;
        //                adapter.Fill(table);

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


    }
}




