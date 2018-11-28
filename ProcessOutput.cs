using System;
using System.Collections;
using System.Data;
using LogDefault;

namespace ExpiredPasswordNotification
{
    class ProcessOutput
    {
        #region Class Variables
        private DataSet dsOut;
        private ArrayList rowDataOut = new ArrayList();
        private ArrayList specialEmail = new ArrayList();
        private OutputMngr om = OutputMngr.GetInstance();
        private LogManager lm = LogManager.GetInstance();
        private Hashtable daysLeft = new Hashtable();
        private Hashtable endDate = new Hashtable();
        private Hashtable emailAddress = new Hashtable();
        private Hashtable unameOffset = new Hashtable();      
        private bool debug = false;
        private string mailTo = "";
        private string entity = "";
        public string Entity
        {
            set { entity = value; }
        }
        public DataSet DsOut
        {
            set { dsOut = value; }
        }
        public bool Debug
        {
            set { debug = value; }
        }       
        #endregion

        ////class UserNameVariant
        ////{
        ////    //takes in the path to the text file containing the names of users who have an email id different from their AMC id.
        ////    //each entry looks like this - mdanna|dannam
        ////    Hashtable userItems = new Hashtable();
        ////    private string unamePath = "";
        ////    private LogManager lm = LogManager.GetInstance();

        ////    public Hashtable UserItems
        ////    {
        ////        get { return userItems; }
        ////        set
        ////        {
        ////            userItems = value;
        ////            GetUserNameVariant();
        ////        }
        ////    }

        ////    private void GetUserNameVariant()
        ////    {
        ////        string[] users = File.ReadAllLines(unamePath);
        ////        ArrayList tmpValu = new ArrayList();
        ////        string[] user;

        ////        foreach (string name in users)
        ////        {
        ////            user = name.Split("|".ToCharArray());
        ////            if (user.Length > 0)
        ////            {
        ////                if (user.Length > 1)
        ////                {
        ////                    //change the userItems entry for users that have a different email
        ////                    if (userItems.ContainsKey(user[0]))
        ////                    {
        ////                        try
        ////                        {
        ////                            tmpValu = (ArrayList)userItems[user[0]];
        ////                            userItems.Remove(user[0]);
        ////                            userItems.Add(user[1], tmpValu);
        ////                        }
        ////                        catch (Exception ex)
        ////                        {
        ////                            lm.Write("ProcessManager/GetUserNameVariant:  " + ex.Message);
        ////                        }
        ////                    }
        ////                }
        ////            }
        ////        }
        ////    }
        ////    //          WHERE LOGIN_ID IN ('mharrington','sbuckingham','mdanna','bsimmons','ayyoub','fyoshioka',
        ////    //'jvarghese','mvasiliades','bwalker','dlam','sgoss','enewcombe','aclouse','jrudd',
        ////    //'kburkette','ewardenburg','mofo','ccastillo')
        ////}

        public void CreateOutFile()
        {            
            om.Debug = debug;
            if(debug)
                lm.Write("ProcessOutput/CreateOutFile:  " + "");
            
            GetUserNameOffsetList();
            DSetBreakdown();             

            //add dlrjones to end of each mail list
            mailTo = mailTo.Length == 0 ? "dlrjones@uw.edu" : mailTo + ";dlrjones@uw.edu";
         SendMail();            
        } 

        private void GetUserNameOffsetList()
        {
            //READ THE TEXT FILE THAT HOLDS THE REAL USER NAMES OF USERS WHOSE EMAILS HAVE PREVIOUSLY BOUNCED AND PUT INTO unameOffset
            string[] uNames = om.GetUserOffsetList();
            string[] user;
            foreach (string name in uNames)
            {
                user = name.Split("|".ToCharArray());
                if (user.Length > 0)
                {
                    if (user.Length > 1)
                    {
                        if (!unameOffset.ContainsKey(user[0]))
                            unameOffset.Add(user[0], user[1]);
                    }
                }
            }
        }

