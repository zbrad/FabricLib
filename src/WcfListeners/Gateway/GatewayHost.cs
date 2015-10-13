using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;
using System.Collections.Generic;
using ZBrad.FabLibs.Utilities;

namespace ZBrad.FabLibs.Wcf.Gateway
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        AddressFilterMode = AddressFilterMode.Any,
        IncludeExceptionDetailInFaults = true)]
    internal class GatewayHost
    {
        public static readonly ContractDescription RequestReply = ContractDescription.GetContract(typeof(IRequestReplyRouter));

        // service info
        GatewayListener listener = null;

        // wcf info
        Uri uri = null;
        ServiceHost host = null;
        NetTcpBinding incomingBinding = new NetTcpBinding(SecurityMode.None);
        NetTcpBinding retryBinding = new NetTcpBinding(SecurityMode.None);
        ServiceEndpoint incomingEndpoint = null;
        ServiceEndpoint retryEndpoint = null;
        public RoutingConfiguration Configuration { get; private set; }

        RoutingBehavior behavior = null;

        public GatewayHost(GatewayListener listener)
        {
            this.listener = listener;
            this.Configuration = new RoutingConfiguration();

            UriBuilder b = new UriBuilder(Uri.UriSchemeNetTcp, Utility.Node, this.listener.Port);
            this.uri = b.Uri;

            // create router
            this.host = new ServiceHost(typeof(RoutingService), this.uri);
            this.behavior = new RoutingBehavior(this.Configuration);
            this.host.Description.Behaviors.Add(this.behavior);
            this.host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;

            // add incoming endpoint
            this.incomingEndpoint = this.host.AddServiceEndpoint(typeof(IRequestReplyRouter), this.incomingBinding, string.Empty);
            this.incomingEndpoint.Behaviors.Add(new MatchAll());

            // add retry endpoint
            this.retryEndpoint = this.host.AddServiceEndpoint(typeof(IRequestReplyRouter), this.retryBinding, "retry");

            //IEndpointBehavior resolver = new Resolver(Log, this.Configuration, this.resolvedEndpoint);
            var resolver = new Resolver(this);

            // set the resolver for endpoints
            this.incomingEndpoint.Behaviors.Add(resolver);
            this.retryEndpoint.Behaviors.Add(resolver);
        }

        // service host address
        public string Address { get { return this.uri.AbsoluteUri; } }

        public bool IsRetry(Uri u)
        {
            return u.Equals(this.retryEndpoint.Address.Uri);
        }

        /// <summary>
        /// start listening for requests
        /// </summary>
        public void StartListening()
        {
            this.host.Open();
        }

        /// <summary>
        /// stop listening for requests
        /// </summary>
        public void StopListening()
        {
            this.host.Close();
        }

        class MatchAll : IEndpointBehavior
        {
            public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
            {
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
            {
                endpointDispatcher.AddressFilter = new MatchAllMessageFilter();
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }
        }
    }
}
