using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using LogDefault;

namespace ExpiredPasswordNotification
{
    class Program
    {
        #region class variables
        private static DataSet dsExpiringPW = new DataSet();
       // private static string logPath = "";
        private static string pw = "";
        private static string entity = "";       //HMC, UWMC
        private static bool debug = false;
        private static LogManager lm = LogManager.GetInstance();
        private static NameValueCollection ConfigData = null;
        #endregion

        static void Main(string[] args)
        {
            entity = args[0];
            ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings");
            GetParameters();
            OutputMngr om = OutputMngr.GetInstance();            
            om.Entity = entity;      //uw or hmc
            om.GetAccess();                     
            try
            {                               
                LoadExpiringPWDataSet();
                ProcessOutput(args[0]);
            }
            catch (Exception ex)
            {
                lm.Write("Program/Main:  " + ex.Message);
            }
            finally
            {
                Environment.Exit(1);
            }
        }

        private static void GetParameters()
        {
            debug = Convert.ToBoolean(ConfigData.Get("debug"));
            lm.LogFilePath = ConfigData.Get("logFilePath") + entity.ToLower() + @"\";
            lm.LogFile = ConfigData.Get("logFile");
        }

        private static void LoadExpiringPWDataSet()
        {
            DataSetLoad dsl = new DataSetLoad();
            dsl.ThisDs = dsExpiringPW;
            dsl.Debug = debug;
            dsl.LoadDataSet();
            dsExpiringPW = dsl.ThisDs;
        }

        private static void ProcessOutput(string entity)
        {
            ProcessOutput po = new ProcessOutput();
            po.DsOut = dsExpiringPW;
            po.Debug = debug;
            po.Entity = entity;
            po.CreateOutFile();
        }
       
    }
}
