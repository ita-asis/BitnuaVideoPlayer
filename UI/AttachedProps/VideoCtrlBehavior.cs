using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
                Stop();
            else
                Start();
        }

        protected virtual void Start()
        {
            AutoPlay();
        }

        protected virtual void Stop()
        {
            Player.Stop();
        }

        public VideoSource Source
        {
            get { return (VideoSource)GetValue(SourceProperty); }
            set
            {
                SetValue(SourceProperty, value);
            }
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

        protected void AutoPlay()
        {
            if (!string.IsNullOrWhiteSpace(Source?.Path))
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

        protected static void MutePlayer(Vlc.DotNet.Forms.VlcControl player)
        {
            if (player != null && !player.Audio.IsMute)
            {
                player.Audio.ToggleMute();
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behaviour = ((VideoCtrlBehavior)d);
            behaviour.Play();
        }

        protected void Play(VlcControl player, VideoSource video)
        {
            Play(player.MediaPlayer, video);
        }

        protected void Play(Vlc.DotNet.Forms.VlcControl player = null, VideoSource source = null)
        {
            PlayerPlay(player ?? Player, source ?? Source);
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

    public class VideoDirPlayerBehaviour : VideoCtrlBehavior
    {
        protected override void OnAttached()
        {
            base.OnAttached();

        }

        #region Props
        public int Delay
        {
            get { return (int)GetValue(DelayProperty); }
            set { SetValue(DelayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Delay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DelayProperty =
            DependencyProperty.Register("Delay", typeof(int), typeof(VideoDirPlayerBehaviour), new PropertyMetadata(10000));

        public List<VideoSource> Videos
        {
            get { return (List<VideoSource>)GetValue(VideosProperty); }
            set { SetValue(VideosProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Videos.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideosProperty =
            DependencyProperty.Register("Videos", typeof(List<VideoSource>), typeof(VideoDirPlayerBehaviour), new PropertyMetadata(null, OnVideosChanged));

        #endregion

        private CancellationTokenSource m_LoopToken;

        protected override void Stop()
        {
            m_LoopToken?.Cancel();
            base.Stop();
        }

        private static async void OnVideosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = d as VideoDirPlayerBehaviour;

            @this.Stop();
            @this.m_LoopToken = new CancellationTokenSource();
            var token = @this.m_LoopToken.Token;
            if (@this.Videos != null)
            {
                await @this.StartVideoTask(@this.AssociatedObject, @this.Videos, @this.Delay, token);
            }
        }

        private async Task StartVideoTask(VlcControl player, List<VideoSource> videos, int delay, CancellationToken token)
        {
            var playTime = DateTime.Now;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    VideoSource currVideo = default(VideoSource);
                    foreach (var video in videos.IterateLoop(token))
                    {
                        if (video != currVideo)
                        {
                            currVideo = video;
                            long time = CalcLastPlayTime(playTime, video.Path);
                            Play(player, video);
                        }

                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private static long CalcLastPlayTime(DateTime playTime, string videoPath)
        {
            long time = 0;
            TimeSpan videoDuration;
            if (Ex.GetDuration(videoPath, out videoDuration))
            {
                var diff = (DateTime.Now - playTime).TotalMilliseconds;
                time = (long)(diff % TimeSpan.FromTicks(videoDuration.Ticks).TotalMilliseconds);
            }

            return time;
        }
    }

    public static class Ex
    {
        public static IEnumerable<T> IterateLoop<T>(this IEnumerable<T> source, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var videos = source.GetEnumerator())
                    while (videos.MoveNext() && !token.IsCancellationRequested)
                        yield return videos.Current;
            }
        }

        public static bool GetDuration(string filename, out TimeSpan duration)
        {
            try
            {
                using (var shell = ShellObject.FromParsingName(filename))
                {
                    IShellProperty prop = shell.Properties.System.Media.Duration;
                    var t = (ulong)prop.ValueAsObject;
                    duration = TimeSpan.FromTicks((long)t);
                }
                return true;
            }
            catch (Exception)
            {
                duration = new TimeSpan();
                return false;
            }
        }
    }
}
