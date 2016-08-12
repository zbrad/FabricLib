using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.FabricLib.Utilities
{
    public class PackageException : Exception
    {
        public PackageException(string s) : base(s) { }

        public PackageException(string s, Exception e) : base(s, e) { }
    }
}
