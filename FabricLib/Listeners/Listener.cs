using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ZBrad.FabricLib.Utilities;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib
{
    public abstract class Listener : ICommunicationListener
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public StatefulService Stateful { get; private set; }
        public StatelessService Stateless { get; private set; }
        public object Instance { get { return this.Stateless != null ? (object)this.Stateless : (object)this.Stateful; } }
        public Uri Path { get; protected set; }
        public IStartable Starter { get; private set; }

        protected virtual void Initialize(StatelessService instance, IStartable starter)
        {
            var path = Utility.GetDefaultPartitionUri(instance);
            this.Path = Util.GetWcfUri(path);
            this.Stateless = instance;
            this.Starter = starter;
        }

        protected virtual void Initialize(StatefulService instance, IStartable starter)
        {
            var path = Utility.GetDefaultPath(instance);
            this.Path = Util.GetWcfUri(path);
            this.Stateful = instance;
            this.Starter = starter;
        }

        public virtual void Initialize(ServiceInitializationParameters init)
        {
            // nothing yet
        }

        public async virtual Task<string> OpenAsync(CancellationToken token)
        {
            log.Info("Start listening on {0}", this.Path);
            await this.Starter.StartAsync();
            return this.Path.AbsoluteUri;
        }

        public virtual Task CloseAsync(CancellationToken token)
        {
            return this.Starter.StopAsync();
        }

        public virtual void Abort()
        {
            this.Starter.StopAsync();
        }
    }

}
