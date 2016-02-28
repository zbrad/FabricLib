using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;
using System.Text;
using System.Threading.Tasks;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Gateway
{
    // This class implements the IEndpointBehavior and IDispatchMessageInspector interfaces.
    // The IEndpointBehavior hooks up the message inspector to the service EndpointDispatcher.
    // The message inspector methods are invoked for every message received/sent.
    internal class FabricResolver : WcfLib.Resolver
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        static readonly FabricClient client = new FabricClient();
        public static FabricClient Client { get { return client; } }

        TimeSpan timeout = TimeSpan.FromSeconds(30);
        object routingTableLock = new object();
        public Uri Retry { get; private set; }

        public void Initialize(Uri retry)
        {
            this.Retry = retry;
        }

        public override Task<Filter> CreateFilter(Message request)
        {
            var part = new PartInfo(request);
            log.Info("Create filter for service {0} with partition key {1}",
                    request.Headers.To,
                    part.ToString());
            return createFilter(part, null);
        }

        // This method is invoked by the host for every request message received. For every message received we
        // resolve the service address using the To address (service name) and the partition key using FabricClient.
        // The RouterTable is updated with the result.         
        public override Task<Filter> UpdateFilter(Message request, Filter oldfilter)
        {
            // if this is not a retry, then just reuse the existing filter
            if (!request.Headers.To.Equals(Retry))
                return Task.FromResult<Filter>(oldfilter);

            // we need to get an updated filter if client told us to retry
            var part = new PartInfo(request);
            log.Info("Updating filter for service {0} with partition key {1}",
                    request.Headers.To,
                    part.ToString());

            var oldFf = (FabricFilter)oldfilter;
            return createFilter(part, oldFf);
        }

        async Task<Filter> createFilter(PartInfo part, FabricFilter old)
        {
            var prev = old == null ? null : old.ResolvedServicePartition;
            var rsp = await getRsp(part, prev);
            var ff = new FabricFilter();
            ff.Initialize(this.Retry, part, rsp);
            return ff;
        }

        async Task<ResolvedServicePartition> getRsp(PartInfo part, ResolvedServicePartition prev)
        {
            ResolvedServicePartition rsp = null;

            try
            {               
                switch (part.Kind)
                {
                    case ServicePartitionKind.Singleton:
                        rsp = await Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, prev, this.timeout);
                        break;
                    case ServicePartitionKind.Int64Range:
                        rsp = await Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.RangeKey, prev, this.timeout);
                        break;
                    case ServicePartitionKind.Named:
                        rsp = await Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.NameKey, prev, this.timeout);
                        break;
                }
            }
            catch (AggregateException e)
            {
                log.Error(e, "Resolved for service {0} with partition key {1}. Found no endpoints.",
                    part.Message.Headers.To,
                    part.ToString());
                return null;
            }

            log.Info("Resolve for service {0} with partition key {1}. Found {2} endpoints.",
                        part.Message.Headers.To,
                        part.KindName,
                        rsp.Endpoints.Count);
            return rsp;
        }


        // creates new table without indicated filter item
        MessageFilterTable<IEnumerable<ServiceEndpoint>> deltaTable(Filter filter)
        {
            var table = new MessageFilterTable<IEnumerable<ServiceEndpoint>>();
            foreach (var kv in this.Router.Configuration.FilterTable)
            {
                if (filter.Equals(kv.Key))
                    continue;
                table.Add(kv.Key, kv.Value);
            }

            return table;
        }

        // Remove a MessageFilter from the RouterTable
        void removeRouting(Filter filter)
        {
            lock (this.routingTableLock)
            {
                var table = deltaTable(filter);
                var config = new RoutingConfiguration(table, true);
                this.Router.Extension.ApplyConfiguration(config);
            }

            log.Info("A stale MessageFilter has been removed.");
        }
    }
}
