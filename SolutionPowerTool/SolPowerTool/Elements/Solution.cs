using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SolPowerTool.App.Elements
{
    internal class Solution
    {
        private const string SOLUTION_FILE_HEADER = "Microsoft Visual Studio Solution File, Format Version ";

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
            string line = null;
            while (string.IsNullOrWhiteSpace(line))
                line = sr.ReadLine();
            if (!line.StartsWith(SOLUTION_FILE_HEADER, true, CultureInfo.InvariantCulture))
                throw new InvalidOperationException("This does not appear to be a " + SOLUTION_FILE_HEADER);

            var verS = line.Substring(SOLUTION_FILE_HEADER.Length);
            double ver;
            if (!double.TryParse(verS, out ver))
                throw new InvalidOperationException("Cannot determine version of the file: " + verS);

            if (ver < 11.0 || ver > 12.0)
                throw new InvalidOperationException("This solution version is not supported: " + verS);
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