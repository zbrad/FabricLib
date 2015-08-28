using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using Microsoft.ServiceFabric.Services;
using System.Threading.Tasks;
using System.Threading;
using ZBrad.ServiceFabric.WcfLib.Gateway;

namespace ZBrad.ServiceFabric.WcfLib
{
    /// <summary>
    /// A Service Fabric Gateway for Wcf
    /// </summary>
    public class GatewayListener : ICommunicationListener, ILibListener
    {
        GatewayHost gateway = null;
        public ServiceInitializationParameters Init { get; private set; }

        public int Port { get; private set; }
        public IEventLog Log { get; private set; }

        /// <summary>
        /// create a Wcf gateway listener
        /// </summary>
        /// <param name="log"></param>
        public GatewayListener(IEventLog log)
        {
            this.Log = log;
        }

        void ICommunicationListener.Initialize(ServiceInitializationParameters init)
        {
            this.Init = init;
            UpdatePort();
            this.gateway = new GatewayHost(this);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken cancellationToken)
        {
            Log.Info("Starting {0} listening on {1}", Init.ServiceName, this.gateway.Address);
            this.gateway.StartListening();
            return Task.FromResult<string>(this.gateway.Address);
        }

        Task ICommunicationListener.CloseAsync(CancellationToken cancellationToken)
        {
            this.gateway.StopListening();
            return Task.FromResult<int>(0);
        }

        void ICommunicationListener.Abort()
        {
            this.gateway.StopListening();
        }

        void UpdatePort()
        {
            this.Port = Util.GetPort(this, "Gateway");
            if (this.Port <= 0)
            {
                Log.Error("?Unable to get port number");
                throw new ApplicationException("unable to get port number");
            }

            Log.Info("Gateway port: {0}", this.Port);
        }
    }
}
