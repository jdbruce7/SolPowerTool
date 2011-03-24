using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Data;
using System.Xml;
using SolPowerTool.App.Common;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("Project = {ProjectName}")]
    public class Project : DTOBase, IDisposable
    {
        private static readonly List<Project> _instances = new List<Project>();
        private static IEnumerable<string> _filterOut;
        private readonly FileSystemWatcher _fileWatcher;

        private readonly FileInfo _projectFileInfo;
        private string _assemblyName;
        private DirtyTrackingCollection<BuildConfiguration> _buildConfigurations;
        private bool _isReadOnly;
        private bool _isSelected;
        private XmlNamespaceManager _nsmgr;
        private string _rootNamespace;
        private XmlDocument _xmlDocument;

        private Project(Solution solution, string projectFilename)
        {
            Solution = solution;
            _projectFileInfo = new FileInfo(projectFilename);
            ProjectFilename = _projectFileInfo.FullName;

            BuildConfigurations = new DirtyTrackingCollection<BuildConfiguration>();
            BuildConfigurations.DirtyChanged += OnDirtyChanged;

            References = new DirtyTrackingCollection<Reference>();
            References.DirtyChanged += OnDirtyChanged;


            _instances.Add(this);

            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(ProjectFilename), Path.GetFileName(ProjectFilename));
            _fileWatcher.Changed += _fileWatcher_Changed;
        }

        public DirtyTrackingCollection<Reference> References { get; private set; }

        public ICollectionView ReferencesView
        {
            get
            {
                ICollectionView collectionView = CollectionViewSource.GetDefaultView(References);
                collectionView.SortDescriptions.Add(new SortDescription("Include", ListSortDirection.Ascending));
                return collectionView;
            }
        }

        public Solution Solution { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }

        public bool IsReadOnly
        {
            get
            {
                _projectFileInfo.Refresh();
                bool b = (_projectFileInfo.Attributes & FileAttributes.ReadOnly) != 0;
                if (_isReadOnly == b)
                    return b;
                _isReadOnly = b;
                RaisePropertyChanged(() => IsReadOnly);
                return b;
            }
        }

        public static IEnumerable<string> FilterOut
        {
            get { return _filterOut; }
            set
            {
                BuildConfiguration.ApplyFilter(value);
                foreach (Project project in _instances)
                {
                    Project p = project;
                    project.RaisePropertyChanged(() => p.IrregularOutputPaths);
                }
                _filterOut = value;
                _filterChanged();
            }
        }

        public string ProjectName
        {
            get { return Path.GetFileNameWithoutExtension(ProjectFilename); }
        }

        public string ProjectFilename { get; private set; }

        public Guid ProjectGuid { get; private set; }

        [DirtyTracking]
        public string RootNamespace
        {
            get { return _rootNamespace; }
            set
            {
                if (_rootNamespace == value)
                    return;
                _rootNamespace = value;
                RaisePropertyChanged(() => RootNamespace);
            }
        }

        [DirtyTracking]
        public string AssemblyName
        {
            get { return _assemblyName; }
            set
            {
                if (_assemblyName == value)
                    return;
                _assemblyName = value;
                RaisePropertyChanged(() => RootNamespace);
            }
        }

        public string TargetFrameworkVersion { get; private set; }

        public bool IrregularOutputPaths
        {
            get { return BuildConfigurations.Where(bc => !bc.IsExcluded).Select(bc => bc.NormalizedOutputPath).Distinct().Count() > 1; }
        }

        public string OutputType { get; private set; }

        public string Platform { get; private set; }

        public string Configuration { get; private set; }

        public DirtyTrackingCollection<BuildConfiguration> BuildConfigurations
        {
            get { return _buildConfigurations; }
            private set
            {
                _buildConfigurations = value;
                RaisePropertyChanged(() => BuildConfigurations);
                RaisePropertyChanged(() => BuildConfigurationsView);
            }
        }

        public ICollectionView BuildConfigurationsView
        {
            get
            {
                ICollectionView collectionView = CollectionViewSource.GetDefaultView(BuildConfigurations);
                collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                collectionView.Filter = new Predicate<object>(param => !((BuildConfiguration)param).IsExcluded);
                return collectionView;
            }
        }

        public string PreBuildEvent { get; private set; }

        public bool HasPreBuildEvent
        {
            get { return !string.IsNullOrWhiteSpace(PreBuildEvent); }
        }

        public string PostBuildEvent { get; private set; }

        public bool HasPostBuildEvent
        {
            get { return !string.IsNullOrWhiteSpace(PostBuildEvent); }
        }

        public override bool IsDirty
        {
            get { return base.IsDirty || BuildConfigurations.Any(bc => bc.IsDirty) || References.Any(bc => bc.IsDirty); }
            set { base.IsDirty = value; }
        }

        private void OnDirtyChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => IsDirty);
            FireDirtyChanged();
        }

        private void _fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    RaisePropertyChanged(() => IsReadOnly);
                    _parse();
                    break;
            }
        }

        public override int CompareTo(object obj)
        {
            var target = obj as Project;
            if (target == null)
                throw new InvalidCastException("Must compare to same type.");
            return ProjectName.CompareTo(target.ProjectName);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [FileIOPermission(SecurityAction.LinkDemand, Unrestricted = true)]
        public static Project Parse(Solution solution, string projectFilename)
        {
            var project = new Project(solution, projectFilename);
            project._parse();
            return project;
        }


        private void _parse()
        {
            _xmlDocument = new XmlDocument();
            _xmlDocument.Load(ProjectFilename);
            var nsmgr = new XmlNamespaceManager(_xmlDocument.NameTable);
            nsmgr.AddNamespace("root", "http://schemas.microsoft.com/developer/msbuild/2003");
            _nsmgr = nsmgr;

            XmlElement root = _xmlDocument.DocumentElement;
            if (root == null)
                throw new InvalidOperationException(string.Format("No document element: {0}", ProjectFilename));

            XmlNode firstPropertyGroup = root.SelectSingleNode("//root:Project/root:PropertyGroup[not(@Condition)]",
                                                               nsmgr);

            if (firstPropertyGroup == null)
                throw new InvalidOperationException(string.Format("No First PropertyGroup: {0}", ProjectFilename));

            // Get ProjectGuid.
            XmlNode node = firstPropertyGroup.SelectSingleNode("root:ProjectGuid", nsmgr);
            ProjectGuid = Guid.Parse(node.InnerText);

            // Get Project general properties
            node = firstPropertyGroup.SelectSingleNode("root:Configuration", nsmgr);
            if (node != null)
                Configuration = node.InnerText;
            node = firstPropertyGroup.SelectSingleNode("root:Platform", nsmgr);
            if (node != null)
                Platform = node.InnerText;
            node = firstPropertyGroup.SelectSingleNode("root:OutputType", nsmgr);
            if (node != null)
                OutputType = node.InnerText;
            node = firstPropertyGroup.SelectSingleNode("root:RootNamespace", nsmgr);
            if (node != null)
                RootNamespace = node.InnerText;
            node = firstPropertyGroup.SelectSingleNode("root:AssemblyName", nsmgr);
            if (node != null)
                AssemblyName = node.InnerText;
            node = firstPropertyGroup.SelectSingleNode("root:TargetFrameworkVersion", nsmgr);
            if (node != null)
                TargetFrameworkVersion = node.InnerText;

            node = root.SelectSingleNode("//root:Project/root:PropertyGroup/root:PostBuildEvent", nsmgr);
            if (node != null)
                if (!string.IsNullOrWhiteSpace(node.InnerText))
                    PostBuildEvent = node.InnerText;
            node = root.SelectSingleNode("//root:Project/root:PropertyGroup/root:PreBuildEvent", nsmgr);
            if (node != null)
                if (!string.IsNullOrWhiteSpace(node.InnerText))
                    PreBuildEvent = node.InnerText;

            // Build configurations
            XmlNodeList nodes = root.SelectNodes("//root:Project/root:PropertyGroup[@Condition]", nsmgr);
            if (nodes != null)
            {
                var sorter = new BuildConfigurationSorter();
                foreach (XmlNode node2 in nodes)
                {
                    BuildConfiguration configuration = BuildConfiguration.Parse(this, node2, nsmgr);
                    if (configuration != null)
                        _buildConfigurations.Add(configuration);
                }
            }

            // References
            nodes = root.SelectNodes("//root:Project/root:ItemGroup/root:Reference", nsmgr);
            foreach (XmlNode node2 in nodes)
            {
                Reference reference = Reference.Parse(this, node2, nsmgr);
                if (reference != null)
                    References.Add(reference);
            }

            IsDirty = false;
        }

        public void CommitChanges()
        {
            XmlElement root = _xmlDocument.DocumentElement;
            XmlNode firstPropertyGroup = root.SelectSingleNode("//root:Project/root:PropertyGroup[1]", _nsmgr);

            // Save Project general properties
            XmlNode node = firstPropertyGroup.SelectSingleNode("root:RootNamespace", _nsmgr);
            if (node != null)
                node.InnerText = RootNamespace;
            node = firstPropertyGroup.SelectSingleNode("root:AssemblyName", _nsmgr);
            if (node != null)
                node.InnerText = AssemblyName;

            foreach (BuildConfiguration buildConfiguration in BuildConfigurations)
                buildConfiguration.CommitChanges();

            foreach (Reference reference in References)
                reference.CommitChanges();

            _xmlDocument.Save(ProjectFilename);

            IsDirty = false;
        }

        public override string ToString()
        {
            return ProjectName;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        [FileIOPermission(SecurityAction.LinkDemand, Unrestricted = true)]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_fileWatcher != null)
                {
                    _fileWatcher.Changed -= _fileWatcher_Changed;
                    _fileWatcher.Dispose();
                }
            }
        }

        private static void _filterChanged()
        {
            foreach (Project project in _instances)
            {
                Project p = project;
                project.RaisePropertyChanged(() => p.BuildConfigurations);
                project.RaisePropertyChanged(() => p.BuildConfigurationsView);
            }
        }

        public bool MakeWriteable()
        {
            _projectFileInfo.Attributes &= ~FileAttributes.ReadOnly;
            return !IsReadOnly;
        }

        public void Reload()
        {
            References.Clear();
            BuildConfigurations.Clear();
            _parse();
        }

        #region Nested type: BuildConfigurationSorter

        private class BuildConfigurationSorter : IComparer<BuildConfiguration>
        {
            #region IComparer<BuildConfiguration> Members

            public int Compare(BuildConfiguration x, BuildConfiguration y)
            {
                return string.Compare(x.Name, y.Name);
            }

            #endregion
        }

        #endregion
    }
}