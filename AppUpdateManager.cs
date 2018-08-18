﻿using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitnuaVideoPlayer
{
    public class AppUpdateManager
    {
        private static AppUpdateManager s_instance;
        private static object s_lock = new object();
        private AppUpdateManager()
        {
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) => DisposeUpdateManager();

            var t = checkForUpdates();
        }

        public static AppUpdateManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new AppUpdateManager();
                        }
                    }
                }

                return s_instance;
            }
        }

        private static async Task checkForUpdates()
        {
            try
            {
                using (s_UpdateManager = await getUpdateManager())
                {
                    var result = await s_UpdateManager.UpdateApp(OnVersionUpdateProgressChanged);
                    if (result != null)
                    {
                        UpdateManager.RestartApp();
                    }
                }
            }
            catch (Exception ex)
            {
                MainWindow.LogException(ex);
            }
        }

        private static async Task<Squirrel.UpdateManager> getUpdateManager()
        {
            return await UpdateManager.GitHubUpdateManager("https://github.com/ita-asis/BitnuaVideoPlayer");
        }

        private static int _isUpdateManagerDisposed = 1;
        internal static void DisposeUpdateManager()
        {
            WaitForCheckForUpdateLockAcquire();

            if (1 == Interlocked.Exchange(ref _isUpdateManagerDisposed, 0))
            {
                if (s_UpdateManager != null)
                {
                    s_UpdateManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Workaround for exception throw on SingleGlobalInstance destructor call.
        /// Before app close we should wait for 2 seconds before SingleGlobalInstance will be disposed
        /// </summary>
        private static void WaitForCheckForUpdateLockAcquire()
        {
            var goTime = _lastUpdateCheckDateTime + TimeSpan.FromMilliseconds(2000);
            var timeToWait = goTime - DateTime.Now;
            if (timeToWait > TimeSpan.Zero)
            {
                Task.Delay(timeToWait).Wait();
            }
        }
        private static DateTime _lastUpdateCheckDateTime = DateTime.Now - TimeSpan.FromDays(1);
        private static UpdateManager s_UpdateManager;

        private static async Task<UpdateInfo> CheckForUpdate(bool ignoreDeltaUpdates)
        {
            _lastUpdateCheckDateTime = DateTime.Now;
            return await s_UpdateManager.CheckForUpdate(ignoreDeltaUpdates);
        }


        public static event Action<int> VersionUpdateProgressChanged;
        public static void OnVersionUpdateProgressChanged(int progress)
        {
            if (VersionUpdateProgressChanged != null)
            {
                VersionUpdateProgressChanged.Invoke(progress);
            }
        }
    }
}
