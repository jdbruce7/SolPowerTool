using System.IO;

namespace SolPowerTool.App.Elements
{
    public class Pair : IElement
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public static IElement Parse(string line, StreamReader sr)
        {
            return new Pair()._parse(line, sr);
        }

        private IElement _parse(string line, StreamReader sr)
        {
            string[] pair = line.Split('=');
            Key = pair[0].Trim();
            Value = pair[1].Trim();
            return this;
        }

        public override string ToString()
        {
            return string.Format("\t\t{0} = {1}", Key, Value);
        }

        #region Implementation of IElement

        public void ToStream(StreamWriter sw)
        {
            sw.WriteLine(ToString());
        }

        #endregion
    }
}