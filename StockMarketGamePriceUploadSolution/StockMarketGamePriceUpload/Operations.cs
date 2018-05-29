using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.IO;
using log4net;



namespace StockMarketGamePriceUpload
{
    public class Operations
    {

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    

        string tableName = Properties.Settings.Default.ProdTableName;
        string fieldTerminator = Properties.Settings.Default.FieldTerminator;
        string lineTerminator = Properties.Settings.Default.LineTerminator;
        string fileName = Properties.Settings.Default.FileName;
        int numberOfLinesToSkip = Properties.Settings.Default.NumberOfLinesToSkip;
    

        public DataTable GetDataTableMSSQL(string query)
        {

            DataTable dt = null;
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = null;

            try
            {
                cmd = new SqlCommand(query, new SqlConnection(GetMSSQLConnectionString()));
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection.Open();
                cmd.CommandTimeout = 0;

                da.SelectCommand = cmd;
                dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    return dt;

                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                
                log.Error( ex.Message);
                return null;
            }

            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }
            }

            return dt;
        }

        public string GetMSSQLConnectionString()
        {
            return Properties.Settings.Default.WebRepoSQLConnection.ToString();
        }

        public string GetMySQLConnectionString()
        {
            return Properties.Settings.Default.JseWebSiteProd.ToString();
        }
        public bool CreateCSVFile(DataTable pricesdt, string delimiter, bool includeHeader)
        {
            try
            {
            var result = new StringBuilder();

            if (includeHeader)
            {
                foreach (DataColumn column in pricesdt.Columns)
                {
                    result.Append(column.ColumnName);
                    result.Append(delimiter);
                }

                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);
            }

            foreach (DataRow row in pricesdt.Rows)
            {
            

                foreach (object item in row.ItemArray)
                {
                    if (item is DBNull)
                        result.Append(delimiter);
                    else
                    {
                        string itemAsString = item.ToString();
                        // Double up all embedded double quotes

                       // itemAsString = itemAsString.Replace("\"", "\"\"");

                        // To keep things simple, always delimit with double-quotes
                        // so we don't have to determine in which cases they're necessary
                        // and which cases they're not.
                      //  itemAsString = "\"" + itemAsString + "\"";

                        result.Append(itemAsString + delimiter);
                    }
                }

                result.Remove(--result.Length, 0);
                result.Append(Environment.NewLine);
            }

            
                File.WriteAllText(fileName, result.ToString());
            }
            catch (Exception ex)
            {
               
                log.Error(ex.Message);
                return false;
            }

            return true;
        }
        public bool UpdateJseWebsite(string FileName, DataTable dt,string ConnectionString,string TableName, string Env)
        {


            MySqlConnection mysqlConn=null;
            MySqlBulkLoader bulkloader = null;
            
                


            string[] columnNames = dt.Columns.Cast<DataColumn>()
                                 .Select(x => x.ColumnName)
                                 .ToArray(); 

            try
            {

                mysqlConn = new MySqlConnection(ConnectionString);

                 mysqlConn.Open();
                 bulkloader = new MySqlBulkLoader(mysqlConn);

                 bulkloader.Columns.AddRange(columnNames);
                 bulkloader.TableName = TableName;
                 bulkloader.FieldTerminator = fieldTerminator;
                 bulkloader.LineTerminator = lineTerminator;
                 bulkloader.FileName = FileName;
                 bulkloader.NumberOfLinesToSkip = numberOfLinesToSkip;
                 
    


                int count = bulkloader.Load();
               log.Info( count.ToString() + " Record(s)  Uploaded in Environment:  " + Env);
                //Log here
               
            }
            catch (Exception exp)
            {
               
                log.Error("Environment :" + Env, exp );
                return false;
                //throw exp;

            }

            finally
            {


                 if ( bulkloader.Connection.State == ConnectionState.Open)
                 {
                     bulkloader.Connection.Close();
                }
            }



            return true;

        }

        public DataTable GetDataTableMySQL(string Query,string ConnectionString)
        {

            DataTable dt = null;

            MySqlDataAdapter da = new MySqlDataAdapter();
            MySqlCommand cmd = null;


            try
            {
                cmd = new MySqlCommand(Query, new MySqlConnection(ConnectionString));
            
                cmd.Connection.Open();
                cmd.CommandTimeout = 0;


                da.SelectCommand = cmd;
                dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    return dt;

                }
                else
                    return null;
            }
            catch (Exception exp)
            {
       
                log.Error( exp.Message.ToString());


            }

            finally
            {
                if (cmd.Connection.State == ConnectionState.Open)
                {
                    cmd.Connection.Close();
                }
            }



            return dt;

        }
    }
}
