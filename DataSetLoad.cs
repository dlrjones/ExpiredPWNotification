using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using OleDBDataManager;
using LogDefault;

namespace ExpiredPasswordNotification
{
    class DataSetLoad
    {
        #region class variables
        private DataSet thisDS;
        private NameValueCollection ConfigData = null;
        protected static ODMDataFactory ODMDataSetFactory = null;
        private OutputMngr om = OutputMngr.GetInstance();
        private LogManager lm = LogManager.GetInstance();
        private bool debug = false;
        public DataSet ThisDs
        {
            get { return thisDS; }
            set { thisDS = value; }
        }
        public bool Debug
        {
            set { debug = value; }
        }
        #endregion

        public DataSetLoad()
        { 
            om.Debug = debug;
            ODMDataSetFactory = new ODMDataFactory();          
        }

        public void LoadDataSet()
        {
            ODMRequest Request = new ODMRequest();
            Request.ConnectString = om.DbaseConnStr;
            Request.CommandType = CommandType.Text;
            Request.Command = "Execute ('" + BuildQuery() + "')";

            if(debug)
                lm.Write("DataSetLoad/LoadDataSet:  " + Request.Command);
            try
            {
                thisDS = ODMDataSetFactory.ExecuteDataSetBuild(ref Request);
                //QuickWatch:
                //((System.Data.DataRow)((new System.Linq.SystemCore_EnumerableDebugView(((System.Data.DataTable)((new System.Collections.ArrayList.ArrayListDebugView(thisDS.Tables.List)).Items[0])).Rows.table.Rows.list)).Items[0])).ItemArray               
                // Row Count:
                //((System.Data.DataTable)((new System.Collections.ArrayList.ArrayListDebugView(thisDS.Tables.List)).Items[0])).Rows.Count	
            }
            catch (Exception ex)
            {
                lm.Write("DataSetLoad/LoadDataSet:  " + ex.Message);
            }
        }

        private string BuildQuery()
        {   //
            return
                "select USR_ID,LOGIN_ID,EMAIL,NAME,MONTH(PSWD_EXPIRATION_DATE) [MONTH], " +
                "DAY(PSWD_EXPIRATION_DATE) [DAY],YEAR(PSWD_EXPIRATION_DATE) [YEAR], " +
                "DATEDIFF(DAY,GETDATE(),PSWD_EXPIRATION_DATE) [Days Left] " +
                "FROM USR " +
                "WHERE INACT_IND = ''N'' " +
                "AND DATEDIFF(DAY,GETDATE(),PSWD_EXPIRATION_DATE) > 0 " +
                "AND DATEDIFF(DAY,GETDATE(),PSWD_EXPIRATION_DATE) < 14 " +
                "order by [Days Left] ";
        }

    }
}
