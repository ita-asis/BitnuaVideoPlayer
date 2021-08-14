using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
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
using System.Windows.Input;
using System.Windows.Interactivity;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public class VideoCtrlBehavior : Behavior<VideoView>
    {
        protected VideoView PlayerCtrl => AssociatedObject;
        protected override void OnAttached()
        {
            RegisterEvents();
            InitPlayer();
            base.OnAttached();
        }

        private void RegisterEvents()
        {
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;


            var binding = new CommandBinding(AppCommands.PlayToggleCommand, TogglePause, CanPause);
            CommandManager.RegisterClassCommandBinding(typeof(Window), binding);

            binding = new CommandBinding(AppCommands.StopCommand, DoStop, CanStop);
            CommandManager.RegisterClassCommandBinding(typeof(Window), binding);
        }

        private void DoStop(object sender, ExecutedRoutedEventArgs e)
        {
            Stop();
        }

        private void TogglePause(object sender, ExecutedRoutedEventArgs e)
        {
            if (PlayerCtrl?.MediaPlayer?.CanPause ?? false)
                PlayerCtrl?.MediaPlayer?.Pause();
            else
                Play();
        }

        private void CanPause(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CanStop(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = PlayerCtrl?.MediaPlayer?.CanPause ?? false;
        }

        private void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (!isVisible)
                Stop();
            else
                Play();
        }

        protected virtual void Play()
        {
            AutoPlay();
        }

        protected virtual void Stop()
        {
            PlayerCtrl?.MediaPlayer?.Stop();
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

        public bool MuteSound
        {
            get
            {
                return (bool)GetValue(MuteSoundProperty);
            }
            set
            {
                SetValue(MuteSoundProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MuteSoundProperty =
            DependencyProperty.Register("MuteSound", typeof(bool), typeof(VideoCtrlBehavior), new PropertyMetadata(true, OnMuteSoundChanged));


        private void InitPlayer()
        {
            PlayerCtrl.MediaPlayer = new MediaPlayer(App.Instance.LibVLC);
            PlayerCtrl.SizeChanged += (o, e) => PlayerCtrl.MediaPlayer.AspectRatio = $"{e.NewSize.Width}:{e.NewSize.Height}";
            AutoPlay();
        }

        protected void AutoPlay()
        {
            if (!string.IsNullOrWhiteSpace(Source?.Path))
            {
                PlayerPlay(PlayerCtrl.MediaPlayer, Source);
            }
        }

        private void PlayerPlay(MediaPlayer player, VideoSource source)
        {
            if (player == null || string.IsNullOrEmpty(source.Path))
                return;

            try
            {
                var media = new Media(App.Instance.LibVLC, source.Path);


                if (MuteSound)
                    media.AddOption(":no-audio");
                player.Play(media);
                if (source.Time != null)
                {
                    player.Time = source.Time.Value;
                }
            }
            catch (Exception ex)
            {
                App.LogException(ex);
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behaviour = ((VideoCtrlBehavior)d);
            behaviour.Play();
        }

        private static void OnMuteSoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behaviour = ((VideoCtrlBehavior)d);

            if (behaviour.MuteSound)
                behaviour.PlayerCtrl?.MediaPlayer.SetAudioTrack(-1);
            else
                behaviour.PlayerCtrl?.MediaPlayer.SetAudioTrack(1);
        }

        protected void Play(MediaPlayer player = null, VideoSource source = null)
        {
            PlayerPlay(player ?? PlayerCtrl?.MediaPlayer, source ?? Source);
        }

        
    }

    public class VideoDirPlayerBehaviour : VideoCtrlBehavior
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            PlayerCtrl.MediaPlayer.EndReached += VlcMediaPlayer_EndReached;

            Play();
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            PlayerCtrl.MediaPlayer.EndReached -= VlcMediaPlayer_EndReached;
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

        private int m_CurrVideo;
        private int m_VideosCount;
        #endregion

        protected void Play(VideoView player, int videoId)
        {
            if (player != null)
                player.Dispatcher.BeginInvoke((Action)(() => Play(player?.MediaPlayer, Videos[videoId])));
        }

        protected override void Stop()
        {
            base.Stop();
        }

        private static void OnVideosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = d as VideoDirPlayerBehaviour;
            if (@this.AssociatedObject == null)
                return;

            @this.Play();
        }

        protected override void Play()
        {
            Stop();
            if (Videos == null || Videos.Count == 0)
                return;

            m_VideosCount = Videos.Count;
            m_CurrVideo = 0;
            Play(PlayerCtrl, m_CurrVideo);
        }

        private void VlcMediaPlayer_EndReached(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                m_CurrVideo = ++m_CurrVideo % m_VideosCount;
                Play(PlayerCtrl, m_CurrVideo);
            }));
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
