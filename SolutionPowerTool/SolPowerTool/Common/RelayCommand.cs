using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace SolPowerTool.App.Common
{
    internal class RelayCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;
        private EventHandler _canExecuteChanged;
        private bool _lastCanExexute = true;

        /// <summary>
        ///     Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        private bool _LastCanExecute
        {
            set
            {
                if (_lastCanExexute == value) return;
                _lastCanExexute = value;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        ///     Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _canExecuteChanged += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                _canExecuteChanged -= value;
            }
        }

        /// <summary>
        ///     Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || (_LastCanExecute = _canExecute());
        }

        /// <summary>
        ///     Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        public void Execute(object parameter)
        {
            //failsafe to avoid having to retype the can execute in each command 
            //and still feel warm and comfy our intent is respected
            if (CanExecute(parameter))
                _execute();
        }

        /// <summary>
        ///     Raises the <see cref="CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (_canExecuteChanged != null)
            {
                Delegate[] delegates = _canExecuteChanged.GetInvocationList();
                // Walk thru invocation list
                foreach (EventHandler handler in delegates)
                {
                    var dispatcher = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcher != null && dispatcher.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcher.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, EventArgs.Empty);
                    }
                    else // Execute handler as is
                        handler(this, EventArgs.Empty);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }

    internal class RelayCommand<T> : ICommand where T : class
    {
        private readonly Func<T, bool> _canExecute;
        private readonly Action<T> _execute;
        private EventHandler _canExecuteChanged;
        private bool _lastCanExexute = true;

        /// <summary>
        ///     Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        private bool _LastCanExecute
        {
            set
            {
                if (_lastCanExexute == value) return;
                _lastCanExexute = value;
                RaiseCanExecuteChanged();
            }
        }

        void ICommand.Execute(object parameter)
        {
            Execute(parameter as T);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute(parameter as T);
        }

        /// <summary>
        ///     Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                _canExecuteChanged += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                _canExecuteChanged -= value;
            }
        }

        /// <summary>
        ///     Raises the <see cref="CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (_canExecuteChanged != null)
            {
                Delegate[] delegates = _canExecuteChanged.GetInvocationList();
                // Walk thru invocation list
                foreach (EventHandler handler in delegates)
                {
                    var dispatcher = handler.Target as DispatcherObject;
                    // If the subscriber is a DispatcherObject and different thread
                    if (dispatcher != null && dispatcher.CheckAccess() == false)
                    {
                        // Invoke handler in the target dispatcher's thread
                        dispatcher.Dispatcher.Invoke(DispatcherPriority.DataBind, handler, this, EventArgs.Empty);
                    }
                    else // Execute handler as is
                        handler(this, EventArgs.Empty);
                }
            }
            CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        ///     Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(T parameter)
        {
            return _canExecute == null || (_LastCanExecute = _canExecute(parameter));
        }

        /// <summary>
        ///     Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        public void Execute(T parameter)
        {
            //failsafe to avoid having to retype the can execute in each command 
            //and still feel warm and comfy our intent is respected
            if (CanExecute(parameter))
                _execute(parameter);
        }
    }
}