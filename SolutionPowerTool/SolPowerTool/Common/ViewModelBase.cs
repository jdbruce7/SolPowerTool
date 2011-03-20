using System;

namespace SolPowerTool.App.Common
{
    public abstract class ViewModelBase : PropertyChangedBase, IDisposable
    {
        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        ~ViewModelBase()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            OnDispose(disposing);
        }

        protected virtual void OnDispose(bool disposing)
        {
        }
    }
}