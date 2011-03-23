using System.IO;

namespace SolPowerTool.App.Elements
{
    public class Line : IElement
    {
        private string _line;

        #region Implementation of IElement

        public void ToStream(StreamWriter sw)
        {
            sw.WriteLine(_line);
        }

        #endregion

        public static Line Parse(string line, StreamReader sr)
        {
            return new Line {_line = line};
        }
    }
}