using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabricLib.Utilities;
using ZBrad.WcfLib;
namespace ZBrad.FabricLib.Wcf
{ 
    internal class TcpListenerInternal : WcfListenerBase
    {
        protected override WcfServiceBase GetWcfService()
        {
            var service = new TcpService();
            service.Initialize(this.Path, this.Instance);
            return service;
        }
    }
}
