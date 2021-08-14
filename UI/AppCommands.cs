using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BitnuaVideoPlayer.UI
{
    public static class AppCommands
    {
        public static RoutedUICommand PlayToggleCommand { get; } = new RoutedUICommand("Play / Pause", "PlayToggleCommand", typeof(AppCommands));
        public static RoutedUICommand StopCommand { get; } = new RoutedUICommand("Stop playback", "StopCommand", typeof(AppCommands));
    }
}
