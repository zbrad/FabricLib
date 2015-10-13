using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZBrad.WcfLib
{
    public abstract class WcfServiceBase : IWcfService
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // wcf info
        public Sm.ServiceHost Host { get; private set; }
        public abstract Smc.Binding Binding { get; }
        //public Smd.ContractDescription Contract { get; private set; }

        public Uri Uri { get { return this.Host.BaseAddresses[0]; } }

        public bool IsListening { get { return this.Host.State == Sm.CommunicationState.Opened; } }

        public virtual void Initialize(Uri path, object instance)
        {
            var contract = Smd.ContractDescription.GetContract(instance.GetType());

            var endpoint = new Smd.ServiceEndpoint(
                contract,
                this.Binding,
                new Sm.EndpointAddress(path));

            this.Host = new Sm.ServiceHost(instance, endpoint.ListenUri);
            this.Host.AddServiceEndpoint(endpoint);
            initHost();
        }

        public virtual void Initialize(Sm.ServiceHost host)
        {
            this.Host = host;
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
        public Task StartAsync()
        {
            return Task.Run(() =>
            {
                log.Info("Start listening on {0}", this.Uri);
                this.Host.Open();
            });
        }

        /// <summary>
        /// stop listening for requests
        /// </summary>
        public Task StopAsync()
        {
            return Task.Run(() =>
            {
                log.Info("Stop listening on {0}", this.Uri);
                this.Host.Close();
            });
        }

    }

}
