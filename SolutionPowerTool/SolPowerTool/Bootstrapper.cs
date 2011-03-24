using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Practices.Prism.MefExtensions;
using Microsoft.Practices.Prism.Modularity;
using SolPowerTool.App.Interfaces.Shell;
using SolPowerTool.App.Properties;
using SolPowerTool.App.Shell;

namespace SolPowerTool.App
{
    public class Bootstrapper : MefBootstrapper
    {
        #region Overrides of Bootstrapper

        protected override IModuleCatalog CreateModuleCatalog()
        {
            if (!Directory.Exists(Settings.Default.ModulesFolder))
                Directory.CreateDirectory(Settings.Default.ModulesFolder);
            var directoryModuleCatalog = new DirectoryModuleCatalog {ModulePath = Settings.Default.ModulesFolder};
            directoryModuleCatalog.Load();
            return directoryModuleCatalog;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override CompositionContainer CreateContainer()
        {
            var catalog = new AggregateCatalog(
                new DirectoryCatalog(Settings.Default.ModulesFolder),
                new DirectoryCatalog("."),
                new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            var container = new CompositionContainer(catalog);
            return container;
        }

        protected override void ConfigureContainer()
        {
            var batch = new CompositionBatch();
            batch.AddExportedValue(Container);
            Container.Compose(batch);
            base.ConfigureContainer();
        }

        protected override DependencyObject CreateShell()
        {
            return new ShellView();
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            var shellViewModel = Container.GetExportedValue<IShellViewModel>();
            Debug.Assert((Shell).Equals(shellViewModel.View), "ShellView/ShellViewModel incorrect.");
            shellViewModel.View.Show();
        }

        #endregion
    }
}