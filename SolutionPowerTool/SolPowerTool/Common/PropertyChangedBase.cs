using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace SolPowerTool.App.Common
{
    public abstract class PropertyChangedBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaisePropertyChanged(PropertyInfo propertyInfo)
        {
            RaisePropertyChanged(propertyInfo.Name);
        }

        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null) throw new ArgumentNullException("propertyExpression");
            if (propertyExpression.Body.NodeType == ExpressionType.MemberAccess)
            {
                var memberExpr = propertyExpression.Body as MemberExpression;
                var propertyInfo = memberExpr.Member as PropertyInfo;
                RaisePropertyChanged(propertyInfo);
            }
        }
    }
}