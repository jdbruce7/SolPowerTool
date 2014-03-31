using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;
using SolPowerTool.App.Interfaces.Views;

namespace SolPowerTool.App.ViewModels
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DirtyReadonlyPromptViewModel : ViewModelBase<IDirtyReadonlyPromptView>, IDirtyReadonlyPromptViewModel
    {
        #region Bindings

        public IEnumerable<IFileAction> Projects { get; set; }

        #region Commands

        private ICommand _cancelCommand;
        private ICommand _checkoutCommand;
        private ICommand _makeWriteableCommand;

        public ICommand MakeWriteableCommand
        {
            get
            {
                return _makeWriteableCommand ?? (_makeWriteableCommand = new RelayCommand<object>(param =>
                                                                                                      {
                                                                                                          Result = DirtyReadonlyPromptResults.MakeWriteable;
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
                                                                                                Result = DirtyReadonlyPromptResults.Checkout;
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
                                                                                            Result = DirtyReadonlyPromptResults.Cancel;
                                                                                            View.Close();
                                                                                        }));
            }
        }

        #endregion

        #endregion

        #region IDirtyReadonlyPromptViewModel Members

        public DirtyReadonlyPromptResults Result { get; private set; }

        public bool? ShowDialog()
        {
            return View.ShowDialog();
        }

        #endregion
    }
}