using System;

namespace SolPowerTool.App.Common
{
    public interface IDirtyTracking
    {
        event EventHandler DirtyChanged;
    }
}