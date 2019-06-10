﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BitnuaVideoPlayer.UI
{
    /// <summary>
    /// Interaction logic for Clockgrid.xaml
    /// </summary>
    [TemplatePart(Name = "PART_moveThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_resizeThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_grid", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_canvas", Type = typeof(Canvas))]

    public partial class MyPopup : UserControl
    {
        private Thumb moveThumb;
        private Thumb resizeThumb;
        private Grid grid;
        private Canvas canvas;

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(MyPopup), new PropertyMetadata(propertyChanged));

        private static void propertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyPopup popup)
            {
                if (e.Property == IsOpenProperty)
                    popup.updateVisibility();
                else if (e.Property == XProperty)
                    popup.X = (double)e.NewValue;
                else if (e.Property == YProperty)
                    popup.Y = (double)e.NewValue;
                else if (e.Property == ClockHeightProperty)
                    popup.ClockHeight = (double)e.NewValue;
                else if (e.Property == ClockWidthProperty)
                    popup.ClockWidth = (double)e.NewValue;
            }
        }

        private void updateVisibility()
        {
            this.Visibility = this.IsOpen ? Visibility.Visible : Visibility.Hidden;
        }

        public MyPopup()
        {
            InitializeComponent();
            MouseDown += (s, e) => moveThumb.RaiseEvent(e);
            MouseEnter += (s, e) => resizeThumb.Visibility = Visibility.Visible;
            MouseLeave += (s, e) => resizeThumb.Visibility = Visibility.Hidden;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            moveThumb = this.GetTemplateChild("PART_moveThumb") as Thumb;
            resizeThumb = this.GetTemplateChild("PART_resizeThumb") as Thumb;
            grid = this.GetTemplateChild("PART_grid") as Grid;
            canvas = this.GetTemplateChild("PART_canvas") as Canvas;
            updateVisibility();
        }

        public double X
        {
            get { return (double)grid.GetValue(Canvas.LeftProperty); }
            set
            {
                SetValue(XProperty, value);
                grid.SetValue(Canvas.LeftProperty, value);
            }
        }

        public double Y
        {
            get { return (double)grid.GetValue(Canvas.TopProperty); }
            set
            {
                SetValue(YProperty, value);
                grid.SetValue(Canvas.TopProperty, value);
            }
        }

        public double ClockWidth
        {
            get { return (double)grid.GetValue(Canvas.WidthProperty); }
            set
            {
                SetValue(ClockWidthProperty, value);
                grid.SetValue(Canvas.WidthProperty, value);
            }
        }

        public double ClockHeight
        {
            get { return (double)grid.GetValue(Canvas.HeightProperty); }
            set
            {
                SetValue(ClockHeightProperty, value);
                grid.SetValue(Canvas.HeightProperty, value);
            }
        }

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(MyPopup),
                new FrameworkPropertyMetadata(0.0, 
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | 
                    FrameworkPropertyMetadataOptions.AffectsRender, propertyChanged));
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(MyPopup),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
                    FrameworkPropertyMetadataOptions.AffectsRender, propertyChanged));
        public static readonly DependencyProperty ClockHeightProperty =
            DependencyProperty.Register("ClockHeight", typeof(double), typeof(MyPopup),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
                    FrameworkPropertyMetadataOptions.AffectsRender, propertyChanged));
        public static readonly DependencyProperty ClockWidthProperty =
            DependencyProperty.Register("ClockWidth", typeof(double), typeof(MyPopup),
                new FrameworkPropertyMetadata(0.0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault |
                    FrameworkPropertyMetadataOptions.AffectsRender, propertyChanged));

        private void onDragMoveDelta(object sender, DragDeltaEventArgs e)
        {
            var p = new Point(X + e.HorizontalChange, Y + e.VerticalChange);
            Y = p.Y;
            X = p.X;
        }

        private void onDragResizeDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = grid.Height + e.VerticalChange;
            double xadjust = grid.Width + e.HorizontalChange;
            if ((xadjust >= grid.MinWidth) && (yadjust >= grid.MinHeight))
            {
                ClockWidth = xadjust;
                ClockHeight = yadjust;
            }
        }

        private void PART_canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mpoint = e.GetPosition(this);

            // check out of clock bounds
            if (mpoint.X < this.X ||
                mpoint.Y < this.Y ||
                mpoint.X > this.X + this.ClockWidth ||
                mpoint.Y > this.Y + this.ClockHeight)
            {
                e.Handled = true;
                var parent = this.FindParentOfType<Window>();
                //Copy over event arg members and raise it
                MouseButtonEventArgs newarg = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp,
                                                  e.ChangedButton, e.StylusDevice);
                newarg.RoutedEvent = ListViewItem.MouseDownEvent;
                newarg.Source = sender;
                parent.RaiseEvent(newarg);
            }
        }
    }

    public static class Ex
    {
        public static T FindParentOfType<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentDepObj = child;
            do
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
                T parent = parentDepObj as T;
                if (parent != null) return parent;
            }
            while (parentDepObj != null);
            return null;
        }
    }
}
