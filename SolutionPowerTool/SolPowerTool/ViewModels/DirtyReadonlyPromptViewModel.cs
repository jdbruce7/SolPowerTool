using System.Collections.Generic;
using System.Windows.Input;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Views;

namespace SolPowerTool.App.ViewModels
{
    public class DirtyReadonlyPromptViewModel : ViewModelBase
    {
        #region Results enum

        public enum Results
        {
            Cancel,
            MakeWriteable,
            Checkout
        }

        #endregion

        private ICommand _cancelCommand;
        private ICommand _checkoutCommand;
        private ICommand _makeWriteableCommand;

        public DirtyReadonlyPromptViewModel(IEnumerable<Project> projects)
        {
            Projects = projects;
            View = new DirtyReadonlyPromptView();
            View.ViewModel = this;
        }

        public IEnumerable<Project> Projects { get; private set; }

        public DirtyReadonlyPromptView View { get; set; }

        public ICommand MakeWriteableCommand
        {
            get
            {
                return _makeWriteableCommand ?? (_makeWriteableCommand = new RelayCommand<object>(param =>
                                                                                                      {
                                                                                                          Result = Results.MakeWriteable;
                                                                                                          View.Close();
                                                                                                      }));
            }
        }

        public ICommand CheckoutCommand
        {
            get
            {
                return _checkoutCommand ?? (_checkoutCommand = new RelayCommand<object>(param =>
                                                                                            {
                                                                                                Result = Results.Checkout;
                                                                                                View.Close();
                                                                                            }));
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new RelayCommand<object>(param =>
                                                                                        {
                                                                                            Result = Results.Cancel;
                                                                                            View.Close();
                                                                                        }));
            }
        }

        public Results Result { get; set; }

        public void ShowDialog()
        {
            View.ShowDialog();
        }
    }
}