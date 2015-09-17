using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabLibs.Wcf.Service;

namespace ZBrad.FabLibs.Wcf
{
    public class TcpListener<T> : ICommunicationListener, ILibListener where T : class, new()
    {
        public static readonly string Node = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

        TcpService<T> host = null;
        public T Instance { get; private set; }
        public ServiceInitializationParameters Init { get; private set; }
        public int Port { get; private set; }
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public TcpListener()
        {
            this.Instance = new T();
        }

        void ICommunicationListener.Initialize(ServiceInitializationParameters init)
        {
            this.Init = init;
            UpdatePort();
            this.host = new TcpService<T>(this);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken cancellationToken)
        {
            log.Info("Starting {0} listening on {1}", Init.ServiceName, this.host.UriPath);
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
                log.Error("?Unable to get port number");
                throw new ApplicationException("unable to get port number");
            }

            log.Info("Service port: {0}", this.Port);
        }
    }
}
