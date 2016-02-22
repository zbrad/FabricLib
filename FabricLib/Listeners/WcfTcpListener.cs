using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using ZBrad.FabricLib.Utilities;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib
{
    public class WcfTcpListener : Listener
    {
        TcpService service = new TcpService();   

        public void Initialize(StatelessService stateless)
        {
            base.Initialize(stateless, service);
            service.Initialize(this.Path, stateless);
        }

        public void Initialize(StatefulService stateful)
        {
            base.Initialize(stateful, service);
            service.Initialize(this.Path, stateful);
        }
    }
}
