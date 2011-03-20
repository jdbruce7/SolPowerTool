using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SolPowerTool.App.Common
{
    public class DirtyTrackingCollection<T> : ObservableCollection<T>, IDirtyTracking
        where T : IDirtyTracking
    {
        public event EventHandler DirtyChanged;

        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems.OfType<T>())
                        item.DirtyChanged += _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.OfType<T>())
                        item.DirtyChanged -= _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems.OfType<T>())
                        item.DirtyChanged -= _dirtyChanged;
                    foreach (var item in e.NewItems.OfType<T>())
                        item.DirtyChanged += _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var item in Items)
                        item.DirtyChanged -= _dirtyChanged;
                    break;
                case NotifyCollectionChangedAction.Move:
                default:
                    break;
            }
            base.OnCollectionChanged(e);
        }

        void _dirtyChanged(object sender, EventArgs e)
        {
            if (DirtyChanged != null)
                DirtyChanged(this, EventArgs.Empty);
        }
    }
}
