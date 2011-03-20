using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml;
using SolPowerTool.App.Common;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("BuildConfiguration = {Name}")]
    public class BuildConfiguration : DTOBase
    {
        private static readonly List<BuildConfiguration> _instances = new List<BuildConfiguration>();
        private const string CodeAnalysisRuleSetDirectories = @";C:\Program Files (x86)\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\\Rule Sets;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\\Rule Sets";
        private const string CodeAnalysisRuleDirectories = @";C:\Program Files (x86)\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\\Rules;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\\Rules";
        private const string CodeAnalysisModuleSuppressionsFile = "GlobalSuppressions.cs";
        private const string ErrorReport = "prompt";

        private readonly string _realConfiguration;
        private readonly string _realPlatform;
        private string _codeAnalysisRuleSet;
        private string _outputPath;
        private bool _runCodeAnalysis;
        XmlNode _groupNode;
        private XmlNamespaceManager _nsmgr;
        private FileInfo _codeAnalysisRuleSetFileInfo;

        private BuildConfiguration(Project project, string name)
        {
            Project = project;
            RealName = name;
            Name = Uri.UnescapeDataString(name);
            string[] s = Name.Split('|');
            Configuration = s[0];
            Platform = s[1];

            s = RealName.Split('|');
            _realConfiguration = s[0];
            _realPlatform = s[1];

            _instances.Add(this);
        }

        public SolidColorBrush IsDirtyColor
        {
            get { return IsDirty ? Brushes.Red : Brushes.Black; }
        }

        public bool IsSelected { get; set; }

        public bool IsExcluded { get; set; }

        public string Platform { get; private set; }

        public string Configuration { get; private set; }

        public string Name { get; private set; }

        public string RealName { get; private set; }

        public Project Project { get; private set; }

        [DirtyTracking]
        public bool RunCodeAnalysis
        {
            get { return _runCodeAnalysis; }
            set
            {
                if (_runCodeAnalysis == value)
                    return;
                _runCodeAnalysis = value;
                RaisePropertyChanged(() => RunCodeAnalysis);
            }
        }

        [DirtyTracking]
        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                if (_outputPath == value)
                    return;
                _outputPath = value;
                RaisePropertyChanged(() => OutputPath);
                RaisePropertyChanged(() => NormalizedOutputPath);
            }
        }

        public string NormalizedOutputPath
        {
            get
            {
                return OutputPath != null
                           ? OutputPath.Replace(_realConfiguration, "{Configuration}").Replace(_realPlatform,
                                                                                               "{Platform}")
                           : null;
            }
            set
            {
                OutputPath = value == null
                                 ? null
                                 : value.Replace("{Configuration}", _realConfiguration).Replace("{Platform}",
                                                                                                _realPlatform);
            }
        }

        [DirtyTracking]
        public string CodeAnalysisRuleSet
        {
            get { return _codeAnalysisRuleSet; }
            set
            {
                if (_codeAnalysisRuleSet == value)
                    return;
                _codeAnalysisRuleSet = value;
                _codeAnalysisRuleSetFileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(Project.ProjectFilename), _codeAnalysisRuleSet));
                RaisePropertyChanged(() => CodeAnalysisRuleSet);
                RaisePropertyChanged(() => RootedCodeAnalysisRuleSet);
                RaisePropertyChanged(() => HasCodeAnalysisRuleSet);
                RaisePropertyChanged(() => HasCodeAnalysisRuleSetFile);
            }
        }

        public string RootedCodeAnalysisRuleSet
        {
            get { return HasCodeAnalysisRuleSet ? _codeAnalysisRuleSetFileInfo.FullName : null; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var x = new Uri(Project.ProjectFilename);
                    var y = new Uri(value);
                    CodeAnalysisRuleSet = Uri.UnescapeDataString(x.MakeRelativeUri(y).ToString().Replace('/', '\\'));
                }
            }
        }

        public bool HasCodeAnalysisRuleSet
        {
            get { return !string.IsNullOrWhiteSpace(_codeAnalysisRuleSet); }
        }

        public bool HasCodeAnalysisRuleSetFile
        {
            get
            {
                if (!HasCodeAnalysisRuleSet)
                    return false;
                _codeAnalysisRuleSetFileInfo.Refresh();
                return _codeAnalysisRuleSetFileInfo.Exists;
            }
        }

        public bool IsMissingElements { get; private set; }

        public override string ToString()
        {
            return Name;
        }


        public static BuildConfiguration Parse(Project parent, XmlNode node, XmlNamespaceManager nsmgr)
        {
            XmlAttribute conditionAttr = node.Attributes["Condition"];
            if (conditionAttr == null)
                return null;

            string condition = conditionAttr.Value;
            string[] a = condition.Split(new[] { "==" }, StringSplitOptions.None);
            if (a.Length != 2)
                return null;
            if (a[0].Trim() != "'$(Configuration)|$(Platform)'")
                return null;
            string item = a[1].Trim();
            string name = item.Substring(1, item.Length - 2);

            return new BuildConfiguration(parent, name)._parse(node, nsmgr);
        }

        /*
    <CodeAnalysisUseTypeNameInSuppression>true</CodeAnalysisUseTypeNameInSuppression>
    <CodeAnalysisModuleSuppressionsFile>GlobalSuppressions.cs</CodeAnalysisModuleSuppressionsFile>
    <ErrorReport>prompt</ErrorReport>
    <CodeAnalysisRuleSet>..\..\Solution Items\AuraClient.ruleset</CodeAnalysisRuleSet>
    <CodeAnalysisRuleSetDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\\Rule Sets</CodeAnalysisRuleSetDirectories>
    <CodeAnalysisIgnoreBuiltInRuleSets>true</CodeAnalysisIgnoreBuiltInRuleSets>
    <CodeAnalysisRuleDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\\Rules</CodeAnalysisRuleDirectories>
    <CodeAnalysisIgnoreBuiltInRules>true</CodeAnalysisIgnoreBuiltInRules>
         */
        private BuildConfiguration _parse(XmlNode groupNode, XmlNamespaceManager nsmgr)
        {
            _groupNode = groupNode;
            _nsmgr = nsmgr;
            XmlNode node;
            XmlNodeList nodes;

            //<RunCodeAnalysis>true</RunCodeAnalysis>         
            node = groupNode.SelectSingleNode("root:RunCodeAnalysis", nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    RunCodeAnalysis = b;
            }

            //<CodeAnalysisRuleSet>..\..\Solution Items\AuraClient.ruleset</CodeAnalysisRuleSet>
            node = groupNode.SelectSingleNode("root:CodeAnalysisRuleSet", nsmgr);
            if (node != null)
                CodeAnalysisRuleSet = node.InnerText;

            IsMissingElements = _checkForIncorrectElements();

            //OutputPath
            node = groupNode.SelectSingleNode("root:OutputPath", nsmgr);
            if (node != null)
                OutputPath = node.InnerText;

            IsDirty = false;
            return this;
        }

        private bool _checkForIncorrectElements()
        {
            XmlNode node;
            XmlNodeList nodes;

            //<CodeAnalysisLogFile>bin\Debug\PwC.Aura.Client.Models.AddDCPane.dll.CodeAnalysisLog.xml</CodeAnalysisLogFile>
            nodes = _groupNode.SelectNodes("root:CodeAnalysisLogFile", _nsmgr);
            if (nodes != null)
            {
                if (nodes.Count != 0)
                    return true;
            }
            else return true;

            //<CodeAnalysisRuleSetDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\\Rule Sets</CodeAnalysisRuleSetDirectories>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisRuleSetDirectories", _nsmgr);
            if (node != null)
            {
                if (string.Compare(node.InnerText, CodeAnalysisRuleSetDirectories, true) != 0)
                    return true;
            }
            else return true;

            //<CodeAnalysisRuleDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\\Rules</CodeAnalysisRuleDirectories>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisRuleDirectories", _nsmgr);
            if (node != null)
            {
                if (string.Compare(node.InnerText, CodeAnalysisRuleDirectories, true) != 0)
                    return true;
            }
            else return true;

            //<CodeAnalysisUseTypeNameInSuppression>true</CodeAnalysisUseTypeNameInSuppression>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisUseTypeNameInSuppression", _nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    if (!b) return true;
            }
            else return true;

            //<CodeAnalysisModuleSuppressionsFile>GlobalSuppressions.cs</CodeAnalysisModuleSuppressionsFile>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisModuleSuppressionsFile", _nsmgr);
            if (node != null)
            {
                if (string.Compare(node.InnerText, CodeAnalysisModuleSuppressionsFile, true) != 0)
                    return true;
            }
            else return true;

            //<ErrorReport>prompt</ErrorReport>
            node = _groupNode.SelectSingleNode("root:ErrorReport", _nsmgr);
            if (node != null)
            {
                if (string.Compare(node.InnerText, ErrorReport, true) != 0)
                    return true;
            }
            else return true;

            //<CodeAnalysisIgnoreBuiltInRuleSets>true</CodeAnalysisIgnoreBuiltInRuleSets>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisIgnoreBuiltInRuleSets", _nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    if (!b) return true;
            }
            else return true;

            //<CodeAnalysisIgnoreBuiltInRules>true</CodeAnalysisIgnoreBuiltInRules>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisIgnoreBuiltInRules", _nsmgr);
            if (node != null)
            {
                bool b;
                if (bool.TryParse(node.InnerText, out b))
                    if (!b) return true;
            }
            else return true;

            return false;
        }

        private void _fixForIncorrectElements()
        {
            XmlNode node;
            XmlNodeList nodes;

            //<CodeAnalysisLogFile>bin\Debug\PwC.Aura.Client.Models.AddDCPane.dll.CodeAnalysisLog.xml</CodeAnalysisLogFile>
            nodes = _groupNode.SelectNodes("root:CodeAnalysisLogFile", _nsmgr);
            if (nodes != null)
                foreach (XmlNode node2 in nodes)
                    _groupNode.RemoveChild(node2);

            //<CodeAnalysisRuleSetDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\\Rule Sets</CodeAnalysisRuleSetDirectories>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisRuleSetDirectories", _nsmgr)
                ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisRuleSetDirectories", _nsmgr.LookupNamespace("root")));
            node.InnerText = CodeAnalysisRuleSetDirectories;

            //<CodeAnalysisRuleDirectories>;C:\Program Files\Microsoft Visual Studio 10.0\Team Tools\Static Analysis Tools\FxCop\\Rules</CodeAnalysisRuleDirectories>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisRuleDirectories", _nsmgr)
                ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisRuleDirectories", _nsmgr.LookupNamespace("root")));
            node.InnerText = CodeAnalysisRuleDirectories;

            //<CodeAnalysisUseTypeNameInSuppression>true</CodeAnalysisUseTypeNameInSuppression>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisUseTypeNameInSuppression", _nsmgr)
                ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisUseTypeNameInSuppression", _nsmgr.LookupNamespace("root")));
            node.InnerText = "true";

            //<CodeAnalysisModuleSuppressionsFile>GlobalSuppressions.cs</CodeAnalysisModuleSuppressionsFile>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisModuleSuppressionsFile", _nsmgr)
               ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisModuleSuppressionsFile", _nsmgr.LookupNamespace("root")));
            node.InnerText = CodeAnalysisModuleSuppressionsFile;

            //<ErrorReport>prompt</ErrorReport>
            node = _groupNode.SelectSingleNode("root:ErrorReport", _nsmgr)
               ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("ErrorReport", _nsmgr.LookupNamespace("root")));
            node.InnerText = ErrorReport;

            //<CodeAnalysisIgnoreBuiltInRuleSets>true</CodeAnalysisIgnoreBuiltInRuleSets>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisIgnoreBuiltInRuleSets", _nsmgr)
              ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisIgnoreBuiltInRuleSets", _nsmgr.LookupNamespace("root")));
            node.InnerText = "true";

            //<CodeAnalysisIgnoreBuiltInRules>true</CodeAnalysisIgnoreBuiltInRules>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisIgnoreBuiltInRules", _nsmgr)
              ?? _groupNode.AppendChild(_groupNode.OwnerDocument.CreateElement("CodeAnalysisIgnoreBuiltInRules", _nsmgr.LookupNamespace("root")));
            node.InnerText = "true";

        }

        public static void ResetExclusions()
        {
            foreach (BuildConfiguration bc in _instances)
                bc.IsExcluded = false;
        }

        public static void ApplyFilter(IEnumerable<string> include)
        {
            ResetExclusions();
            if (include == null)
                return;
            foreach (BuildConfiguration buildConfiguration in _instances)
            {
                BuildConfiguration bc = buildConfiguration;
                if (!include.Any(s => s == bc.Name))
                    buildConfiguration.IsExcluded = true;
            }
        }

        public void CommitChanges()
        {
            if (!IsDirty)
                return;
            XmlNode node;
            XmlNodeList nodes;

            //<RunCodeAnalysis>true</RunCodeAnalysis>         
            node = _groupNode.SelectSingleNode("root:RunCodeAnalysis", _nsmgr);
            if (node != null)
                node.InnerText = RunCodeAnalysis.ToString().ToLower();
            else
            {
                if (RunCodeAnalysis)
                {
                    node = _groupNode.OwnerDocument.CreateElement("RunCodeAnalysis", _nsmgr.LookupNamespace("root"));
                    node.InnerText = RunCodeAnalysis.ToString().ToLower();
                    _groupNode.AppendChild(node);
                }
            }

            //<CodeAnalysisRuleSet>..\..\Solution Items\AuraClient.ruleset</CodeAnalysisRuleSet>
            node = _groupNode.SelectSingleNode("root:CodeAnalysisRuleSet", _nsmgr);
            if (node != null)
                node.InnerText = CodeAnalysisRuleSet;
            else
            {
                if (!string.IsNullOrWhiteSpace(CodeAnalysisRuleSet))
                {
                    node = _groupNode.OwnerDocument.CreateElement("CodeAnalysisRuleSet", _nsmgr.LookupNamespace("root"));
                    node.InnerText = CodeAnalysisRuleSet;
                    _groupNode.AppendChild(node);
                }
            }

            _fixForIncorrectElements();

            //OutputPath
            node = _groupNode.SelectSingleNode("root:OutputPath", _nsmgr);
            if (node != null)
                node.InnerText = OutputPath;
            else
            {
                if (!string.IsNullOrWhiteSpace(OutputPath))
                {
                    node = _groupNode.OwnerDocument.CreateNode(XmlNodeType.Element, "OutputPath", _nsmgr.LookupNamespace("root"));
                    node.InnerText = OutputPath;
                    _groupNode.AppendChild(node);
                }
            }
            IsDirty = false;
        }

        public override int CompareTo(object obj)
        {
            var target = obj as BuildConfiguration;
            if (target == null)
                throw new InvalidCastException("Must compare to same type.");
            return Name.CompareTo(target.Name);
        }
    }
}