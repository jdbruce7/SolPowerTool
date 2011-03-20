using System;
using System.Collections.Generic;
using SolPowerTool.App.Common;
using SolPowerTool.App.Data;

namespace SolPowerTool.App.ViewModels
{
    public class BuildConfigItemFilter : PropertyChangedBase
    {
        private static readonly List<BuildConfigItemFilter> _instances = new List<BuildConfigItemFilter>();
        private bool _isSelected;

        public BuildConfigItemFilter(BuildConfiguration buildConfiguration)
        {
            Name = buildConfiguration.Name;
            _instances.Add(this);
        }

        public string Name { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                if (SelectedChanged != null)
                    SelectedChanged(this, EventArgs.Empty);
            }
        }

        public static event EventHandler SelectedChanged;
    }
}