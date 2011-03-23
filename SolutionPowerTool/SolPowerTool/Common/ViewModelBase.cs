using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Globalization;
using System.Reflection;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Regions;
using SolPowerTool.App.Interfaces;

namespace SolPowerTool.App.Common
{
    public abstract class ViewModelBase<TView> : PropertyChangedBase, IViewModel<TView>
        where TView : class, IView<TView>
    {
        private TView _view;
        private Lazy<TView> _viewExport;

#pragma warning disable 649
        [Import]
        private Lazy<CompositionContainer> _container;
        [Import]
        private Lazy<IEventAggregator> _eventAggregator;
        [Import]
        private Lazy<IRegionManager> _regionManager;
#pragma warning restore 649

        public CompositionContainer Container
        {
            get { return _container.Value; }
        }

        public IRegionManager RegionManager
        {
            get { return _regionManager.Value; }
        }

        public IEventAggregator EventAggregator
        {
            get { return _eventAggregator.Value; }
        }



        #region IViewModel<TView> Members

        public virtual TView View
        {
            get
            {
                if (_view != null) return _view;
                _viewExport = Container.GetExport<TView>();
                _view = _viewExport.Value;
                _view.SetViewModel(this);
                return _view;
            }
        }

        #endregion

        #region Implementation of IDisposable

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ViewModelBase()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            OnDispose(disposing);
            if (disposing && !_disposed)
            {
                if (_view != null)
                {
                    _view.DataContext = null;
                    MethodInfo method = _view.GetType().GetMethod("Close");
                    if (method != null)
                        method.Invoke(_view, BindingFlags.Default, null, new object[] { }, CultureInfo.CurrentUICulture);
                    Container.ReleaseExport(_viewExport);
                }
                _view = null;
            }
            _disposed = true;
        }

        protected virtual void OnDispose(bool disposing)
        {
        }

        #endregion

    }
}