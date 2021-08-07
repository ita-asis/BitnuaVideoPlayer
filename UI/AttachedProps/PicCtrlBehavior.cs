using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SWC = System.Windows.Controls;
namespace BitnuaVideoPlayer.UI.AttachedProps
{

    public class PicCtrlBehavior : Behavior<SWC.Image>
    {
      
        // ---------------------
        private const int c_DefaultDelayMs = 3000;
        protected SWC.Image ImgCtrl => AssociatedObject;

        private DispatcherTimer m_timer;
        private int m_selectedPicIndex;
        private string[] m_pics;
        protected override void OnAttached()
        {
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
            base.OnAttached();

            ReadSource(ImageSource);
        }

        private void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (!isVisible)
                m_timer?.Stop();
        }


        public int DelayMS
        {
            get { return (int)GetValue(DelayMSProperty); }
            set
            {
                SetValue(DelayMSProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DelayMSProperty =
            DependencyProperty.Register("DelayMS", typeof(int), typeof(PicCtrlBehavior), new PropertyMetadata(c_DefaultDelayMs));

        public string ImageSource
        {
            get { return (string)GetValue(ImageSourceProperty); }
            set
            {
                SetValue(ImageSourceProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(string), typeof(PicCtrlBehavior), new PropertyMetadata(null, OnImageSourceChanged));


        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as PicCtrlBehavior;
            if (ctrl.AssociatedObject != null)
                ctrl.ReadSource((string)e.NewValue);
        }

        private void ReadSource(string source)
        {
            if (File.Exists(source))
            {
                m_timer?.Stop();
                ImgCtrl.Source = BuildSource(source);
            }
            else if (Directory.Exists(source))
            {
                m_pics = Directory.GetFiles(source, "*", SearchOption.AllDirectories);

                if (m_pics.Length > 0)
                {
                    m_timer?.Stop();
                    m_selectedPicIndex = 0;
                    ImgCtrl.Source = BuildSource(m_pics[0]);
                    startTimer();
                }
            }
        }

        private void startTimer()
        {
            if (m_timer is null)
            {
                m_timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(DelayMS)
                };

                m_timer.Tick += timerTick; ;
            }
            m_timer.Start();
        }

        private void timerTick(object sender, EventArgs e)
        {
            m_selectedPicIndex++;
            ImgCtrl.Source = BuildSource(m_pics[m_selectedPicIndex % m_pics.Length]);
        }

        private static BitmapImage BuildSource(string picPath)
        {
            return new BitmapImage(new Uri(picPath));
        }
    }
}
