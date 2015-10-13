using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public abstract class WcfServiceBase
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // wcf info
        public Sm.ServiceHost Host { get; protected set; }
        public Smc.Binding Binding { get; protected set; }
        public Smd.ServiceEndpoint Endpoint { get; protected set; }
        public Smd.ContractDescription Contract { get; protected set; }

        public string UriPath { get { return this.Endpoint.Address.Uri.AbsoluteUri; } }

        public bool IsListening { get { return this.Host.State == Sm.CommunicationState.Opened; } }
        protected abstract Smc.Binding GetBinding();

        protected virtual Smd.ContractDescription GetContract(object instance)
        {
           return Smd.ContractDescription.GetContract(instance.GetType());
        }

        protected virtual Sm.ServiceHost GetHost(object instance)
        {
            var host = new Sm.ServiceHost(instance);
            host.AddServiceEndpoint(this.Endpoint);
            return host;
        }

        protected virtual Smd.ServiceEndpoint GetEndpoint(Uri path)
        {
            return new Smd.ServiceEndpoint(this.Contract, this.Binding, new Sm.EndpointAddress(path));
        }

        public virtual void Initialize(Uri path, object instance)
        {
            if (path.Scheme.Equals("tcp"))
            {
                var b = new UriBuilder(path);
                b.Scheme = Uri.UriSchemeNetTcp;
                path = b.Uri;
            }

            this.Contract = GetContract(instance);
            this.Binding = GetBinding();
            this.Endpoint = GetEndpoint(path);
            this.Host = GetHost(instance);
            initHost();
        }

    void initHost()
        {
            // Programmatically set service behaviors:
            //    [Sm.ServiceBehavior(
            //        InstanceContextMode = Sm.InstanceContextMode.Single,
            //        ConcurrencyMode = Sm.ConcurrencyMode.Multiple,
            //        IncludeExceptionDetailInFaults = true)]

            var b = this.Host.Description.Behaviors.Find<Sm.ServiceBehaviorAttribute>();
            b.InstanceContextMode = Sm.InstanceContextMode.Single;
            b.ConcurrencyMode = Sm.ConcurrencyMode.Multiple;
            b.IncludeExceptionDetailInFaults = true;
        }

        /// <summary>
        /// start listening for requests
        /// </summary>
        public void StartListening()
        {
            log.Info("Start listening on {0}", this.Endpoint.Address.Uri.AbsoluteUri);
            this.Host.Open();
        }

        /// <summary>
        /// stop listening for requests
        /// </summary>
        public void StopListening()
        {
            log.Info("Stop listening on {0}", this.Endpoint.Address.Uri.AbsoluteUri);
            this.Host.Close();
        }

    }

}
