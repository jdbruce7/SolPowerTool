﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;

namespace SolPowerTool.App.Common
{
    public abstract class DTOBase : PropertyChangedBase , IDirtyTracking, IComparable
    {
        public static event EventHandler AnyDirtyChanged;
        public event EventHandler DirtyChanged;
        private bool _isDirty;

        public virtual bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                if (_isDirty == value)
                    return;
                _isDirty = value;
                RaisePropertyChanged(() => IsDirty);
                if (AnyDirtyChanged != null)
                    AnyDirtyChanged(this, EventArgs.Empty);
                FireDirtyChanged();
            }
        }

        public void FireDirtyChanged()
        {
            if (DirtyChanged != null)
                DirtyChanged(this, EventArgs.Empty);
        }

        protected override void RaisePropertyChanged(PropertyInfo propertyInfo)
        {
            object[] propertyAttributes = propertyInfo.GetCustomAttributes(typeof (DirtyTrackingAttribute), true);
            if (propertyAttributes.Length > 0)
                IsDirty = true;
            base.RaisePropertyChanged(propertyInfo);
        }

        #region Implementation of IComparable

        public abstract int CompareTo(object obj);

        #endregion
    }
    public interface IDirtyTracking
    {
        event EventHandler DirtyChanged;
    }
}