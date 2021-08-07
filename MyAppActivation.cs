using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using BitnuaVideoPlayer;
using System.Management;

static class MyAppActivation
{
    private const string c_AppID = "7";

    public static void ValidateActivation()
    {
        if (!System.IO.File.Exists(System.Environment.GetEnvironmentVariable("WINDIR") + @"\ISC.dat"))
            throw new Exception("אין רישון לתוכנה, לקניית רישיון פנה ל\n ita.asis@gmail.com");

        var sym = new Encryption.Symmetric(Encryption.Symmetric.Provider.Rijndael);
        Encryption.Data key = new Encryption.Data("®nI¥)DÃO‰v1aË³Êj³¯Aœ&dvÕ±");
        System.IO.StreamReader fr = new System.IO.StreamReader(System.Environment.GetEnvironmentVariable("WINDIR") + @"\ISC.dat");
        try
        {
            string str;
            Encryption.Data decryptedData, encryptedData;
            bool HasValidActivation = false;
            string[] Data = null;
            string ProccessorID = RunQuery("Processor", "ProcessorId");
            string Data2 = RunQuery("NetworkAdapterConfiguration", "MacAddress");

            do
            {
                // read throw encrypted activation strings
                encryptedData = new Encryption.Data();
                str = fr.ReadLine();
                if (str == null)
                    break;
                encryptedData.Base64 = str;
                decryptedData = sym.Decrypt(encryptedData, key);
                Data = decryptedData.ToString().Replace(",!!!,", ",").Split(',');
                if (Data[0] == c_AppID)
                {
                    // check if processor id is maching the licenced one
                    if (Data[1] == ProccessorID)
                    {
                        for (var i = 2; i <= Data.Count() - 1; i++)
                        {
                            if (Data2.Contains(Data[i]))
                            {
                                // pc mac address is same as mac address found in dat file
                                HasValidActivation = true;
                                break;
                            }
                        }
                    }
                }
            }
            while (true);

            if (HasValidActivation)
            {
                DateTime ExDate;
                if (DateTime.TryParse(Data.Last(), out ExDate))
                {
                    if (IsConnectionAvailable())
                    {
                        // check trail time 
                        //DateTime NetTime = Daytime.GetTime.Add(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now));
                        //if (NetTime.ToShortDateString() != DateTime.Now.ToShortDateString())
                        //    // system date is incorrect
                        //    throw new Exception("תאריך המערכת אינו נכון ולכן התוכנה לא יכולה להפתח." + "\n" + "היום ה " + NetTime.ToShortDateString() + "\n" + "עדכן את התאריך במערכת.");
                        //else
                        {
                            // system date is correct
                            // log date
                            UserSettings.Instance.SetValue("LastRunDate", DateTime.Now);
                            // check if expied date has passed
                            if (DateTime.Compare(ExDate, DateTime.Today) <= 0)
                                throw new Exception("הרשיון לתוכנה פג." + "\n" + "לקבלת רשיון פנה ל-" + "\n" + "ita.asis@gmail.com");
                            return;
                        }
                    }
                    else
                    {
                        // there is no internet connection
                        // check if now date is bigger then the last loged date, if not-exception
                        if (DateTime.Compare((DateTime)(UserSettings.Instance.GetValue("LastRunDate")), DateTime.Now) > 0)
                            throw new Exception("תאריך המערכת אינו נכון ולכן התוכנה לא יכולה להפתח." + "\n" + "התחבר לחיבור אינטרנט ועדכן את התאריך במערכת.");
                        // check if expied date has passed
                        if (DateTime.Compare(ExDate, DateTime.Today) <= 0)
                            throw new Exception("הרשיון לתוכנה פג." + "\n" + "לקבלת רשיון פנה ל-" + "\n" + "ita.asis@gmail.com");
                        return;
                    }
                }
                else
                    // there is no expiration date in dat file
                    return;
            }
        }
        finally
        {
            fr.Close();
        }
        throw new Exception("אין רישון לתוכנה, לקניית רישיון פנה ל" + "\n" + "ita.asis@gmail.com");
    }

    private static bool IsConnectionAvailable()
    {
        // Call url
        System.Uri url = new System.Uri("http://www.google.com/");
        // Request for request
        System.Net.WebRequest req;
        req = System.Net.WebRequest.Create(url);
        System.Net.WebResponse resp;
        try
        {
            resp = req.GetResponse();
            resp.Close();
            req = null;
            return true;
        }
        catch (Exception ex)
        {
            req = null;
            return false;
        }
    }

    public static string ComputerMacAddresses(System.Net.NetworkInformation.NetworkInterfaceType NetworkInterfaceType = default(System.Net.NetworkInformation.NetworkInterfaceType))
    {
        StringBuilder str = new StringBuilder();

        foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.GetPhysicalAddress().ToString() != "")
            {
                if (NetworkInterfaceType == default(System.Net.NetworkInformation.NetworkInterfaceType))
                    str.Append(nic.GetPhysicalAddress().ToString() + ",");
                else if (NetworkInterfaceType == nic.NetworkInterfaceType)
                    str.Append(nic.GetPhysicalAddress().ToString() + ",");
            }
        }
        str.Remove(str.Length - 1, 1);
        return str.ToString();
    }

    public static string RunQuery(string TableName, string MethodName, string strComputer = "")
    {
        if (strComputer == "")
            strComputer = System.Environment.MachineName;

        string wmiQuery = $"SELECT * FROM Win32_{TableName}";

        //ManagementObjectSearcher searcher = new ManagementObjectSearcher(strComputer, wmiQuery);
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
        ManagementObjectCollection retObjectCollection = searcher.Get();

        StringBuilder str = new StringBuilder();
        try
        {
            foreach (var objSMBIOS in retObjectCollection)
            {
                var val = objSMBIOS[MethodName];
                switch (MethodName)
                {
                    case "ProcessorId":
                    case "Product":
                    case "Manufacturer":
                        return (string)val;

                    case "MacAddress":
                        if (objSMBIOS["MacAddress"] != null)
                            str.Append((string)objSMBIOS["MacAddress"] + ",");
                        break;
                    default:
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            return "";
        }

        if (str.Length > 0)
            str.Remove(str.Length - 1, 1);

        return str.ToString();
    }
}
