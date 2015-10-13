using System;
using System.Fabric;
using System.Fabric.Description;
using System.Globalization;
using Microsoft.ServiceFabric.Services;
using System.Threading.Tasks;
using System.Threading;
using ZBrad.FabLibs.Utilities;

namespace ZBrad.FabLibs.Wcf.Gateway
{
    /// <summary>
    /// A Service Fabric Gateway for Wcf
    /// </summary>
    public class GatewayListener : ICommunicationListener
    {
        GatewayHost gateway = null;
        public ServiceInitializationParameters Init { get; private set; }

        public int Port { get; private set; }
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// create a Wcf gateway listener
        /// </summary>
        /// <param name="log"></param>
        public GatewayListener()
        {
        }

        void ICommunicationListener.Initialize(ServiceInitializationParameters init)
        {
            this.Init = init;
            UpdatePort();
            this.gateway = new GatewayHost(this);
        }

        Task<string> ICommunicationListener.OpenAsync(CancellationToken token)
        {
            log.Info("Starting {0} listening on {1}", Init.ServiceName, this.gateway.Address);
            this.gateway.StartListening();
            return Task.FromResult<string>(this.gateway.Address);
        }

        Task ICommunicationListener.CloseAsync(CancellationToken token)
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
            this.Port = Utility.GetPort(this.Init.CodePackageActivationContext);
            if (this.Port <= 0)
            {
                log.Error("?Unable to get port number");
                throw new ApplicationException("unable to get port number");
            }

            log.Info("Gateway port: {0}", this.Port);
        }
    }
}
