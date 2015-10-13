using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabLibs.Wcf.Services;
using ZBrad.FabLibs.Utilities;

namespace ZBrad.FabLibs.Wcf.Listeners
{
    internal interface IWcfServiceProvider
    {
        WcfService GetWcfService();
    }

    internal class TcpListener : WcfListener, IWcfServiceProvider
    {
        public void Initialize(StatefulService instance)
        {
            this.Initialize(instance, this);
        }

        public void Initialize(StatelessService instance)
        {
            this.Initialize(instance, this);
        }
        public WcfService GetWcfService()
        {
            var service = new TcpService();
            service.Initialize(this.Path, this.Instance);
            return service;
        }
    }
}
