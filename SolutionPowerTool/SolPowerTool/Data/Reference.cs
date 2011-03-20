using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Microsoft.Win32;
using SolPowerTool.App.Common;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("Reference = {Include}")]
    public class Reference : DTOBase
    {
        private readonly Project _project;
        private FileInfo _fileInfo;
        private string _hintPath;
        private string _include;
        private bool _isSelected;
        private XmlNode _itemNode;
        private ICommand _loadAssemblyCommand;
        private string _name;
        private XmlNamespaceManager _nsmgr;
        private ICommand _pickFileCommand;
        private ICommand _previewAssemblyCommand;
        private bool _private;
        private string _processArchitecture;
        private string _publicKeyToken;
        private ICommand _removeAssemblyVersionCommand;
        private bool _specificVersion;
        private Version _version;

        private Reference(Project project)
        {
            _project = project;
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }

        [DirtyTracking]
        public string HintPath
        {
            get { return _hintPath; }
            set
            {
                if (_hintPath == value)
                    return;
                _hintPath = value;
                _fileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(_project.ProjectFilename), _hintPath));
                RaisePropertyChanged(() => HintPath);
                RaisePropertyChanged(() => RootedHintPath);
                RaisePropertyChanged(() => HasHintPath);
                RaisePropertyChanged(() => HasFile);
            }
        }

        public string RootedHintPath
        {
            get { return HasHintPath ? _fileInfo.FullName : null; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var x = new Uri(_project.ProjectFilename);
                    var y = new Uri(value);
                    HintPath = Uri.UnescapeDataString(x.MakeRelativeUri(y).ToString().Replace('/', '\\'));
                }
            }
        }

        public bool HasHintPath
        {
            get { return !string.IsNullOrWhiteSpace(_hintPath); }
        }

        public bool HasFile
        {
            get
            {
                if (!HasHintPath)
                    return false;
                _fileInfo.Refresh();
                return _fileInfo.Exists;
            }
        }

        [DirtyTracking]
        public bool SpecificVersion
        {
            get { return _specificVersion; }
            set
            {
                if (_specificVersion == value)
                    return;
                _specificVersion = value;
                RaisePropertyChanged(() => SpecificVersion);
            }
        }

        [DirtyTracking]
        public string Include
        {
            get { return _include; }
            set
            {
                if (_include == value)
                    return;
                _include = value;

                var assemblyName = new AssemblyName(value);

                Name = assemblyName.Name;
                Version = assemblyName.Version;
                PublicKeyToken = null;
                byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
                if (publicKeyToken != null)
                    PublicKeyToken = BitConverter.ToString(publicKeyToken).Replace("-", "").ToLower();
                ProcessArchitecture = assemblyName.ProcessorArchitecture.ToString();
                RaisePropertyChanged(() => Include);
            }
        }

        public string ProcessArchitecture
        {
            get { return _processArchitecture; }
            private set
            {
                if (value == _processArchitecture) return;
                _processArchitecture = value;
                RaisePropertyChanged(() => ProcessArchitecture);
            }
        }

        public string Name
        {
            get { return _name; }
            private set
            {
                _name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        public Version Version
        {
            get { return _version; }
            private set
            {
                _version = value;
                RaisePropertyChanged(() => Version);
            }
        }

        public string PublicKeyToken
        {
            get { return _publicKeyToken; }
            private set
            {
                _publicKeyToken = value;
                RaisePropertyChanged(() => PublicKeyToken);
            }
        }

        public Project Project
        {
            get { return _project; }
        }

        [DirtyTracking]
        public bool Private
        {
            get { return _private; }
            set
            {
                if (_private == value) return;
                _private = value;
                RaisePropertyChanged(() => Private);
            }
        }


        public ICommand PreviewAssemblyCommand
        {
            get
            {
                return _previewAssemblyCommand ?? (_previewAssemblyCommand
                                                   = new RelayCommand<object>(
                                                         obj =>
                                                         {
                                                             var name = AssemblyLoader.GetAssemblyFullName(RootedHintPath);
                                                             var msg = string.Format("Name:  {0}\r\n\nPath:  {1}", name.FullName, name.CodeBase);
                                                             MessageBox.Show(msg, Name, MessageBoxButton.OK, MessageBoxImage.Information);
                                                         },
                                                         param => HasFile));
            }
        }

        public ICommand LoadAssemblyCommand
        {
            get
            {
                return _loadAssemblyCommand ?? (_loadAssemblyCommand = new RelayCommand<object>
                                                                           (param => { Include = AssemblyLoader.GetAssemblyFullName(RootedHintPath).FullName; },
                                                                            param => HasFile));
            }
        }

        public ICommand RemoveAssemblyVersionCommand
        {
            get { return _removeAssemblyVersionCommand ?? (_removeAssemblyVersionCommand = new RelayCommand<object>(param => { Include = Name; })); }
        }


        public ICommand PickFileCommand
        {
            get { return _pickFileCommand ?? (_pickFileCommand = new RelayCommand<object>(param => _pickFile())); }
        }

        public static Reference Parse(Project project, XmlNode node, XmlNamespaceManager nsmgr)
        {
            var reference = new Reference(project);
            reference._parse(node, nsmgr);
            return reference;
        }

        private void _parse(XmlNode itemNode, XmlNamespaceManager nsmgr)
        {
            _itemNode = itemNode;
            _nsmgr = nsmgr;

            XmlNode node = itemNode.Attributes["Include"];
            if (node != null)
                Include = node.Value;

            SpecificVersion = true;
            node = itemNode.SelectSingleNode("root:SpecificVersion", nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    SpecificVersion = b;
            }

            node = itemNode.SelectSingleNode("root:HintPath", nsmgr);
            if (node != null)
                HintPath = node.InnerText;

            Private = true;
            node = itemNode.SelectSingleNode("root:Private", nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    Private = b;
            }


            IsDirty = false;
        }

        public void CommitChanges()
        {
            if (!IsDirty)
                return;
            XmlNodeList nodes;
            XmlNode node;

            node = _itemNode.Attributes["Include"];
            if (node == null)
                throw new InvalidOperationException("No Include attribute found.");
            node.Value = Include;

            nodes = _itemNode.SelectNodes("root:HintPath", _nsmgr);
            if (nodes != null)
                foreach (XmlNode node2 in nodes)
                    _itemNode.RemoveChild(node2);

            nodes = _itemNode.SelectNodes("root:SpecificVersion", _nsmgr);
            if (nodes != null)
                foreach (XmlNode node2 in nodes)
                    _itemNode.RemoveChild(node2);

            nodes = _itemNode.SelectNodes("root:Private", _nsmgr);
            if (nodes != null)
                foreach (XmlNode node2 in nodes)
                    _itemNode.RemoveChild(node2);

            if (HasHintPath)
            {
                node = _itemNode.OwnerDocument.CreateElement("HintPath", _nsmgr.LookupNamespace("root"));
                node.InnerText = HintPath;
                _itemNode.AppendChild(node);
            }
            if (!SpecificVersion)
            {
                node = _itemNode.OwnerDocument.CreateElement("SpecificVersion", _nsmgr.LookupNamespace("root"));
                node.InnerText = SpecificVersion.ToString().ToLower();
                _itemNode.AppendChild(node);
            }
            if (!Private)
            {
                node = _itemNode.OwnerDocument.CreateElement("Private", _nsmgr.LookupNamespace("root"));
                node.InnerText = Private.ToString().ToLower();
                _itemNode.AppendChild(node);
            }

            IsDirty = false;
        }

        public override int CompareTo(object obj)
        {
            var target = obj as Reference;
            if (target == null)
                throw new InvalidCastException("Must compare to same type.");
            return Include.CompareTo(target.Include);
        }

        private void _pickFile()
        {
            var ofd = new OpenFileDialog();
            if (HasHintPath)
                ofd.FileName = Path.GetFileName(HintPath);
            else
                ofd.FileName = Name;
            ofd.Filter = "DLL (*.dll)|*.dll|All files (*.*)|*.*";
            ofd.DefaultExt = "dll";
            ofd.AddExtension = true;
            ofd.CheckFileExists = true;
            if (ofd.ShowDialog() == true)
                RootedHintPath = ofd.FileName;
        }
    }
}