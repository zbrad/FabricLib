using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using Smr = System.ServiceModel.Routing;
using Smp = System.ServiceModel.Dispatcher;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public class TcpRouter : Router<TcpService>
    {
        public TcpRouter()
        {
            this.Service = new TcpService();
        }
    }

    public class RestRouter : Router<RestService>
    {
        public RestRouter()
        {
            this.Service = new RestService();
        }
    }

    public abstract class Router<T> : IRouter where T : WcfServiceBase
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public Smr.RoutingConfiguration Configuration { get; set; }
        public Smr.RoutingExtension Extension { get { return this.Service.Host.Extensions.Find<Smr.RoutingExtension>(); } }
        public Resolver Resolver { get; private set; }
        public T Service { get; protected set; }

        public virtual void Initialize(Uri path, Resolver resolver)
        {
            if (this.Service == null)
                throw new ApplicationException("Service must be initialized in constructor");

            var host = new Sm.ServiceHost(typeof(Smr.RoutingService), path);
            this.Service.Initialize(host);

            // create config
            this.Configuration = new Smr.RoutingConfiguration();
            var behavior = new Smr.RoutingBehavior(this.Configuration);

            this.Resolver = resolver;
            this.Resolver.Initialize(this);

            // set behaviors for our router endpoint
            var endpoint = host.AddServiceEndpoint(typeof(Smr.IRequestReplyRouter), this.Service.Binding, string.Empty);
            endpoint.Behaviors.Add(new MatchAll()); // match all behaviors
            endpoint.Behaviors.Add(this.Resolver);

            // add routing behavior
            this.Service.Host.Description.Behaviors.Add(behavior);
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
