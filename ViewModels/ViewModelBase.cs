using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BitnuaVideoPlayer
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => this.OnPropertyChanged(propertyName, PropertyChanged);
        //protected virtual void OnPropertyChanged<T>(Expression<Func<T>> selectorExpression) => this.OnPropertyChanged(selectorExpression, PropertyChanged);
    }

    public static class ViewModelBaseEx
    {
        public static void OnPropertyChanged<T>(this object sender, Expression<Func<T>> selectorExpression, PropertyChangedEventHandler propertyChanged)
        {
            if (selectorExpression == null)
                throw new ArgumentNullException("selectorExpression");
            MemberExpression body = selectorExpression.Body as MemberExpression;
            if (body == null)
                throw new ArgumentException("The body must be a member expression");
            OnPropertyChanged(sender, body.Member.Name, propertyChanged);
        }

        public static void OnPropertyChanged(this object sender, string propertyName, PropertyChangedEventHandler propertyChanged)
        {
            propertyChanged?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }
    }
}
