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
    public class RestListener : ICommunicationListener
    {
        RestListenerInternal listener;

        public RestListener(StatefulService instance)
        {
            listener = new RestListenerInternal();
            listener.Initialize(instance);
        }

        public RestListener(StatelessService instance)
        {
            listener = new RestListenerInternal();
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
