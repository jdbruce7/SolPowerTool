using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Windows.Data;
using SolPowerTool.App.Common;
using SolPowerTool.App.Interfaces.Views;

namespace SolPowerTool.App.Data
{
    [DebuggerDisplay("Solution = {SolutionName}")]
    public sealed class Solution : DTOBase, IFileAction
    {
        private readonly FileInfo _solutionFileInfo;
        private DirectoryInfo _solutionDirectoryInfo;

        private Solution(string solutionFilename)
        {
            _solutionFileInfo = new FileInfo(solutionFilename);
            SolutionFilename = _solutionFileInfo.FullName;


            Projects = new DirtyTrackingCollection<Project>();
            Projects.DirtyChanged += OnDirtyChanged;

            Folders = new DirtyTrackingCollection<Folder>();
            Folders.DirtyChanged += OnDirtyChanged;
        }


        public string SolutionFilename { get; private set; }

        public string SolutionDirectoryname { get; set; }

        public DirtyTrackingCollection<Project> Projects { get; private set; }

        public DirtyTrackingCollection<Folder> Folders { get; private set; }

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

        [FileIOPermission(SecurityAction.LinkDemand, Unrestricted = true)]
        public static Solution Parse(string solutionFilename)
        {
            var solution = new Solution(solutionFilename);
            solution._parseSolutionFile();
            return solution;
        }

        private void _parseSolutionFile()
        {
            _solutionDirectoryInfo = new DirectoryInfo(Path.GetDirectoryName(SolutionFilename));
            SolutionDirectoryname = _solutionDirectoryInfo.FullName;

            Elements.Solution solutionElement = Elements.Solution.Parse(SolutionFilename);
            IEnumerable<Elements.Project> projects = solutionElement.Elements
                                                                    .OfType<Elements.Project>()
                                                                    .Where(p => p.TypeID == Elements.Project.ProjectTypeID);

            IEnumerable<Elements.Project> folders = solutionElement.Elements
                                                                   .OfType<Elements.Project>()
                                                                   .Where(p => p.TypeID == Elements.Project.FolderTypeID);

            foreach (Elements.Project projectElement in projects)
            {
                string filename = Path.Combine(SolutionDirectoryname, projectElement.Location);
                if (File.Exists(filename))
                {
                    Project project = Project.Parse(this, filename);
                    if (Projects.Any(p => p.ProjectGuid == project.ProjectGuid))
                        throw new InvalidOperationException(string.Format("Duplicate project found: {0} ({1})", project.ProjectGuid, project.ProjectName));
                    Projects.Add(project);
                }
            }

            foreach (Elements.Project folder in folders)
            {
                Folders.Add(new Folder(folder.DisplayName, folder.ProjectGuid));
            }

            DistinctReferences = new ObservableCollection<Reference>(Projects.SelectMany(p => p.References).OrderBy(r => r.Name)); //.Where(p => p.HasHintPath);

            foreach (Project project in Projects)
            {
                foreach (ProjectReference projectReference in project.ProjectReferences)
                {
                    if (Projects.All(p => p.ProjectGuid != projectReference.ProjectGuid))
                    {
                        var byPath = Projects.FirstOrDefault(p => string.Compare(p.ProjectFilename, projectReference.RootedPath, StringComparison.InvariantCultureIgnoreCase) == 0);
                        if (byPath == null)
                        {
                            projectReference.IsNotInSolution = true;
                            project.HasMissingProjectReferences = true;
                        }
                        else
                        {
                            projectReference.HasIncorrectProjectGuid = true;
                            project.HasIncorrectProjectReferenceGuids = true;
                        }
                    }
                }
            }

            IsDirty = false;
        }

        public bool MakeWriteable()
        {
            File.SetAttributes(SolutionFilename, File.GetAttributes(SolutionFilename) & ~FileAttributes.ReadOnly);
            return (File.GetAttributes(SolutionFilename) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        public string Filename { get { return SolutionFilename; } }
    }
}