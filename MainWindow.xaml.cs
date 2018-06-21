using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace BitnuaVideoPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitPlayer();
        }

        private void InitPlayer()
        {
            var player = MyControl.MediaPlayer;
            player.VlcLibDirectoryNeeded += OnVlcControlNeedsLibDirectory;
            player.EndInit();

            // This can also be called before EndInit
            player.Log += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine(string.Format("libVlc : {0} {1} @ {2}", args.Level, args.Message, args.Module));
            };

            player.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi"));
        }

        private void OnVlcControlNeedsLibDirectory(object sender, Vlc.DotNet.Forms.VlcLibDirectoryNeededEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory == null)
                return;
            if (IntPtr.Size == 4)
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x86\"));
            else
                e.VlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, @"libvlc\win-x64\"));
        }
    }
}
