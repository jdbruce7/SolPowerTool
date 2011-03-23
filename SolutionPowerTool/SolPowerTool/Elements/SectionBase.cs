using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SolPowerTool.App.Elements
{
    public abstract class SectionBase : IElement
    {
        protected readonly List<IElement> Elements = new List<IElement>();
        protected string[] Values { get; set; }

        public string Type { get; private set; }

        protected virtual IElement _parse(string line, StreamReader sr)
        {
            Type = _getType(line);
            _getValues(line);
            return this;
        }

        private string _getType(string line)
        {
            int openPos = line.IndexOf("(");
            if (openPos < 0) return null;
            int closePos = line.IndexOf(")", openPos + 1);
            string type = line.Substring(openPos + 1, closePos - openPos - 1);
            return type;
        }

        private void _getValues(string line)
        {
            int pos = line.IndexOf('=');
            if (pos < 0) return;
            Values = line.Substring(pos + 1).Split(new[] {", "}, StringSplitOptions.None).Select(s => s.Trim()).ToArray();
        }

        #region Implementation of IElement

        public virtual void ToStream(StreamWriter sw)
        {
            sw.WriteLine(ToString());
            foreach (IElement element in Elements)
            {
                element.ToStream(sw);
            }
        }

        #endregion
    }
}