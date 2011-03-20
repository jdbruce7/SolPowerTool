using System.Collections.Generic;
using System.Linq;
using System.Text;
using SolPowerTool.App.ViewModels;

namespace SolPowerTool.App.Interfaces
{
    public interface IProjectDetailView:ISolPowerToolView
    {
        IProjectDetailViewModel ViewModel { get; set; }
        void Show();
        bool Focus();
        bool Activate();
        void Close();
    }
}
