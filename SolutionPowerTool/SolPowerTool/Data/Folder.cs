using System;
using SolPowerTool.App.Common;

namespace SolPowerTool.App.Data
{
    public class Folder : DTOBase
    {
        public const string SOLPOWERFOLDER = "Added by Solution Power Tool";
        private readonly Guid _guid;
        private readonly string _name;

        public Folder(string name, Guid guid)
        {
            _name = name;
            _guid = guid;
        }

        public string Name
        {
            get { return _name; }
        }

        public Guid Guid
        {
            get { return _guid; }
        }

        public override int CompareTo(object obj)
        {
            throw new NotImplementedException();
        }
    }
}