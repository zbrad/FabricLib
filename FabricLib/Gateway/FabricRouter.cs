using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Gateway
{
    internal class FabricRouter<T> : Router<T> where T : WcfServiceBase,new()
    {

    }
}
