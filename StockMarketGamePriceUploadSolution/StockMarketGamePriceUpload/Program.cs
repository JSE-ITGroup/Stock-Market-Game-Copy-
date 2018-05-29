using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using log4net;

using System.Configuration;

namespace StockMarketGamePriceUpload
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {

            #region Get SSH Settings
            const string configSectionName = "SSHSettings";

            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var sectionKey = configuration.Sections.Keys;
            var section = configuration.GetSection(configSectionName);
            var appSettings = section as AppSettingsSection;
            if (appSettings == null)
            {
                log.Error("Config Section for AppSettings was not Found");

            }

            Dictionary<string, Object> SSHParams = new Dictionary<string, object>();
            foreach (KeyValueConfigurationElement setting in appSettings.Settings)
            {

                SSHParams[setting.Key] = setting.Value;
            }

            #endregion



            bool filecreated = false;
            string filename = Properties.Settings.Default.FileName;

            string[] EnvironmentList = Properties.Settings.Default.Environments.ToString().Split(',');



            //Logging

            log.Info(string.Format("Destination File retrieved: {0}", filename));

            Operations operations = new Operations();
            string storedProcedure = Properties.Settings.Default.QueryProcedure;

            log.Info(string.Format("Stored Procedure retrieved: {0}", storedProcedure));


            DataTable pricedt = operations.GetDataTableMSSQL(storedProcedure);

            if (pricedt != null)
            {
                int rowcount = pricedt.Rows.Count;
                if (rowcount > 0)
                {

                    log.Info(string.Format("{0} Record(s) retrieved from dataset {0}", rowcount));


                    filecreated = operations.CreateCSVFile(pricedt, ",", true);

                    log.Info(string.Format("The price file was succesfully created at: {0}", filename));


                    if (filecreated)
                    {
                        SshWrapper.Connect(SSHParams);
                        //

                        string ConnectionString = null;
                        string TableName = null;
                        Dictionary<string, string> dic = null;
                        string[] sslparam = null;
                        string lookupQuery = null;

                        foreach (string Env in EnvironmentList)
                        {
                            if (Env.ToUpper() == "PRODUCTION")
                            {
                                ConnectionString = Properties.Settings.Default.JseWebSiteProd;
                                TableName = Properties.Settings.Default.ProdTableName;
                                dic = new Dictionary<string, string>();
                               // sslparam = Properties.Settings.Default.ProductionSSHParams.Split(';');
                                lookupQuery = Properties.Settings.Default.DestinationLookupQuery;


                                var uniqueDates = pricedt.AsEnumerable()
                                .Select(s => new
                                {
                                    id = "'" + s.Field<string>("TradeDate") + "'"
                                })
                                .Distinct().ToList();

                                string dates = string.Join(",", uniqueDates.Select(a => a.id).ToList());


                                lookupQuery = lookupQuery.Replace("@Date", dates);
                                DataTable dt = operations.GetDataTableMySQL(lookupQuery, ConnectionString);

                                if (dt != null)
                                {
                                    int cnt = dt.Rows.Count;

                                    log.Info(string.Format("It seems like table was already updated. The Destination Table contains {0} Records that match the date {1}. No Update will take place in Evironment: {2}", cnt, dates, Env));
                                }
                                else
                                {
                                    operations.UpdateJseWebsite(filename, pricedt, ConnectionString, TableName, Env);
                                }

                            }

                            else if (Env.ToUpper() == "TRAINING")
                            {
                                ConnectionString = Properties.Settings.Default.JseWebsiteTraning;
                                TableName = Properties.Settings.Default.TrainingTableName;
                                dic = new Dictionary<string, string>();
                               // sslparam = Properties.Settings.Default.TraningSSHParams.Split(';');
                                lookupQuery = Properties.Settings.Default.DestinationLookupQuery;



                                var uniqueDates = pricedt.AsEnumerable()
                                .Select(s => new
                                {
                                    id = "'" + s.Field<string>("TradeDate") + "'"
                                })
                                .Distinct().ToList();

                                string dates = string.Join("", uniqueDates.Select(a => a.id).ToList());


                                lookupQuery = lookupQuery.Replace("@Date", dates);
                                DataTable dt = operations.GetDataTableMySQL(lookupQuery, ConnectionString);

                                if (dt != null)
                                {
                                    int cnt = dt.Rows.Count;

                                    log.Info(string.Format("It seems like table was already updated. The Destination Table contains {0} Records that match the date {1}. No Update will take place in Evironment: {2}", cnt, dates, Env));
                                }
                                else
                                {
                                    operations.UpdateJseWebsite(filename, pricedt, ConnectionString, TableName, Env);
                                }



                            }
                            else if (Env.ToUpper() == "TESTING")
                            {
                                ConnectionString = Properties.Settings.Default.JseWebsiteTesting;
                                TableName = Properties.Settings.Default.TestingSSHParams;
                                dic = new Dictionary<string, string>();
                                //sslparam = Properties.Settings.Default.TestingSSHParams.Split(';');
                                lookupQuery = Properties.Settings.Default.DestinationLookupQuery;

                               




                                var uniqueDates = pricedt.AsEnumerable()
                                .Select(s => new
                                {
                                    id = "'" + s.Field<string>("TradeDate") + "'"
                                })
                                .Distinct().ToList();

                                string dates = string.Join("", uniqueDates.Select(a => a.id).ToList());


                                lookupQuery = lookupQuery.Replace("@Date", dates);
                                DataTable dt = operations.GetDataTableMySQL(lookupQuery, ConnectionString);

                                if (dt != null)
                                {
                                    int cnt = dt.Rows.Count;
                                    log.Info(string.Format("It seems like table was already updated. The Destination Table contains {0} Records that match the date {1}. No Update will take place in Evironment: {2}", cnt, dates, Env));

                                }
                                else
                                {
                                    operations.UpdateJseWebsite(filename, pricedt, ConnectionString, TableName, Env);
                                }



                            }
                            else
                            {
                                log.Error("Invalid Environments.");

                            }
                        }//End Environment


                        //disconnect
                        SshWrapper.Disconnect();
                    }
                    else
                    {
                        //Action if no file was not created

                        log.Error("Error Creating Price file.");
                    }

                }
                else
                {
                    //No Data Retrieve from the WebRepository
                    log.Error(string.Format("{0} Record(s) from Dataset", rowcount));

                }
            }

            else
            {

                //No Data Retrieve from the WebRepository
                log.Error(string.Format("No data receieved from database, Please verify that the stored procedure: {0} returns a value.", storedProcedure));

            }


        }
    }
}
