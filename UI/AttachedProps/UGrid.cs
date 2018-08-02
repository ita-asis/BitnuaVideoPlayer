using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BitnuaVideoPlayer.UI.AttachedProps
{
    public static class UGrid
    {
        public static int GetRows(DependencyObject obj)
        {
            return (int)obj.GetValue(RowsProperty);
        }

        public static void SetRows(DependencyObject obj, int value)
        {
            obj.SetValue(RowsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Rows.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.RegisterAttached("Rows", typeof(int), typeof(UGrid), new PropertyMetadata(1));


        public static int GetCols(DependencyObject obj)
        {
            return (int)obj.GetValue(ColsProperty);
        }

        public static void SetCols(DependencyObject obj, int value)
        {
            obj.SetValue(ColsProperty, value);
        }

        // Using a DependencyProperty as the backing store for Cols.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColsProperty =
            DependencyProperty.RegisterAttached("Cols", typeof(int), typeof(UGrid), new PropertyMetadata(1));


    }
}
