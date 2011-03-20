using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SolPowerTool.App.Common
{
    public class DirtyTrackingCollection<T> : ObservableCollection<T>, IDirtyTracking
        where T : IDirtyTracking
    {
        #region IDirtyTracking Members

        public event EventHandler DirtyChanged;

        #endregion

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (T item in e.NewItems.OfType<T>())
                        item.DirtyChanged += _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (T item in e.OldItems.OfType<T>())
                        item.DirtyChanged -= _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (T item in e.OldItems.OfType<T>())
                        item.DirtyChanged -= _dirtyChanged;
                    foreach (T item in e.NewItems.OfType<T>())
                        item.DirtyChanged += _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (T item in Items)
                        item.DirtyChanged -= _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
            base.OnCollectionChanged(e);
        }

        private void _dirtyChanged(object sender, EventArgs e)
        {
            if (DirtyChanged != null)
                DirtyChanged(this, EventArgs.Empty);
        }
    }
}