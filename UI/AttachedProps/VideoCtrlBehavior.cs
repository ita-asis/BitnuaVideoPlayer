using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;
using Vlc.DotNet.Wpf;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public class VideoCtrlBehavior : Behavior<VlcControl>
    {
        private Vlc.DotNet.Forms.VlcControl Player => AssociatedObject?.MediaPlayer;
        protected override void OnAttached()
        {
            RegisterEvents();
            InitPlayer();
            base.OnAttached();
        }

        private void RegisterEvents()
        {
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
        }

        private void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (!isVisible)
            {
                Player.Stop();
            }
            else
            {
                AutoPlay();
            }
        }

        public VideoSource Source
        {
            get { return (VideoSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(VideoSource), typeof(VideoCtrlBehavior), new PropertyMetadata(default(VideoSource), OnSourceChanged));



        private void InitPlayer()
        {
            Player.VlcLibDirectoryNeeded += OnVlcControlNeedsLibDirectory;
            Player.VlcMediaplayerOptions = new[] { "-I rc", "--rc-quiet" }; //"-I dummy","--dummy-quiet"
            Player.EndInit();

            // This can also be called before EndInit
            //player.Log += (sender, args) =>
            //{
            //    System.Diagnostics.Debug.WriteLine(string.Format("libVlc : {0} {1} @ {2}", args.Level, args.Message, args.Module));
            //};
            AutoPlay();
        }

        private void AutoPlay()
        {
            if (!string.IsNullOrWhiteSpace(Source.Path))
            {
                PlayerPlay(Player, Source);
            }
        }

        private static void PlayerPlay(Vlc.DotNet.Forms.VlcControl player, VideoSource source)
        {
            if (player == null || string.IsNullOrEmpty(source.Path))
                return;

            player.Play(new Uri(source.Path));
            MutePlayer(player);
            if (source.Time != null)
            {
                player.Time = source.Time.Value;
            }
        }

        private static void MutePlayer(Vlc.DotNet.Forms.VlcControl player)
        {
            if (player != null && !player.Audio.IsMute)
            {
                player.Audio.ToggleMute();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var player = ((VideoCtrlBehavior)d).Player;
            var source = (VideoSource)e.NewValue;
            PlayerPlay(player, source);
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
