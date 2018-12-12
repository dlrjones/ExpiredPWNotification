using System;
using System.Net.Mail;
using System.Collections.Specialized;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Threading;
using KeyMaster;
using LogDefault;

namespace ExpiredPasswordNotification
{
    class OutputMngr
    {
        #region class variables
        private static NameValueCollection ConfigData = null;
        private Hashtable daysLeft = new Hashtable();
        private Hashtable endDate = new Hashtable();
        private static string corp = "";
        private static string attachmentPath = "";
        private static string archiveFile = "";
        private string userOffsetList = "";
        private string dbaseConnStr = "";
        private string entity = "";
        private string email = "";
        private string[] keyChain;
        private char TAB = '\t';
        private bool debug = false;
        private static OutputMngr outMngr = null;
        private LogManager lm = LogManager.GetInstance();
        #endregion
        #region properties
        public Hashtable EndDate
        {
            set { endDate = value; }
        }
        public Hashtable DaysLeft
        {
            set { daysLeft = value; }
        }
        public bool Debug
        {
            set { debug = value; }
        }
        public string AttachmentPath
        {
            get { return attachmentPath; }
            set { attachmentPath = value; }
        }
        public string Entity
        {
            get { return entity; }
            set { entity = value; }
        }

        public string DbaseConnStr
        {
            get { return dbaseConnStr; }
            set { dbaseConnStr = value; }
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        #endregion

        private OutputMngr()
        {
            // this constructor is private to force the calling program to use GetInstance()
            ////InitOutMngr();            
            ConfigData = (NameValueCollection)ConfigurationSettings.GetConfig("appSettings"); 
            debug = Convert.ToBoolean(ConfigData.Get("debug"));            
        }

        public void GetAccess()
        {
            int line = -1;
            try
            {
                attachmentPath = ConfigData.Get("attachmentPath");
                archiveFile = ConfigData.Get("archiveFile");
                userOffsetList = ConfigData.Get("unameVariantList");
                lm.Write(entity.ToUpper());
                dbaseConnStr = entity.ToLower() == "hmc" ? ConfigData.Get("HMCConnect") : ConfigData.Get("UWConnect");
                //0 = hmc  1 = uw  2 = email  3 = debug  4 = nwh (when implemented)
                if (debug)
                {
                    line = 1; //[3] = debug
                }
                else
                {
                    line = entity.ToLower() == "hmc" ? 0 : 1;   // (entity.ToLower() == "uw" ? 1 : 2);
                }
                keyChain = File.ReadAllLines(attachmentPath + archiveFile);
                dbaseConnStr += StringCipher.Decrypt(keyChain[line], "pmmjobs");
                email = StringCipher.Decrypt(keyChain[2], "pmmjobs"); // [2] = email pw
                attachmentPath += entity + "\\";
            }
            catch (Exception ex)
            {
                lm.Write("GetAccess: " + Environment.NewLine + ex.Message);             
            }
        }

        public void SendMail(string mailTo)
        {
            string[] mailList = mailTo.Split(";".ToCharArray());
            int debugCount = 0;
            string days = "";

            try
            {
                foreach (string recipient in mailList)
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient("smtp.uw.edu");
                    if (debug)
                        { //this is where dlrjones gets substituted for the real recipient when debug is true
                            mail.To.Add("dlrjones@uw.edu");                           
                        }
                    else
                        {
                            mail.To.Add(recipient);
                        }
                    days = Convert.ToInt32(daysLeft[recipient]) > 1 ? "days" : "day";
                    mail.From = new MailAddress("pmmhelp@uw.edu", "pmmHelp");
                    mail.Subject = "Your " + entity.ToUpper() + " HEMM Password Expires in " + daysLeft[recipient] + " " + days;
                    if (mail.To.ToString() == "dlrjones@uw.edu")
                        mail.Subject = "Your " + entity.ToUpper() + " HEMM Password Expires in " + daysLeft[recipient] + " " + days + " " + mailList.Length + " emails";
                    if (entity == "uw")
                        entity = "uwmc";
                    else if (entity == "hmc")
                        entity = "harborview";

                    //entity = entity == "uw" ? "uwmc" : "harborview";

                    lm.Write("Entity = " + entity);
                    mail.Body = "Your " + entity.ToUpper() + " HEMM password will expire in " + daysLeft[recipient] + " " + days + " on " + endDate[recipient] + "." + Environment.NewLine +                        
                    
                         "The attached file can help you find where to change your password." +
                          Environment.NewLine +
                          "Thanks." +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                 Environment.NewLine +
                                "PMMHelp" + Environment.NewLine +
                                "UW Medicine" + Environment.NewLine +
                                "Supply Chain Management Informatics" + Environment.NewLine +
                                "206-598-0044" + Environment.NewLine +
                                "pmmhelp@uw.edu";
                    mail.ReplyToList.Add("pmmhelp@uw.edu");

                    Attachment attachment;
                    attachment =
                        new Attachment(AttachmentPath + "Changing Your HEMM Password.pdf");

                    mail.Attachments.Add(attachment);

                    SmtpServer.Port = 587;
                    SmtpServer.Credentials = new System.Net.NetworkCredential("pmmhelp", Email);
                    SmtpServer.EnableSsl = true;

                    if (debugCount == 0)
                    {//this is only incremented when debug = true so that I can see 1 email instead of mailList.Count emails
                        SmtpServer.Send(mail);
                        lm.Write(recipient + TAB + daysLeft[recipient] + TAB + endDate[recipient]);
                    }

                    if (debug)
                    debugCount++;
                }
            }
            catch (Exception ex)
            {
                string mssg = ex.Message;
                lm.Write("OutputMngr/SendMail:  " + mssg);
            }
        }

        public string[] GetUserOffsetList()
        {
            string[] users = File.ReadAllLines(userOffsetList);
            return users;
        }

        public static OutputMngr GetInstance()
        {
            if (outMngr == null)
            {
                CreateInstance();
            }
            return outMngr;
        }

        private static void CreateInstance()
        {
            Mutex configMutex = new Mutex();
            configMutex.WaitOne();
            outMngr = new OutputMngr();
            configMutex.ReleaseMutex();
        }             
    }
}
