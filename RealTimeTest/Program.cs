using Newtonsoft.Json;
using Npgsql;
using NUnit.Framework;
using RestSharp;
using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Z.BulkOperations;
namespace RealTimeTest
{

    public class Program
    {
        ATLoggerClass.ATFileLogger log = new ATLoggerClass.ATFileLogger();
        string connString = Convert.ToString(ConfigurationManager.AppSettings["DbConnection"]);
        string path = Convert.ToString(ConfigurationManager.AppSettings.Get("Logs_Path"));

        static void Main(string[] args)
        {
           
            Thread t = new Thread(delegate ()
            {
                ReadData();
            });
            t.Start();
        }

        public static void ReadData()
        {
            TcpListener server = null;
            ATLoggerClass.ATFileLogger log;
            log = new ATLoggerClass.ATFileLogger();
            string path = Convert.ToString(ConfigurationManager.AppSettings["Logs_Path"]);
            string IP = Convert.ToString(ConfigurationManager.AppSettings["IP"]);
            log.Path = path;
            try
            {
                Int32 port = 5000;
                IPAddress localAddr = IPAddress.Parse(IP);
                server = new TcpListener(localAddr, port);
                server.Start();
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Program main = new Program();
                    Thread t = new Thread(new ParameterizedThreadStart(main.ReadDataMain));
                    t.Start(client);
                }
            }
            catch(Exception ex)
            {
                log.Log("Thread " + ex);
            }
        }


        public void ReadDataMain(Object obj)
        {
            ATLoggerClass.ATFileLogger log;
            log = new ATLoggerClass.ATFileLogger();
            string path = Convert.ToString(ConfigurationManager.AppSettings["Logs_Path"]);
            log.Path = path;
            TcpClient client = (TcpClient)obj;
            try
            {
                NetworkStream stream = client.GetStream();
                int totalCount = 0;
                int datacount = 0;
                DataTable table;
                DataTable table2 = new DataTable();
                while (true)
                {
                    while (!stream.DataAvailable) ;
                    Byte[] bytes = new Byte[client.Available];
                    stream.Read(bytes, 0, bytes.Length);
                    String data = Encoding.UTF8.GetString(bytes);
                    data = data.ToUpper().Replace('"', ' ').Replace("PROJECTS ,TECH & TRANSFORMATION", "PROJECTS TECH & TRANSFORMATION");
                    Program prog = new Program();
                    table = prog.insertDataTable(data);
                    table2.Merge(table);
                    
                    if (table2.Rows.Count > 5000)
                    {
                        datacount = insertRealTimeData(table2);
                        table2 = new DataTable();
                        totalCount = totalCount + datacount;
                        log.Log("Total count " + totalCount);
                    }
                    
                }
                
            }
            
            catch (Exception ex)
            {
                log.Log("Exception " + ex);
            }
            client.Close();
        }
    

