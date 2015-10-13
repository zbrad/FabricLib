using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using L = ZBrad.FabLibs.Wcf.Listeners;


namespace ZBrad.FabLibs.Wcf
{
    public class RestListener : ICommunicationListener
    {
        L.RestListener listener;

        public RestListener(StatefulService instance)
        {
            listener = new L.RestListener();
            listener.Initialize(instance);
        }

        public RestListener(StatelessService instance)
        {
            listener = new L.RestListener();
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
