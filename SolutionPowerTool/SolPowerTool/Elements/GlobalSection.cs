using System.IO;

namespace SolPowerTool.App.Elements
{
    public class GlobalSection : SectionBase
    {
        public static IElement Parse(string line, StreamReader sr)
        {
            return new GlobalSection()._parse(line, sr);
        }

        protected override IElement _parse(string line, StreamReader sr)
        {
            base._parse(line, sr);

            while (sr.Peek() >= 0)
            {
                line = sr.ReadLine();
                if (line.TrimStart().StartsWith("EndGlobalSection"))
                    break;
                else
                    Elements.Add(Pair.Parse(line, sr));
            }
            Elements.Add(Line.Parse(line, sr));
            return this;
        }

        public override string ToString()
        {
            return string.Format("\tGlobalSection({0}) = {1}", Type, Values[0]);
        }

        public override void ToStream(StreamWriter sw)
        {
            sw.WriteLine(ToString());
            foreach (IElement element in Elements)
                element.ToStream(sw);
        }
    }
}