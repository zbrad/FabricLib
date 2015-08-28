using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.ServiceFabric.WcfLib.Service;

namespace ZBrad.ServiceFabric.WcfLib
{
    public class RestListener<T> : ICommunicationListener, ILibListener where T : class, new()
    {
        RestService<T> host = null;
        public T Instance { get; private set; }
        public ServiceInitializationParameters Init { get; private set; }
        public int Port { get; private set; }
        public IEventLog Log { get; private set; }

        public RestListener(IEventLog log)
        {
            this.Log = log;
            this.Instance = new T();
        }

        void ICommunicationListener.Initialize(ServiceInitializationParameters init)
        {
            this.Init = init;
            UpdatePort();
            this.host = new RestService<T>(this);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken cancellationToken)
        {
            Log.Info("Starting {0} listening on {1}", Init.ServiceName, this.host.UriPath);
            this.host.StartListening();
            return Task.FromResult<string>(this.host.UriPath);
        }

        Task ICommunicationListener.CloseAsync(CancellationToken cancellationToken)
        {
            this.host.StopListening();
            return Task.FromResult<int>(0);
        }

        void ICommunicationListener.Abort()
        {
            this.host.StopListening();
        }

        void UpdatePort()
        {
            this.Port = Util.GetPort(this, "Service");
            if (this.Port <= 0)
            {
                Log.Error("?Unable to get port number");
                throw new ApplicationException("unable to get port number");
            }

            Log.Info("Service port: {0}", this.Port);
        }
    }
}