        private void DSetBreakdown()
        {   //takes one datarow at a time, converts it to an ArrayList to be sent to OutputMngr  
            try
            {
                foreach (DataRow drow in dsOut.Tables[0].Rows)
                {
                    SendRow(drow);
                }
            }
            catch (Exception ex)
            {
               // OutputMngr om = OutputMngr.GetInstance();
                string mssg = ex.Message;
                if (mssg.Equals("Cannot find table 0."))
                    mssg = "No Passwords Expiring" + Environment.NewLine;
                lm.Write("Process/DSetBreakdown:  " + mssg);
            }
        }

        private void SendRow(DataRow outRow)
        {
            string userEmail = "";
            string userName = "";
            /*              ----------------------------------  required  ----------------------------------------               
             * LOG DATE| USR_ID | LOGIN_ID | EMAIL | NAME | MONTH | DAY | YEAR | DAYS LEFT                                    
            */
            try
            {
                rowDataOut.Add(outRow[0].ToString());   //USR_ID
                rowDataOut.Add(outRow[1].ToString());   //LOGIN_ID
                userName = outRow[1].ToString().Trim();
                if (entity.ToLower().Equals("uw"))//EMAIL 
                {
                    userEmail = ParseEmailAddress(outRow[2].ToString());  //the email address is fully formed for UW HEMM users                    
                }
                else
                {
                    userEmail = outRow[2].ToString().Trim();//EMAIL
                    userEmail = FindValidEmail(userName, userEmail); //this checks the unameOffset list and appends the uw.edu to the amc user name of HMC HEMM users
                }
                rowDataOut.Add(userEmail);  //this is for a log entry
                rowDataOut.Add(outRow[3].ToString());   //NAME
                rowDataOut.Add(outRow[4].ToString());   //MONTH
                rowDataOut.Add(outRow[5].ToString());   //DAY
                rowDataOut.Add(outRow[6].ToString());   //YEAR
                rowDataOut.Add(outRow[7].ToString());   //DAYS LEFT    
         //       lm.Write(rowDataOut);
                //lm.WriteArrayList();
                BuildMailToList(userName,userEmail);
                daysLeft.Add(userEmail, outRow[7].ToString().Trim());
                endDate.Add(userEmail, outRow[4].ToString().Trim() + "/" + outRow[5].ToString().Trim() + "/" + outRow[6].ToString().Trim());
                rowDataOut.Clear();
            }
            catch (Exception ex)
            {
                lm.Write("Process/SendRow:  " + ex.Message);
            }
        }

        private string FindValidEmail(string uname, string email)
        {
           // uname = "ewardenburg";

            //CHECK IF USERNAME IS IN unameOffset LIST AND PUT THAT NAME INTO uname
            string nameCheck = "";
            bool specialAddress = false;

            if (unameOffset.ContainsKey(uname))
                nameCheck = unameOffset[uname].ToString();
            
            uname = nameCheck.Length > 0 ? nameCheck : uname;        
            if (CheckSpecialAddress(uname))
                specialAddress = true;  //the full address has already been copied in from the username.txt file
  
           return uname + (specialAddress ? "" : "@uw.edu");          
        }

        private bool CheckSpecialAddress(string uname)
        {//some end users have a different email domain. this is where that fact is identified
            bool isSpecial = false;
            foreach (string email in specialEmail)
            {
                if (uname.Contains(email))
                {
                    isSpecial = true;
                    break;
                }
            }
            return isSpecial;
        }

        private string ParseEmailAddress(string email)
        {//UW user email addresses have the location as part of the same field. for no good reason, just some minor convenience
                 //ie: "ACCOUNTING - SHARON MCAULEY" smcauley@uw.edu
            string[] mail = email.Split("\"".ToCharArray());
            if (mail.Length > 0)
            {
                if (mail.Length > 1)
                    email = mail[2].Trim();
                else
                {
                    email = mail[0].Trim();
                }
            }
            return email;
        }       

        private void BuildMailToList(string uname, string email)
        {
            //mailTo = FindValidEmail("mharrington","junk@uw.edu");
            if (entity.Equals("uw"))
                mailTo = mailTo.Length == 0 ? email : mailTo + ";" + email;
            else
                mailTo = mailTo.Length == 0 ? FindValidEmail(uname, email).Trim() : mailTo + ";" + FindValidEmail(uname, email).Trim();
        }

        private void SendMail()
        {
            om.DaysLeft = daysLeft;
            om.EndDate = endDate;
            om.SendMail(mailTo);           
        }        
    }
}
