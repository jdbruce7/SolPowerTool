using System.IO;

namespace SolPowerTool.App.Elements
{
    public class Global : SectionBase
    {
        public static IElement Parse(string line, StreamReader sr)
        {
            return new Global()._parse(line, sr);
        }

        protected override IElement _parse(string line, StreamReader sr)
        {
            base._parse(line, sr);
            while (sr.Peek() >= 0)
            {
                line = sr.ReadLine();
                if (line.TrimStart().StartsWith("GlobalSection"))
                    Elements.Add(GlobalSection.Parse(line, sr));
                else if (line.StartsWith("EndGlobalSection"))
                    break;
                else
                    Elements.Add(Line.Parse(line, sr));
            }
            Elements.Add(Line.Parse(line, sr));
            return this;
        }

        public override string ToString()
        {
            return "Global";
        }

        public override void ToStream(StreamWriter sw)
        {
            sw.WriteLine(ToString());
            foreach (IElement element in Elements)
                element.ToStream(sw);
        }
    }
}