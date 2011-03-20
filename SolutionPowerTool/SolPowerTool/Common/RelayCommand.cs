using System;
using System.Windows.Input;

namespace SolPowerTool.App.Common
{
    public class RelayCommand<T> : ICommand where T : class
    {
        private readonly Action<T> _action;
        private readonly Func<T, bool> _predicate;
        private bool _lastCanExexute = true;

        public RelayCommand(Action<T> action, Func<T, bool> predicate = null)
        {
            _action = action;
            _predicate = predicate;
        }

        private bool _LastCanExecute
        {
            set
            {
                if (_lastCanExexute == value) return;
                _lastCanExexute = value;
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, EventArgs.Empty);
            }
        }

        #region ICommand Members

        public void Execute(object parameter)
        {
            _action(parameter as T);
        }

        public bool CanExecute(object parameter)
        {
            return _predicate != null ? (_LastCanExecute = _predicate(parameter as T)) : (_LastCanExecute = true);
        }

        public event EventHandler CanExecuteChanged;

        #endregion
    }
}