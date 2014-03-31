using System;
using System.IO;

namespace SolPowerTool.App.Elements
{
    public class Project : SectionBase
    {
        public static readonly Guid FolderTypeID = Guid.Parse("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        public static readonly Guid ProjectTypeID = Guid.Parse("FAE04EC0-301F-11D3-BF4B-00C04F79EFBC");
        private Guid? _projectGuid;


        public string DisplayName
        {
            get { return Values[0]; }
            set { Values[0] = value; }
        }

        public string Location
        {
            get { return Values[1]; }
            set { Values[1] = value; }
        }

        public string ProjectGuidString
        {
            get { return Values[2]; }
        }

        public Guid ProjectGuid
        {
            get
            {
                if (Values[2] == null) return default(Guid);
                if (_projectGuid.HasValue) return _projectGuid.Value;
                Guid id;
                _projectGuid = Guid.TryParse(Values[2], out id) ? id : default(Guid);
                return _projectGuid.Value;
            }
        }

        public Guid TypeID
        {
            get { return Guid.Parse(Type.Substring(1, Type.Length - 2)); }
        }


        public static IElement Parse(string line, StreamReader sr)
        {
            return new Project()._parse(line, sr);
        }

        protected override IElement _parse(string line, StreamReader sr)
        {
            base._parse(line, sr);
            for (int i = 0; i < Values.Length; i++)
            {
                Values[i] = Values[i].Trim();
                Values[i] = Values[i].Substring(1, Values[i].Length - 2);
            }
            while (sr.Peek() >= 0)
            {
                line = sr.ReadLine();
                if (line.TrimStart().StartsWith("ProjectSection"))
                    Elements.Add(ProjectSection.Parse(line, sr));
                else if (line.StartsWith("EndProject"))
                    break;
                else
                    Elements.Add(Line.Parse(line, sr));
            }
            Elements.Add(Line.Parse(line, sr));
            return this;
        }

        public override string ToString()
        {
            return string.Format("Project({0}) = \"{1}\"", Type, string.Join("\", \"", Values));
        }

        public override void ToStream(StreamWriter sw)
        {
            sw.WriteLine(ToString());
            foreach (IElement element in Elements)
                element.ToStream(sw);
        }
    }
}