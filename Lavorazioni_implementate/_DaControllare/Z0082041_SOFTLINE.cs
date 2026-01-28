using System;
using System.Data;
using System.Data.OleDb;
using NLog;
using LibraryUtility;
using Oracle.ManagedDataAccess.Client;
using System.Linq;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0082041_SOFTLINE")]
    public class Z0082041_SOFTLINE : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0082041_SOFTLINE(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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


        //OLD
        //public override DataTable GetDatiDemat()
        //{
        //    switch (this.IDCentro)
        //    {
        //        //Verona
        //        case 1:
        //            LavorazioneImplementataByCentro = true;
        //            FillTableVerona();
        //            break;
        //        //Genova
        //        case 2:
        //            LavorazioneImplementataByCentro = false;                    
        //            break;
        //        default:
        //            LavorazioneImplementataByCentro = false;
        //            break;
        //    }

        //    return this.TableData;

        //}

        private DataTable FillTable()
        {
            this.TableData = new DataTable("Z0082041_SOFTLINE");

            try
            {
                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                //Scansione
                if (IDFaseLavorazione == 4)
                {

                    string query = @"select t.Operatore, t.DataLavorazione,  SUM(NVL(t.Totali,0)) as Documenti, SUM(NVL(t.Totali,0)) as Fogli, SUM(NVL(t.Totali*2,0)) as Pagine
                                    from (
                                      select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041AR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAN0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PCG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PMR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RAR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPCG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPMR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                    ) t
                                    GROUP By t.Operatore,t.DataLavorazione";

                    OracleDataAdapter adapter = new OracleDataAdapter(query, _lavorazioniConfigManager.CnxnPdP);

                    using (OracleConnection con = new OracleConnection(_lavorazioniConfigManager.CnxnPdP))
                    {
                        using (OracleCommand cmd = con.CreateCommand())
                        {
                            con.Open();
                            cmd.BindByName = true;

                            cmd.CommandText = query;
                            cmd.Parameters.Add("startDataScan", startDataScan);
                            cmd.Parameters.Add("endDataScan", endDataScan);

                            var table = new DataTable();

                            adapter.SelectCommand = cmd;
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
                                                     Documenti = grp.Sum(r => r.Field<decimal>("Documenti")),
                                                     Fogli = grp.Sum(r => r.Field<decimal>("Fogli")),
                                                     Pagine = grp.Sum(r => r.Field<decimal>("Pagine"))
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
                }

                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select t.Operatore, t.DataLavorazione, SUM(NVL(t.Totali,0)) as Documenti, SUM(NVL(t.Totali,0)) as Fogli, SUM(NVL(t.Totali*2,0)) as Pagine
                                    from (
                                        select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041AR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAN0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PCG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PMR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RAR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPCG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPMR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                    ) t
                                    GROUP By t.Operatore,t.DataLavorazione";


                    OracleDataAdapter adapter = new OracleDataAdapter(query, _lavorazioniConfigManager.CnxnPdP);

                    using (OracleConnection con = new OracleConnection(_lavorazioniConfigManager.CnxnPdP))
                    {
                        using (OracleCommand cmd = con.CreateCommand())
                        {
                            con.Open();
                            cmd.BindByName = true;

                            cmd.CommandText = query;
                            cmd.Parameters.Add("startDataDE", startDataDE);
                            cmd.Parameters.Add("endDataDE", endDataDE);

                            var table = new DataTable();
                            adapter.SelectCommand = cmd;
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
                                                     Documenti = grp.Sum(r => r.Field<decimal>("Documenti")),
                                                     Fogli = grp.Sum(r => r.Field<decimal>("Fogli")),
                                                     Pagine = grp.Sum(r => r.Field<decimal>("Pagine"))
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


        //OLD
        private DataTable FillTableVerona()
        {
            this.TableData = new DataTable("Z0082041_SOFTLINE");

            try
            {

                //OleDbCommand command;
                //OleDbDataAdapter adapter = new OleDbDataAdapter();

                var startDataScan = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataScan = EndDataLavorazione == null ? startDataScan : EndDataLavorazione.Value.ToString("yyyyMMdd");

                var startDataDE = StartDataLavorazione.ToString("yyyyMMdd");
                var endDataDE = EndDataLavorazione == null ? startDataDE : EndDataLavorazione.Value.ToString("yyyyMMdd");


                //Scansione
                if (IDFaseLavorazione == 4)
                {

                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti,Sum(t.Totali) as Fogli, Sum(t.Totali)*2 as Pagine
                                    from (
                                      select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041AR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAN0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PCG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PMR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RAR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPCG0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                      union all
                                      select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                      LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPMR0_DOC prod 
                                      where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                      group by lo.operator_name, TRUNC(data_aggiornamento)
                                    ) t
                                    GROUP By t.Operatore,t.DataLavorazione";


                    OracleDataAdapter adapter = new OracleDataAdapter(query, _lavorazioniConfigManager.CnxnPdP);

                    using (OracleConnection con = new OracleConnection(_lavorazioniConfigManager.CnxnPdP))
                    {
                        using (OracleCommand cmd = con.CreateCommand())
                        {
                            con.Open();
                            cmd.BindByName = true;

                            cmd.CommandText = query;
                            cmd.Parameters.Add("startDataScan", startDataScan);
                            cmd.Parameters.Add("endDataScan", endDataScan);

                            adapter.SelectCommand = cmd;
                            adapter.Fill(this.TableData);

                        }
                    }

                    //using (OleDbConnection connection = new OleDbConnection(_lavorazioniConfigManager.CnxnPdP))
                    //{
                    //    if (connection.State != ConnectionState.Open)
                    //    {
                    //        connection.Open();
                    //    }


                    //    command = new OleDbCommand(query, connection);
                    //    command.CommandTimeout = 0;
                    //    command.Parameters.Clear();
                    //    command.Parameters.AddWithValue("@startDataScan", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan1", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan1", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan2", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan2", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan3", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan3", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan4", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan4", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan5", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan5", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan6", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan6", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataScan7", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataScan7", endDataScan);
                    //    adapter.SelectCommand = command;
                    //    adapter.Fill(tableData);

                    //}
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti,Sum(t.Totali) as Fogli, Sum(t.Totali)*2 as Pagine
                                    from (
                                        select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041AR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041CAN0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PCG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041PMR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RAR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPCG0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0082041_Z82041RPMR0_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDE,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                    ) t
                                    GROUP By t.Operatore,t.DataLavorazione";


                    OracleDataAdapter adapter = new OracleDataAdapter(query, _lavorazioniConfigManager.CnxnPdP);

                    using (OracleConnection con = new OracleConnection(_lavorazioniConfigManager.CnxnPdP))
                    {
                        using (OracleCommand cmd = con.CreateCommand())
                        {
                            con.Open();
                            cmd.BindByName = true;

                            cmd.CommandText = query;
                            cmd.Parameters.Add("startDataDE", startDataDE);
                            cmd.Parameters.Add("endDataDE", endDataDE);

                            adapter.SelectCommand = cmd;
                            adapter.Fill(this.TableData);

                        }
                    }

                    //using (OleDbConnection connection = new OleDbConnection(_lavorazioniConfigManager.CnxnPdP))
                    //{
                    //    if (connection.State != ConnectionState.Open)
                    //    {
                    //        connection.Open();
                    //    }

                    //    command = new OleDbCommand(query, connection);
                    //    command.CommandTimeout = 0;
                    //    command.Parameters.Clear();
                    //    command.Parameters.AddWithValue("@startDataDe", startDataDE);
                    //    command.Parameters.AddWithValue("@endDataDe", endDataDE);
                    //    command.Parameters.AddWithValue("@startDataDe1", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe1", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe2", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe2", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe3", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe3", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe4", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe4", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe5", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe5", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe6", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe6", endDataScan);
                    //    command.Parameters.AddWithValue("@startDataDe7", startDataScan);
                    //    command.Parameters.AddWithValue("@endDataDe7", endDataScan);

                    //    adapter.SelectCommand = command;
                    //    adapter.Fill(tableData);

                    //}
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




