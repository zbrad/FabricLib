using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using Smr = System.ServiceModel.Routing;
using Smp = System.ServiceModel.Dispatcher;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public class TcpGatewayService : TcpService, IRouter
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public Smr.RoutingConfiguration Configuration { get; set; }
        public Smr.RoutingBehavior Behavior { get; set; }
        public Smr.RoutingExtension Extension { get; set; }

        public Resolver Resolver { get; private set; }

        protected override Smc.Binding GetBinding()
        {
            return new Sm.NetTcpBinding(Sm.SecurityMode.None);
        }

        protected override Smd.ContractDescription GetContract(object instance)
        {
            return Smd.ContractDescription.GetContract(typeof(Smr.IRequestReplyRouter));
        }

        protected override Sm.ServiceHost GetHost(object instance)
        {
            var host = new Sm.ServiceHost(typeof(Smr.RoutingService), this.GatewayPath);
            this.Endpoint = host.AddServiceEndpoint(typeof(Smr.IRequestReplyRouter), this.Binding, string.Empty);
            return host;
        }

        public Uri GatewayPath { get; private set; }

        protected override Smd.ServiceEndpoint GetEndpoint(Uri path)
        {
            this.GatewayPath = path;
            return null;
        }

        public void Initialize(Uri path, Resolver resolver)
        {
            base.Initialize(path, null);

            // match all behaviors
            this.Endpoint.Behaviors.Add(new MatchAll());

            // create config
            this.Configuration = new Smr.RoutingConfiguration();
            this.Behavior = new Smr.RoutingBehavior(this.Configuration);

            this.Resolver = resolver;
            this.Resolver.Initialize(this);

            // set the resolver for endpoints
            this.Endpoint.Behaviors.Add(this.Resolver);

            // add routing behavior
            this.Host.Description.Behaviors.Add(this.Behavior);
        }
    }


    class MatchAll : Smd.IEndpointBehavior
    {
        public void AddBindingParameters(Smd.ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyClientBehavior(Smd.ServiceEndpoint endpoint, Smp.ClientRuntime clientRuntime)
        {
        }

        public void ApplyDispatchBehavior(Smd.ServiceEndpoint endpoint, Smp.EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.AddressFilter = new Smp.MatchAllMessageFilter();
        }

        public void Validate(Smd.ServiceEndpoint endpoint)
        {
        }
    }



}
