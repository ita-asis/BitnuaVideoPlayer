using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace BitnuaVideoPlayer
{
    public class UserSettings
    {

        private UserSettings()
        {
            if (Properties.Settings.Default.UpgradeRequired)
            {
                checkUpgrade();
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
        }

        private void checkUpgrade()
        {
            const string strongAppName = "BitnuaVideoPlayer.exe_StrongName_lgujmbe4zuxqp45onbgara1o5b41v3jb";

            string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BitnuaVideoPlayer", strongAppName);
            string currConf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(currConf));
            if (!Directory.Exists(appPath))
                File.WriteAllText(currConf, Properties.Resources.UserConfTemplate);
            else
            {
                var v_dirs = Directory.GetDirectories(appPath);
                var version = GetVersion();
                var currVerDir = v_dirs.SingleOrDefault(d => d.Contains(version));
                if (string.IsNullOrEmpty(currVerDir))
                {
                    var lastVer = v_dirs.OrderByDescending(d => Version.Parse(new DirectoryInfo(d).Name)).First();
                    string prevConf = Path.Combine(lastVer, "user.config");

                    File.Copy(prevConf, currConf, true);
                }
            }
            Properties.Settings.Default.Reload();
        }

        private static string GetVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var productVersion = assembly.GetName().Version;
            var version = string.Format("{0}.{1}.{2}", productVersion.Major, productVersion.Minor, productVersion.Build);
            return version;
        }

        private static UserSettings s_instance;
        private static object s_lock = new object();
        public static UserSettings Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new UserSettings();
                        }
                    }
                }

                return s_instance;
            }
        }

        public static void Set(string key, object value)
        {
            Instance.SetValue(key, value);
        }

        public void SetValue(string key, object value)
        {
            Properties.Settings.Default[key] = value;
            Properties.Settings.Default.Save();
        }


        public static object Get(string key)
        {
            return Instance.GetValue(key);
        }
        public object GetValue(string key)
        {
            return Properties.Settings.Default[key];
        }
    }
}
