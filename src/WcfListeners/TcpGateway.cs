using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;

using ZBrad.FabLibs.Wcf;
using L = ZBrad.FabLibs.Wcf.Listeners;
using S = ZBrad.FabLibs.Wcf.Services;

namespace ZBrad.FabLibs.Wcf
{
    public class TcpGateway : ICommunicationListener
    {
        L.GatewayListener<L.TcpListener, S.TcpService> listener;

        public TcpGateway(StatelessService instance)
        {
            listener = new L.GatewayListener<L.TcpListener, S.TcpService>();
            listener.Initialize(instance);
        }

        void ICommunicationListener.Abort()
        {
            listener.Abort();
        }

        Task ICommunicationListener.CloseAsync(CancellationToken token)
        {
            return listener.CloseAsync(token);
        }

        void ICommunicationListener.Initialize(ServiceInitializationParameters serviceInitializationParameters)
        {
            listener.Initialize(serviceInitializationParameters);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken token)
        {
            return listener.OpenAsync(token);
        }
    }
}
