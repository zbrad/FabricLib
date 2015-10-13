using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabricLib.Wcf.Internal;


namespace ZBrad.FabricLib.Wcf
{
    public class TcpListener : ICommunicationListener
    {
        TcpListenerInternal listener;

        public TcpListener(StatefulService instance)
        {
            listener = new TcpListenerInternal();
            listener.Initialize(instance);
        }

        public TcpListener(StatelessService instance)
        {
            listener = new TcpListenerInternal();
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
