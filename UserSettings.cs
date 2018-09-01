using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
                checkUpgrade();
            }
        }

        private void checkUpgrade()
        {
                const string goodVersion = "1.0.1";
                string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BitnuaVideoPlayer");
                var v_dir = Directory.GetDirectories(appPath).FirstOrDefault(d => Directory.GetDirectories(d).Any(dir => dir.Contains(goodVersion)));
                if (!string.IsNullOrEmpty(v_dir))
                {
                    string v_conf = Path.Combine(v_dir, goodVersion, "user.config");
                    string currConf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

                    File.Copy(v_conf, currConf, true);
                    Properties.Settings.Default.Reload();
                }
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
