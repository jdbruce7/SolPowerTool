using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using SolPowerTool.App.Common;
using SolPowerTool.App.Views;

namespace SolPowerTool.App.ViewModels
{
    public class AboutBoxViewModel
    {
        private ICommand _showCopyrightCommand;
        private ICommand _showWarrantyCommand;

        public AboutBoxViewModel()
        {
            View = new AboutBoxView {ViewModel = this};

            Title = String.Format("About {0}", AssemblyTitle);
            ProductName = AssemblyProduct;
            Version = String.Format("Version {0}", AssemblyVersion);
            Copyright = AssemblyCopyright;
            CompanyName = AssemblyCompany;
            Description = AssemblyDescription;
        }

        public AboutBoxView View { get; private set; }

        public string Title { get; private set; }

        public string ProductName { get; private set; }

        public string Version { get; private set; }

        public string Copyright { get; private set; }

        public string CompanyName { get; private set; }

        public string Description { get; private set; }

        public ICommand ShowWarrantyCommand
        {
            get { return _showWarrantyCommand ?? (_showWarrantyCommand = new RelayCommand<object>(param => { MessageBox.Show("Warranty"); })); }
        }

        public ICommand ShowCopyrightCommand
        {
            get { return _showCopyrightCommand ?? (_showCopyrightCommand = new RelayCommand<object>(param => { MessageBox.Show("Copyright"); })); }
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    var titleAttribute = (AssemblyTitleAttribute) attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute) attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute) attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof (AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute) attributes[0]).Company;
            }
        }

        #endregion

        public bool? ShowDialog()
        {
            return View.ShowDialog();
        }
    }
}