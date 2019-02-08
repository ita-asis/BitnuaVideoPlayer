using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(MyPopup), new PropertyMetadata(false));

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

        }

        private double X
        {
            get { return (double)grid.GetValue(Canvas.LeftProperty); }
            set { grid.SetValue(Canvas.LeftProperty, value); }
        }
        private double Y
        {
            get { return (double)grid.GetValue(Canvas.TopProperty); }
            set { grid.SetValue(Canvas.TopProperty, value); }
        }

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
                grid.Width = xadjust;
                grid.Height = yadjust;
            }
        }
    }
}
