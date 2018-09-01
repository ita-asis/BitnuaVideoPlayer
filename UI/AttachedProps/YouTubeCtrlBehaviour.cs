using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public class YoutubeWebBrowser : CefSharp.Wpf.ChromiumWebBrowser
    {
       
    }

    public class YouTubeCtrlBehaviour : Behavior<YoutubeWebBrowser>
    {
        private const double c_Margin = 20;

        protected override void OnAttached()
        {
            AssociatedObject.Loaded += AssociatedObject_Loaded;
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            InitYoutubePlayer();
        }

        private void InitYoutubePlayer()
        {
            if (string.IsNullOrEmpty(YouTubeId) || AssociatedObject == null)
                return;

            var args = new Dictionary<string, string>()
            {
                { "{videoId}" , YouTubeId},
                { "{width}" , (AssociatedObject.ActualWidth - c_Margin).ToString()},
                { "{height}" , (AssociatedObject.ActualHeight - c_Margin).ToString()}
            };

            var sb = new StringBuilder(Properties.Resources.YouTubeTemplateHtml);

            foreach (var arg in args)
                sb.Replace(arg.Key, arg.Value);

            var html = sb.ToString();
            AssociatedObject.LoadHtml(html);
        }

        public string YouTubeId
        {
            get { return (string)GetValue(YouTubeIdProperty); }
            set { SetValue(YouTubeIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for YouTubeId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YouTubeIdProperty =
            DependencyProperty.Register("YouTubeId", typeof(string), typeof(YouTubeCtrlBehaviour), new PropertyMetadata(null, OnYoutubeIdChanged));

        private static void OnYoutubeIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((YouTubeCtrlBehaviour)d).InitYoutubePlayer();
        }
    }
}
