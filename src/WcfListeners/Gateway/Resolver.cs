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
using ZBrad.FabLibs.Wcf.Services;

namespace ZBrad.FabLibs.Wcf.Gateway
{
    // This class implements the IEndpointBehavior and IDispatchMessageInspector interfaces.
    // The IEndpointBehavior hooks up the message inspector to the service EndpointDispatcher.
    // The message inspector methods are invoked for every message received/sent.
    internal class Resolver<L,S> : IDispatchMessageInspector, IEndpointBehavior
        where L : Listeners.WcfListener, new()
        where S : Services.WcfService, new()
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        static readonly FabricClient client = new FabricClient();

        public Listeners.IGatewayListener<L,S> Gateway { get; private set; }
        public FabricClient Client { get { return client; } }

        public RoutingExtension Extension { get; private set; }

        public RoutingConfiguration Configuration { get; private set; }

        public RoutingBehavior Behavior { get; private set; }

        TimeSpan timeout = TimeSpan.FromSeconds(30);
        object routingTableLock = new object();

        public Resolver(Listeners.IGatewayListener<L,S> gateway)
        {
            this.Gateway = gateway;
            this.Configuration = new RoutingConfiguration();
            this.Behavior = new RoutingBehavior(this.Configuration);
        }

        class PartInfo
        {
            public Message Message { get; set; }
            public ServicePartitionKind Kind { get; set; }
            public string KindName { get { return Enum.GetName(typeof(ServicePartitionKind), this.Kind); } }
            public string NameKey { get; set; }
            public long RangeKey { get; set; }

            public PartInfo(Message m)
            {
                this.Message = m;
                this.Kind = ServicePartitionKind.Singleton;

                var key = Filter.GetPartitionKey(m);
                if (key == null)
                    return;

                    long ranged;
                if (long.TryParse(key, out ranged))
                {
                    this.Kind = ServicePartitionKind.Int64Range;
                    this.RangeKey = ranged;
                    return;
                }

                this.NameKey = key;
                this.Kind = ServicePartitionKind.Named;
            }
        }

        #region IDispatchMessageInspector interface methods

        // This method is invoked by the host for every request message received. For every message received we
        // resolve the service address using the To address (service name) and the partition key using FabricClient.
        // The RouterTable is updated with the result.         
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            bool isRetry = this.Gateway.IsRetry(channel.LocalAddress.Uri);

            var filter = getFilter(request);
            if (filter != null && !isRetry)
                return null;

            var part = new PartInfo(request);
            log.Info(
                    "Received request for service {0} with partition key {1}. IsRetry: {2}",
                    request.Headers.To,
                    part.KindName,
                    isRetry);

            ResolvedServicePartition prev = (filter == null) ? null : filter.ResolvedServicePartition;
            ResolvedServicePartition rsp = getRsp(part, prev);
            if (rsp == null && isRetry && filter != null)
                this.removeRouting(filter);

            log.Info(
                    "Resolved for service {0} with partition key {1}. Found {2} endpoints.",
                    request.Headers.To,
                    part.KindName,
                    rsp.Endpoints.Count);

                filter = new Filter(this.Gateway.Service, request.Headers.To, rsp);
                this.updateRouting(filter);          

            return null;
        }

        ResolvedServicePartition getRsp(PartInfo part, ResolvedServicePartition prev)
        {
            try
            {
                switch (part.Kind)
                {
                    case ServicePartitionKind.Singleton:
                        return this.Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, prev, this.timeout).Result;
                    case ServicePartitionKind.Int64Range:
                        return this.Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.RangeKey, prev, this.timeout).Result;
                    case ServicePartitionKind.Named:
                        return this.Client.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.NameKey, prev, this.timeout).Result;
                }
            }
            catch (AggregateException e)
            {
                log.Error(e, "Resolved for service {0} with partition key {1}. Found no endpoints.",
                    part.Message.Headers.To,
                    part.KindName);                
            }

            return null;
        }

        Filter getFilter(Message request)
        {
            MessageFilter mf = null;
            this.Configuration.FilterTable.GetMatchingFilter(request, out mf);
            return mf as Filter;
        }

        void processEndpoint(List<Uri> endpoints, ResolvedServiceEndpoint e)
        {
            if (e.Role != ServiceEndpointRole.Stateless && e.Role != ServiceEndpointRole.StatefulPrimary)
                return;

            Uri u;
            if (Uri.TryCreate(e.Address.Replace("Tcp:","net.tcp:"), UriKind.Absolute, out u))
                endpoints.Add(u);
        }

        public void BeforeSendReply(ref System.ServiceModel.Channels.Message reply, object correlationState)
        {
        }

        #endregion

        #region IEndpointBehavior interface methods

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
        }

        // Adds message inspection to the EndpointDispatcher
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            this.Extension = endpointDispatcher.ChannelDispatcher.Host.Extensions.Find<RoutingExtension>();

            if (this.Extension == null)
            {
                throw new InvalidOperationException("RoutingExtension is not found. Make sure RoutingBehavior is added to the ServiceHost");
            }

            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        // Updates the router table with a MessageFilter and corresponding endpoints.
        void updateRouting(Filter filter)
        {
            lock (this.routingTableLock)
            {
                var table = deltaTable(filter);         // new table without "filter"
                table.Add(filter, filter.Endpoints);    // add new location

                this.Configuration = new RoutingConfiguration(table, true);
                this.Extension.ApplyConfiguration(this.Configuration);
            }

            log.Info("Routing service MessageFilter table updated.");
        }

        // creates new table without indicated filter item
        MessageFilterTable<IEnumerable<ServiceEndpoint>> deltaTable(Filter filter)
        {
            var table = new MessageFilterTable<IEnumerable<ServiceEndpoint>>();
            foreach (var kv in this.Configuration.FilterTable)
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
                this.Extension.ApplyConfiguration(config);
            }

            log.Info("A stale MessageFilter has been removed.");
        }
    }
}
