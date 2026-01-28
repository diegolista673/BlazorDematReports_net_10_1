using System;
using System.Data;
using System.Data.SqlClient;
using DFCLib;
using LibraryUtility;
using System.Linq;
using NLog;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LibraryLavorazioni
{

    [ProcessingLavorazioneAttribute("PraticheSuccessione")]
    public class PraticheSuccessione : Lavorazione
    {
        private readonly Logger logger;
        private readonly ILavorazioniConfigManager _lavorazioniConfigManager;
        private readonly NormalizzaOperatore _normalizzaOperatore;


        public PraticheSuccessione(ILavorazioniConfigManager lavorazioniConfigManager, NormalizzaOperatore normalizzaOperatore)
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


        private DataTable FillTable()
        {
            this.TableData = new DataTable("PraticheSuccessione");

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


                    string query = @"SELECT Operatore, CONVERT(DATETIME, dataLavorazione, 103) as DataLavorazione ,Documenti,Fogli,Pagine
                                     FROM PraticheSuccessione
                                     WHERE convert(date, CONVERT(DATETIME, dataLavorazione, 103)) >= @startDataScan and convert(date, CONVERT(DATETIME, dataLavorazione,103)) <= @endDataScan and IdFaseLavorazione = 4  and IdCentro =  @IdCentro";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnProduzioneGed))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataScan", startDataScan);
                        command.Parameters.AddWithValue("@endDataScan", endDataScan);
                        command.Parameters.AddWithValue("@IdCentro", this.IDCentro);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);
                    }
                }


                //Data Entry
                if (IDFaseLavorazione == 5)
                {

                    string query = @"SELECT Operatore, CONVERT(DATETIME, dataLavorazione, 103) as DataLavorazione ,Documenti,Fogli,Pagine
                                     FROM PraticheSuccessione
                                     WHERE convert(date, CONVERT(DATETIME, dataLavorazione, 103)) >= @startDataDE and convert(date, CONVERT(DATETIME, dataLavorazione,103)) <= @endDataDE and IdFaseLavorazione = 5 and IdCentro =  @IdCentro";


                    using (SqlConnection connection = new SqlConnection(_lavorazioniConfigManager.CnxnProduzioneGed))
                    {
                        if (connection.State != ConnectionState.Open)
                        {
                            connection.Open();
                        }
                        command = new SqlCommand(query, connection);
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@startDataDE", startDataDE);
                        command.Parameters.AddWithValue("@endDataDE", endDataDE);
                        command.Parameters.AddWithValue("@IdCentro", this.IDCentro);
                        adapter.SelectCommand = command;
                        adapter.Fill(this.TableData);

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



        //Richiamata da servizio WorkerServicePraticheSuccessione
        public DataTable FillTableService()
        {
            DfClientX cx;
            IDfClient client;
            IDfSession session = null;
            IDfSessionManager sessionManager = null;
            IDfLoginInfo logininfo;
            IDfQuery queryScan;
            IDfQuery queryDe;
            int idCentroOper;

            try
            {
                DataTable tableIns = new DataTable();

                tableIns.Columns.Add("Operatore", typeof(string));
                tableIns.Columns.Add("DataLavorazione", typeof(string));
                tableIns.Columns.Add("Documenti", typeof(int));
                tableIns.Columns.Add("Fogli", typeof(int));
                tableIns.Columns.Add("Pagine", typeof(int));

                tableIns.Columns.Add("IdFaseLavorazione", typeof(int));
                tableIns.Columns.Add("IdCentro", typeof(int));

                cx = new DfClientX();
                client = cx.getLocalClient();

                logininfo = cx.getLoginInfo();
                logininfo.setUser(_lavorazioniConfigManager.UserPraticheSucc);
                logininfo.setPassword(_lavorazioniConfigManager.PasswordPraticheSucc);
                logininfo.setDomain("");

                sessionManager = client.newSessionManager();
                sessionManager.setIdentity("GED_PI_SUCCESSIONI", logininfo);
                session = sessionManager.getSession("GED_PI_SUCCESSIONI");

                var startDataScan = "'" + StartDataLavorazione.ToString("dd/MM/yyyy") + " 06:00:00'";
                var endDataScan = EndDataLavorazione == null ? "'" + StartDataLavorazione.ToString("dd/MM/yyyy") + " 23:59:00'" : "'" + EndDataLavorazione.Value.ToString("dd/MM/yyyy") + " 23:59:00'";

                var startDataDE = "'" + StartDataLavorazione.ToString("dd/MM/yyyy") + " 06:00:00'"; 
                var endDataDE = EndDataLavorazione == null ? "'" + StartDataLavorazione.ToString("dd/MM/yyyy") + " 23:59:00'" : "'" + EndDataLavorazione.Value.ToString("dd/MM/yyyy") + " 23:59:00'";


                //Scansione
                string qsScan = "select pt_operatore_scan as operatore, DATETOSTRING(pt_data_scan, 'dd/mm/yyyy') as datalavorazione, count(pt_barcode_ad_uso_interno) as documenti, sum(pt_numero_pagine)/2 as fogli, sum(pt_numero_pagine) as pagine " +
                                "from bp_pratichesucc " +
                                "where pt_data_scan >= date(" + startDataScan + ",'dd/mm/yyyy hh:mi:ss') and pt_data_scan <= date(" + endDataScan + ",'dd/mm/yyyy hh:mi:ss') " +
                                "group by pt_operatore_scan, DATETOSTRING(pt_data_scan, 'dd/mm/yyyy')";


                //Data Entry
                string qsDe = @"select pt_operatore_index as operatore, DATETOSTRING(pt_data_index, 'dd/mm/yyyy') as datalavorazione, count(pt_barcode_ad_uso_interno) as documenti, sum(pt_numero_pagine)/2 as fogli, sum(pt_numero_pagine) as pagine " +
                               "from bp_pratichesucc " +
                               "where pt_data_index >= date(" + startDataDE + ",'dd/mm/yyyy hh:mi:ss') and pt_data_index <= date(" + endDataDE + ",'dd/mm/yyyy hh:mi:ss') " +
                               "group by pt_operatore_index, DATETOSTRING(pt_data_index, 'dd/mm/yyyy')";


                queryScan = cx.getQuery();
                queryScan.setDQL(qsScan);

                IDfCollection collection = queryScan.execute(session, 1);

                if (collection != null)
                {
                    while (collection.next())
                    {
                        string oper = collection.getString("operatore");
                        string operatorName = oper.Replace(" ", ".").ToLower();
                        operatorName = operatorName.Replace(@"postel\", "");
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);


                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName ))
                        {
                            idCentroOper = ElencoOperatoriTotale.First(x => x.Operatore == operatorName).Idcentro;
                        }
                        else
                        {
                            idCentroOper = ElencoOperatoriTotale.First(x => x.Operatore == "Not_Found_Oper").Idcentro;
                        }

                        tableIns.Rows.Add(operatorName, collection.getString("datalavorazione"), collection.getInt("documenti"), (int)collection.getDouble("fogli"), collection.getInt("pagine"), 4, idCentroOper);

                    }
                }

                queryDe = cx.getQuery();
                queryDe.setDQL(qsDe);

                IDfCollection collectionDe = queryDe.execute(session, 1);

                if (collectionDe != null)
                {
                    while (collectionDe.next())
                    {
                        string oper = collectionDe.getString("operatore");
                        string operatorName = oper.Replace(" ", ".").ToLower();
                        operatorName = operatorName.Replace(@"postel\", "");
                        operatorName = _normalizzaOperatore.CorreggiOperatore(operatorName);

                        if (ElencoOperatoriTotale.Any(x => x.Operatore == operatorName))
                        {
                            idCentroOper = ElencoOperatoriTotale.First(x => x.Operatore == operatorName).Idcentro;
                        }
                        else
                        {
                            idCentroOper = ElencoOperatoriTotale.First(x => x.Operatore == "Not_Found_Oper").Idcentro;
                        }

                        tableIns.Rows.Add(operatorName, collectionDe.getString("datalavorazione"), collectionDe.getInt("documenti"), (int)collectionDe.getDouble("fogli"), collectionDe.getInt("pagine"), 5, idCentroOper);
                    }
                }

                return tableIns;
                
            }
            catch (Exception ex)
            {
                if (session != null)
                {
                    sessionManager.release(session);
                }

                logger.Error(ex.Message);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (session != null)
                {
                    sessionManager.release(session);
                }
            }









        }

    }
}










