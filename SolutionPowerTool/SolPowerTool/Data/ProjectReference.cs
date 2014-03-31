using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SolPowerTool.App.Common;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("ProjectReference = {Include}")]
    public class ProjectReference : DTOBase
    {
        private readonly Project _project;
        private FileInfo _fileInfo;
        private bool _hasIncorrectProjectGuid;
        private string _include;
        private bool _isNotInSolution;
        private bool _isSelected;
        private XmlNode _itemNode;
        private string _name;
        private XmlNamespaceManager _nsmgr;
        private Guid _projectGuid;
        private string _rootedPath;

        private ProjectReference(Project project)
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
        public string Include
        {
            get { return _include; }
            set
            {
                if (value == _include) return;
                _include = value;
                _rootedPath = null;
                _fileInfo = null;
                RaisePropertyChanged(() => Include);
                RaisePropertyChanged(() => RootedPath);
            }
        }

        public string RootedPath
        {
            get
            {
                if (_rootedPath != null) return _rootedPath;
                string s = Path.Combine(Path.GetDirectoryName(_project.ProjectFilename), _include);
                _fileInfo = new FileInfo(s);
                return _fileInfo.FullName;
            }
        }

        public bool HasProjectFile
        {
            get
            {
                if (_fileInfo == null)
                    return false;
                _fileInfo.Refresh();
                return _fileInfo.Exists;
            }
        }

        [DirtyTracking]
        public Guid ProjectGuid
        {
            get { return _projectGuid; }
            set
            {
                if (value == _projectGuid) return;
                _projectGuid = value;
                RaisePropertyChanged(() => ProjectGuid);
            }
        }

        [DirtyTracking]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        public bool IsNotInSolution
        {
            get { return _isNotInSolution; }
            set
            {
                _isNotInSolution = value;
                RaisePropertyChanged(() => IsNotInSolution);
            }
        }

        public bool HasIncorrectProjectGuid
        {
            get { return _hasIncorrectProjectGuid; }
            set
            {
                _hasIncorrectProjectGuid = value;
                RaisePropertyChanged(() => HasIncorrectProjectGuid);
            }
        }

        public override int CompareTo(object obj)
        {
            var target = obj as ProjectReference;
            if (target == null)
                throw new InvalidCastException("Must compare to same type.");
            return Include.CompareTo(target.Include);
        }

        public static ProjectReference Parse(Project project, XmlNode node, XmlNamespaceManager nsmgr)
        {
            var reference = new ProjectReference(project);
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

            node = itemNode.SelectSingleNode("root:Project", nsmgr);
            if (node != null)
            {
                Guid projectGuid;
                if (Guid.TryParse(node.InnerText, out projectGuid))
                    ProjectGuid = projectGuid;
            }

            node = itemNode.SelectSingleNode("root:Name", nsmgr);
            if (node != null)
                Name = node.InnerText;

            IsDirty = false;
        }
    }
}