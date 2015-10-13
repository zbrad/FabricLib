using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabricLib.Utilities;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf
{
    internal abstract class WcfListenerBase : ICommunicationListener
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        internal WcfServiceBase WcfService { get; private set; }
        internal StatefulService Stateful { get; private set; }
        internal StatelessService Stateless { get; private set; }
        internal object Instance { get { return this.Stateless != null ? (object)this.Stateless : (object)this.Stateful; } }
        internal Uri Path { get; private set; }

        protected abstract WcfServiceBase GetWcfService();

        public void Initialize(StatelessService instance)
        {
            updatePath(Utility.GetDefaultPath(instance));
            this.Stateless = instance;
        }

        public void Initialize(StatefulService instance)
        {
            updatePath(Utility.GetDefaultPath(instance));
            this.Stateful = instance;
        }

        void updatePath(UriBuilder u)
        {
            if (u.Scheme.Equals("tcp"))
                u.Scheme = Uri.UriSchemeNetTcp;
            this.Path = u.Uri;
        }

        public void Initialize(ServiceInitializationParameters init)
        {
            // nothing yet
        }

        public Task<string> OpenAsync(CancellationToken token)
        {
            this.WcfService = this.GetWcfService();

            log.Info("Start listening on {0}", this.WcfService.UriPath);
            this.WcfService.StartListening();
            return Task.FromResult<string>(this.WcfService.UriPath);
        }

        public Task CloseAsync(CancellationToken token)
        {
            return Task.Run(() => this.WcfService.StopListening());
        }

        public void Abort()
        {
            this.WcfService.StopListening();
        }
    }

}
