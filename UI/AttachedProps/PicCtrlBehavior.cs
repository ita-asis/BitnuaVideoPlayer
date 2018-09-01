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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vlc.DotNet.Wpf;
using SWC = System.Windows.Controls;
namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public static class PicCtrlBehavior 
    {
        private const int c_DefaultDelayMs = 3000;

        public static string GetImageSource(DependencyObject obj)
        {
            return (string)obj.GetValue(ImageSourceProperty);
        }

        public static void SetImageSource(DependencyObject obj, string value)
        {
            obj.SetValue(ImageSourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.RegisterAttached("ImageSource", typeof(string), typeof(PicCtrlBehavior), new PropertyMetadata(null, OnImageSourceChanged));



        public static int GetDelayMS(DependencyObject obj)
        {
            return (int)obj.GetValue(DelayMSProperty);
        }

        public static void SetDelayMS(DependencyObject obj, int value)
        {
            obj.SetValue(DelayMSProperty, value);
        }

        // Using a DependencyProperty as the backing store for DelayMS.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DelayMSProperty =
            DependencyProperty.RegisterAttached("DelayMS", typeof(int), typeof(PicCtrlBehavior), new PropertyMetadata(c_DefaultDelayMs));

        private static async void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var cts = new CancellationTokenSource();
            var ctrl = d as SWC.Image;
            RegisterEvents(ctrl, cts);
            await Init(ctrl, (string)e.NewValue, cts.Token);
        }

        private static void RegisterEvents(FrameworkElement frameworkElement, CancellationTokenSource cts)
        {
            DependencyPropertyChangedEventHandler onVisibleChanged = (object sender, DependencyPropertyChangedEventArgs e) =>
            {
                if (!(bool)e.NewValue)
                {
                    cts.Cancel();
                }
            };

            frameworkElement.IsVisibleChanged -= onVisibleChanged;
            frameworkElement.IsVisibleChanged += onVisibleChanged;
        }

        private static async Task Init(SWC.Image ctrl, string source, CancellationToken token)
        {
            if (File.Exists(source))
            {
                ctrl.Source = BuildSource(source);
            }
            else if (Directory.Exists(source))
            {
                var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                await StartPicTask(ctrl, files, token);
            }
        }

        private static async Task StartPicTask(SWC.Image ctrl, IEnumerable<string> images, CancellationToken token)
        {
            int delay = (int)ctrl.GetValue(DelayMSProperty);

            await Task.Delay(100);
            while (!token.IsCancellationRequested)
            {
                try
                {
                    foreach (var picPath in IterateLoop(images, token))
                    {
                        await ctrl.Dispatcher.BeginInvoke((Action)(() => ctrl.Source = BuildSource(picPath)));
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        private static BitmapImage BuildSource(string picPath)
        {
            return new BitmapImage(new Uri(picPath));
        }

        public static IEnumerable<T> IterateLoop<T>(this IEnumerable<T> source, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (var videos = source.GetEnumerator())
                    while (videos.MoveNext() && !token.IsCancellationRequested)
                        yield return videos.Current;
            }
        }
    }
}
