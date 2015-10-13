using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;

namespace ZBrad.WcfLib
{
    public abstract class Resolver : IDispatchMessageInspector, IEndpointBehavior
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public abstract Task<Filter> UpdateFilter(Message request, Filter oldfilter);
        public abstract Task<Filter> CreateFilter(Message request);

        public IRouter Router { get; private set; }
        TimeSpan timeout = TimeSpan.FromSeconds(30);
        object routingTableLock = new object();

        public void Initialize(IRouter router)
        {
            this.Router = router;
        }

        #region IDispatchMessageInspector interface methods

        // This method is invoked by the host for every request message received. For every message received we
        // resolve the service address using the To address (service name) and the partition key using FabricClient.
        // The RouterTable is updated with the result.         
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            Filter newfilter = null;
            var filter = getFilter(request);

            if (filter == null)
                newfilter = this.CreateFilter(request).Result;
            else
                newfilter = this.UpdateFilter(request, filter).Result;

            this.updateRouting(filter, newfilter);
            return null;
        }

        Filter getFilter(Message request)
        {
            MessageFilter mf = null;
            this.Router.Configuration.FilterTable.GetMatchingFilter(request, out mf);
            return mf as Filter;
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
            this.Router.Extension = endpointDispatcher.ChannelDispatcher.Host.Extensions.Find<RoutingExtension>();

            if (this.Router.Extension == null)
            {
                throw new InvalidOperationException("RoutingExtension is not found. Make sure RoutingBehavior is added to the ServiceHost");
            }

            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        #endregion

        // Updates the router table removing a previous filter, and adding a new one
        void updateRouting(Filter oldfilter, Filter newfilter)
        {
            lock (this.routingTableLock)
            {
                var table = deltaTable(oldfilter);          // new table without "oldfilter"
                table.Add(newfilter, newfilter.Endpoints);  // add new location

                this.Router.Configuration = new RoutingConfiguration(table, true);
                this.Router.Extension.ApplyConfiguration(this.Router.Configuration);
            }

            log.Info("Routing service MessageFilter table updated.");
        }

        // creates new table without indicated filter item
        MessageFilterTable<IEnumerable<ServiceEndpoint>> deltaTable(Filter filter)
        {
            var table = new MessageFilterTable<IEnumerable<ServiceEndpoint>>();
            foreach (var kv in this.Router.Configuration.FilterTable)
            {
                if (kv.Key.Equals(filter))
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