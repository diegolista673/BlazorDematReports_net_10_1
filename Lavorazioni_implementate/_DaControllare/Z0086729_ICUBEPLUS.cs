using System;
using System.Data;
using System.Data.OleDb;
using NLog;
using LibraryUtility;
using Oracle.ManagedDataAccess.Client;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("Z0086729_ICUBEPLUS")]
    public class Z0086729_ICUBEPLUS : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;

        public Z0086729_ICUBEPLUS(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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
                    FillTableVerona();
                    break;
                //Genova
                case 2:
                    LavorazioneImplementataByCentro = false;                    
                    break;
                default:
                    LavorazioneImplementataByCentro = false;
                    break;
            }

            return this.TableData;

        }



        private DataTable FillTableVerona()
        {
            this.TableData = new DataTable("Z0086729_ICUBEPLUS");

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

                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti
                                    from (
                                        select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0086729_Z86729RCAR1_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 100 and TRUNC(data_aggiornamento) >= to_date(:startDataScan,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataScan,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0086729_Z86729RCAR2_DOC prod 
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
                    //    adapter.SelectCommand = command;
                    //    adapter.Fill(tableData);

                    //}
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"select t.Operatore, t.DataLavorazione, Sum(t.Totali) as Documenti
                                    from (
                                        select lo.operator_name as Operatore, TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0086729_Z86729RCAR1_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDe,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
                                        group by lo.operator_name, TRUNC(data_aggiornamento)
                                        union all
                                        select lo.operator_name as Operatore,  TRUNC(data_aggiornamento) as DataLavorazione, COUNT(distinct prod.document_id) AS Totali FROM 
                                        LD_OBJECT_STATUS los, LS_OPERATORI lo, LD_Z0086729_Z86729RCAR2_DOC prod 
                                        where los.OPERATORE_ID = lo.OPERATORE_ID and los.OBJECT_ID = prod.DOCUMENT_ID And los.OBJECT_TYPE_ID = 5 and los.object_status_type_id = 200 and TRUNC(data_aggiornamento) >= to_date(:startDataDe,'yyyymmdd') and TRUNC(data_aggiornamento) <= to_date(:endDataDE,'yyyymmdd')
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
                            cmd.Parameters.Add("startDataDe", startDataDE);
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
                    //    command.Parameters.AddWithValue("@startDataDe1", startDataDE);
                    //    command.Parameters.AddWithValue("@endDataDe1", endDataDE);
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




