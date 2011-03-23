using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using SolPowerTool.App.Common;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("Solution = {SolutionName}")]
    public class Solution : DTOBase
    {
        private readonly FileInfo _solutionFileInfo;
        private DirectoryInfo _solutionDirectoryInfo;

        private Solution(string solutionFilename)
        {
            _solutionFileInfo = new FileInfo(solutionFilename);
            SolutionFilename = _solutionFileInfo.FullName;


            Projects = new DirtyTrackingCollection<Project>();
            Projects.DirtyChanged += OnDirtyChanged;


            _parseSolutionFile();
        }


        public string SolutionFilename { get; private set; }

        public string SolutionDirectoryname { get; set; }

        public DirtyTrackingCollection<Project> Projects { get; private set; }


        public string SolutionName
        {
            get { return Path.GetFileNameWithoutExtension(SolutionFilename); }
        }

        public override bool IsDirty
        {
            get { return base.IsDirty || Projects.Any(p => p.IsDirty); }
            set { base.IsDirty = value; }
        }

        public IEnumerable<Reference> DistinctReferences { get; set; }

        public ICollectionView DistinctReferencesView
        {
            get
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(DistinctReferences);
                view.GroupDescriptions.Add(new PropertyGroupDescription("Name"));
                view.SortDescriptions.Add(new SortDescription("Project.ProjectName", ListSortDirection.Ascending));
                return view;
            }
        }

        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(SolutionFilename); }
        }

        private void OnDirtyChanged(object sender, EventArgs e)
        {
            RaisePropertyChanged(() => IsDirty);
            FireDirtyChanged();
        }

        public override int CompareTo(object obj)
        {
            throw new NotSupportedException();
        }

        public static Solution Parse(string solutionFilename)
        {
            return new Solution(solutionFilename);
        }

        private void _parseSolutionFile()
        {
            _solutionDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(SolutionFilename));
            SolutionDirectoryname = _solutionDirectoryInfo.FullName;

            Elements.Solution solutionElement = Elements.Solution.Parse(SolutionFilename);
            IEnumerable<Elements.Project> projects = solutionElement.Elements
                .OfType<Elements.Project>()
                .Where(p => p.TypeID == Elements.Project.ProjectTypeID);

            foreach (Elements.Project projectElement in projects)
            {
                string filename = Path.Combine(SolutionDirectoryname, projectElement.Location);
                if (File.Exists(filename))
                {
                    Project project = Project.Parse(this, filename);
                    if (Projects.Any(p => p.ProjectGuid == project.ProjectGuid))
                        throw new InvalidOperationException();
                    Projects.Add(project);
                }
            }

            DistinctReferences = new ObservableCollection<Reference>(Projects.SelectMany(p => p.References).OrderBy(r => r.Name)); //.Where(p => p.HasHintPath);

            IsDirty = false;
        }
    }
}