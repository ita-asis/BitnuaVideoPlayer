using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public class MarqueePanel: Behavior<StackPanel>
    {
        public double Speed
        {
            get { return (double)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Speed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(double), typeof(MarqueePanel), new PropertyMetadata(5d, OnPropsChanged));

        public double ActualWidthBind
        {
            get { return (double)GetValue(ActualWidthPropProperty); }
            set { SetValue(ActualWidthPropProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActualWidthProp.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActualWidthPropProperty =
            DependencyProperty.Register("ActualWidthBind", typeof(double), typeof(MarqueePanel), new PropertyMetadata(0d, OnPropsChanged));

        private static void OnPropsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dp = d as MarqueePanel;
            var animation = dp.CreateAnimation();
            dp.AssociatedObject.BeginAnimation(Canvas.LeftProperty, animation);
        }

        private DoubleAnimation CreateAnimation()
        {
            var width = AssociatedObject.ActualWidth;
            var parentWidth = (AssociatedObject.Parent as FrameworkElement).ActualWidth;
            var animation = new DoubleAnimation(-width, parentWidth, new Duration(TimeSpan.FromSeconds(Speed)));
            animation.RepeatBehavior = RepeatBehavior.Forever;

            return animation;
        }
    }
}