        public int insertData(string newdata)
        {
            string path = Convert.ToString(ConfigurationManager.AppSettings.Get("Logs_Path"));
            log = new ATLoggerClass.ATFileLogger();
            log.Path = path;       

            DataTable dataTable = new DataTable();
            DataTable dataTable2 = new DataTable();
            string[] columns = new string[]{"Event Time", "User", "URL", "Policy Action", "Cloud Application Class", "Cloud Application", "Sent Bytes", 
                                            "Received Bytes", "Server Trans. Time (ms)","Client Trans. Time (ms)","Location","Department","Client IP",
                                            "Server IP","Response","Device Owner","Device Hostname","Utilization"};

            foreach (string column in columns)
            {
                if (!dataTable.Columns.Contains(column))
                {
                    dataTable.Columns.Add(column.Trim());
                }
            }

            string[] strlines = newdata.Split(new string[] { "\n\n" }, System.StringSplitOptions.None);
            int linecount = strlines.Length;
            log.Log("Total Data coming : " + linecount);
            foreach (string ln in strlines)
            {
                var stringlines = ln.Split(',');
                int length = stringlines.Length;
                if (length > 17)
                {
                    var splittedLine = ln.Split(',');
                    var row = dataTable2.NewRow();
                    //log.Log("data > 17 :  " + ln);
                }
                else if (length < 17)
                {
                    var splittedLine = ln.Split(',');
                    var row = dataTable2.NewRow();
                    //log.Log("data < 17 :  " + ln);
                }
                else
                {
                    var splittedLine = ln.Split(',');
                    var row = dataTable.NewRow();
                    for (int i = 0; i < splittedLine.Length; i++)
                    {
                        row[i] = splittedLine[i].Trim();
                    }
                    string strlength = row[0].ToString().Trim();
                    if (strlength.Length > 3)
                    {
                        string trim = row[0].ToString().Trim().Substring(0, 3);
                        if (trim == "MON" || trim == "TUE" || trim == "WED" || trim == "THU" || trim == "FRI" || trim == "SAT" || trim == "SUN")
                        {
                            CultureInfo provider = CultureInfo.InvariantCulture;
                            DateTime date1= DateTime.ParseExact(row[0].ToString(), "ddd MMMM dd HH:mm:ss yyyy", provider);
                            row[0] = DateTime.Parse(date1.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            log.Log("data not proper :  " + ln);
                        }
                    }
                    else
                    {
                        log.Log("data length not proper :  " + ln);
                    }
                    if (row[17].ToString() == "")
                    {
                        int sentBytes = Convert.ToInt32(row[6].ToString());
                        double sentBytesinGB = sentBytes / (Math.Pow(1024, 3));
                        double recBytes = Convert.ToInt32(row[7].ToString());
                        double recBytesinGB = recBytes / (Math.Pow(1024, 3));
                        row[6] = sentBytesinGB;
                        row[7] = recBytesinGB;
                        row[17] = sentBytesinGB + recBytesinGB;
                    }
                    dataTable.Rows.Add(row);
                }
            }
            try
            {
                log.Log("Count hit to API: " + dataTable.Rows.Count);
                string jsonInput = DataTableToJSONWithJSONNet(dataTable);
                HitDataAPI(jsonInput);
                
                //NpgsqlConnection conn = new NpgsqlConnection(connString);
                //conn.Open();
                //var bulk = new BulkOperation(conn);
                //bulk.DestinationTableName = "RealTime_DataUpload";
                //bulk.BulkInsert(dataTable);
                //conn.Close();
                return dataTable.Rows.Count;
            }
            catch (Exception ex)
            {
                log.Log(ex.Message);
                return 0;
            }
        }

        public DataTable insertDataTable(string newdata)
        {
            string path = Convert.ToString(ConfigurationManager.AppSettings.Get("Logs_Path"));
            log = new ATLoggerClass.ATFileLogger();
            log.Path = path;

            try
            {

                DataTable dataTable = new DataTable();
                string[] columns = new string[]{"Event Time", "User", "URL", "Policy Action", "Cloud Application Class", "Cloud Application", "Sent Bytes",
                                            "Received Bytes", "Server Trans. Time (ms)","Client Trans. Time (ms)","Location","Department","Client IP",
                                            "Server IP","Response","Device Owner","Device Hostname","Utilization"};

                foreach (string column in columns)
                {
                    if (!dataTable.Columns.Contains(column))
                    {
                        dataTable.Columns.Add(column.Trim());
                    }
                }
                string[] strlines = newdata.Split(new string[] { "\n\n" }, System.StringSplitOptions.None);
                int linecount = strlines.Length;
                log.Log("Total Data coming : " + linecount);
                foreach (string ln in strlines)
                {
                    var stringlines = ln.Split(',');
                    int length = stringlines.Length;
                    if (length > 17)
                    {
                        var splittedLine = ln.Split(',');
                    }
                    else if (length < 17)
                    {
                        var splittedLine = ln.Split(',');
                    }
                    else
                    {
                        var splittedLine = ln.Split(',');
                        var row = dataTable.NewRow();
                        for (int i = 0; i < splittedLine.Length; i++)
                        {
                            row[i] = splittedLine[i].Trim();
                        }
                        string strlength = row[0].ToString().Trim();
                        if (strlength.Length > 3)
                        {
                            string trim = row[0].ToString().Trim().Substring(0, 3);
                            if (trim == "MON" || trim == "TUE" || trim == "WED" || trim == "THU" || trim == "FRI" || trim == "SAT" || trim == "SUN")
                            {
                                CultureInfo provider = CultureInfo.InvariantCulture;
                                DateTime date1 = DateTime.ParseExact(row[0].ToString(), "ddd MMMM dd HH:mm:ss yyyy", provider);
                                row[0] = DateTime.Parse(date1.ToString()).ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                log.Log("data not proper :  " + ln);
                            }
                        }
                        else
                        {
                            log.Log("data length not proper :  " + ln);
                        }
                        if (row[17].ToString() == "")
                        {
                            int sentBytes = Convert.ToInt32(row[6].ToString());
                            double sentBytesinGB = sentBytes / (Math.Pow(1024, 3));
                            double recBytes = Convert.ToInt32(row[7].ToString());
                            double recBytesinGB = recBytes / (Math.Pow(1024, 3));
                            row[6] = sentBytesinGB;
                            row[7] = recBytesinGB;
                            row[17] = sentBytesinGB + recBytesinGB;
                        }
                        dataTable.Rows.Add(row);
                    }

                }
                return dataTable;
            }
            catch(Exception ex)
            {
                log.Log("DataTable got null: " + ex.Message);
                return null;
            }
        }
        public int insertRealTimeData(DataTable table)
        {
            try
            {
                string path = Convert.ToString(ConfigurationManager.AppSettings.Get("Logs_Path"));
                log = new ATLoggerClass.ATFileLogger();
                log.Path = path;
                log.Log("Count hit to API: " + table.Rows.Count);
                string jsonInput = DataTableToJSONWithJSONNet(table);
                HitDataAPI(jsonInput);

                //NpgsqlConnection conn = new NpgsqlConnection(connString);
                //conn.Open();
                //var bulk = new BulkOperation(conn);
                //bulk.DestinationTableName = "RealTime_DataUpload";
                //bulk.BulkInsert(dataTable);
                //conn.Close();
                return table.Rows.Count;
            }
            catch (Exception ex)
            {
                log.Log(ex.Message);
                return 0;
            }
        }

        public void HitDataAPI(string input)
        {
            string dashboardURL =  Convert.ToString(ConfigurationManager.AppSettings["SharepointDashboardURL"]);
            using (var client = new HttpClient())
            {
                string url = dashboardURL;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var content = new StringContent(input, Encoding.UTF8, "application/json");
                var response = client.PostAsync(url, content).Result;
            }
        }
        public string DataTableToJSONWithJSONNet(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
        }
    }
}
