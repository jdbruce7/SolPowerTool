using System.IO;

namespace SolPowerTool.App.Elements
{
    public interface IElement
    {
        void ToStream(StreamWriter sw);
    }
}