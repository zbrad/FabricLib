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

namespace ZBrad.FabLibs.Wcf.Gateway
{
    // This class implements the IEndpointBehavior and IDispatchMessageInspector interfaces.
    // The IEndpointBehavior hooks up the message inspector to the service EndpointDispatcher.
    // The message inspector methods are invoked for every message received/sent.
    internal class Resolver : IDispatchMessageInspector, IEndpointBehavior
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        GatewayHost gateway;
        FabricClient fabric;
        RoutingExtension routingExtension;

        TimeSpan timeout = TimeSpan.FromSeconds(30);
        object routingTableLock = new object();

        public Resolver(GatewayHost gateway)
        { 
            this.gateway = gateway;
            this.fabric = new FabricClient();
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

                var key = GetPartitionKey(m);
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
            bool isRetry = gateway.IsRetry(channel.LocalAddress.Uri);

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
                this.RemoveFromRoutingTable(filter);

            log.Info(
                    "Resolved for service {0} with partition key {1}. Found {2} endpoints.",
                    request.Headers.To,
                    part.KindName,
                    rsp.Endpoints.Count);

                filter = new Filter(request.Headers.To, rsp);
                this.AddtoRoutingTable(filter);          

            return null;
        }

        ResolvedServicePartition getRsp(PartInfo part, ResolvedServicePartition prev)
        {
            try
            {
                switch (part.Kind)
                {
                    case ServicePartitionKind.Singleton:
                        return this.fabric.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, prev, this.timeout).Result;
                    case ServicePartitionKind.Int64Range:
                        return this.fabric.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.RangeKey, prev, this.timeout).Result;
                    case ServicePartitionKind.Named:
                        return this.fabric.ServiceManager.ResolveServicePartitionAsync(part.Message.Headers.To, part.NameKey, prev, this.timeout).Result;
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
            gateway.Configuration.FilterTable.GetMatchingFilter(request, out mf);
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
            this.routingExtension = endpointDispatcher.ChannelDispatcher.Host.Extensions.Find<RoutingExtension>();

            if (this.routingExtension == null)
            {
                throw new InvalidOperationException("RoutingExtension is not found. Make sure RoutingBehavior is added to the ServiceHost");
            }

            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        // Extracts the PartitionKey from the Message
        internal static string GetPartitionKey(Message request)
        {
            string partitionKey = null;

            int partitionHeaderIndex = request.Headers.FindHeader(Wcf.Partition.KeyHeader, "");
            if (partitionHeaderIndex != -1)
            {              
                partitionKey = request.Headers.GetHeader<string>(partitionHeaderIndex);
            }

            return partitionKey;
        }

        // Updates the router table with a MessageFilter and corresponding endpoints.
        void AddtoRoutingTable(Filter filter)
        {
            lock (this.routingTableLock)
            {
                var table = new MessageFilterTable<IEnumerable<ServiceEndpoint>>();
                foreach (var kv in gateway.Configuration.FilterTable)
                {
                    if (!filter.Equals(kv.Key))
                        table.Add(kv);
                }

                table.Add(new KeyValuePair<MessageFilter, IEnumerable<ServiceEndpoint>>(filter, filter.Endpoints));

                var config = new RoutingConfiguration(table, true);
                this.routingExtension.ApplyConfiguration(config);
            }

            log.Info("Routing service MessageFilter table updated.");
        }

        // Remove a MessageFilter from the RouterTable
        void RemoveFromRoutingTable(Filter filter)
        {
            lock (this.routingTableLock)
            {
                var table = new MessageFilterTable<IEnumerable<ServiceEndpoint>>();
                foreach (var kv in gateway.Configuration.FilterTable)
                {
                    if (!filter.Equals(kv.Key))
                        table.Add(kv);
                }

                var config = new RoutingConfiguration(table, true);
                this.routingExtension.ApplyConfiguration(config);
            }

            log.Info("A stale MessageFilter has been removed.");
        }
    }
}
