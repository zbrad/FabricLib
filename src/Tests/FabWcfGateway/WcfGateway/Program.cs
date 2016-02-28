using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using ZBrad.FabLibs.Utilities;

namespace EchoApp
{    public class Program
    {
        public static void Main(string[] args)
        {
            Utility.Register<EchoGateway>();
        }
    }
}
