using System;
using System.Collections.Generic;
using System.IO;

namespace SolPowerTool.App.Elements
{
    internal class Solution
    {
        private const string SOLUTION_FILE_HEADER = "Microsoft Visual Studio Solution File, Format Version 11.00";

        private Solution()
        {
            Elements = new List<IElement>();
        }

        public List<IElement> Elements { get; private set; }

        public static Solution Parse(string file)
        {
            var solution = new Solution();
            using (var sr = new StreamReader(file))
                solution._parse(sr);
            return solution;
        }

        private void _parse(StreamReader sr)
        {
            string line = sr.ReadLine();
            if (string.Compare(line, SOLUTION_FILE_HEADER, StringComparison.InvariantCulture) != 0)
                throw new InvalidOperationException("This does not appear to be a " + SOLUTION_FILE_HEADER);
            Elements.Add(Line.Parse(line, sr));
            while (sr.Peek() >= 0)
            {
                line = sr.ReadLine();
                if (line.StartsWith("Project"))
                    Elements.Add(Project.Parse(line, sr));
                else if (line.StartsWith("Global"))
                    Elements.Add(Global.Parse(line, sr));
                else
                    Elements.Add(Line.Parse(line, sr));
            }
        }

        public void WriteTo(string file)
        {
            using (var sw = new StreamWriter(file))
                foreach (IElement element in Elements)
                    element.ToStream(sw);
        }
    }
}