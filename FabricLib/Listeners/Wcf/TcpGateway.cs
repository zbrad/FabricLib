using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using Microsoft.ServiceFabric.Services;
using System.Threading.Tasks;
using System.Threading;
using ZBrad.FabricLib.Wcf.Internal;

namespace ZBrad.FabricLib.Wcf
{
    /// <summary>
    /// A Service Fabric Gateway for Wcf
    /// </summary>
    public class TcpGateway : ICommunicationListener
    {
        TcpGatewayListenerInternal listener;

        public TcpGateway(StatelessService instance)
        {
            listener = new TcpGatewayListenerInternal();
            listener.Initialize(instance);
        }

        public void Initialize(ServiceInitializationParameters init)
        {
            listener.Initialize(init);
        }

        public Task<string> OpenAsync(CancellationToken token)
        {
            return listener.OpenAsync(token);
        }

        public Task CloseAsync(CancellationToken token)
        {
            return listener.CloseAsync(token);
        }

        public void Abort()
        {
            listener.Abort();
        }
    }
}
