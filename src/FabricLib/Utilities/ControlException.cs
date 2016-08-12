using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.FabricLib.Utilities
{
    public class ControlException : Exception
    {
        public ControlException(string s) : base(s) { }

        public ControlException(string s, Exception e) : base(s, e) { }
    }
}
