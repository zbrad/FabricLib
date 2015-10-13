using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabLibs.Utilities;
using ZBrad.FabLibs.Wcf.Services;

namespace ZBrad.FabLibs.Wcf.Listeners
{
    internal enum ServiceType
    {
        Stateless,
        Stateful
    }

    internal abstract class WcfListener : ICommunicationListener
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        internal WcfService Host { get; private set; }
        internal StatefulService Stateful { get; private set; }
        internal StatelessService Stateless { get; private set; }
        internal object Instance { get { return this.Stateless != null ? (object)this.Stateless : (object)this.Stateful; } }
        internal Uri Path { get; private set; }

        public IWcfServiceProvider WcfServiceProvider { get; private set; }

        public void Initialize(StatelessService instance, IWcfServiceProvider provider)
        {
            this.Path = Utility.GetDefaultPath(instance).Uri;
            this.Stateless = instance;
            this.WcfServiceProvider = provider;
        }

        public void Initialize(StatefulService instance, IWcfServiceProvider provider)
        {
            this.Path = Utility.GetDefaultPath(instance).Uri;
            this.Stateful = instance;
            this.WcfServiceProvider = provider;
        }

        public void Initialize(ServiceInitializationParameters init)
        {
            // nothing yet
        }

        public Task<string> OpenAsync(CancellationToken token)
        {
            this.Host = this.WcfServiceProvider.GetWcfService();

            log.Info("Start listening on {0}", this.Host.UriPath);
            this.Host.StartListening();
            return Task.FromResult<string>(this.Host.UriPath);
        }

        public Task CloseAsync(CancellationToken token)
        {
            return Task.Run(() => this.Host.StopListening());
        }

        public void Abort()
        {
            this.Host.StopListening();
        }
    }

}
