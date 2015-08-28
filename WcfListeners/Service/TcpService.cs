using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.ServiceFabric.WcfLib.Service
{
    [Sm.ServiceBehavior(InstanceContextMode = Sm.InstanceContextMode.Single,
        ConcurrencyMode = Sm.ConcurrencyMode.Multiple,
        AddressFilterMode = Sm.AddressFilterMode.Any,
        IncludeExceptionDetailInFaults = true)]
    internal class TcpService<T> where T : class, new()
    {
        // service info
        TcpListener<T> listener;

        // wcf info
        Uri uri;
        Sm.ServiceHost host;
        Sm.NetTcpBinding binding = new Sm.NetTcpBinding(Sm.SecurityMode.None);
        Sm.EndpointAddress address;
        Smd.ServiceEndpoint endpoint;
        Smd.ContractDescription contract = Smd.ContractDescription.GetContract(typeof(T));

        public IEventLog Log { get { return listener.Log; } }
        public string UriPath { get { return this.uri.AbsoluteUri; } }

        public TcpService(TcpListener<T> listener)
        {
            this.listener = listener;

            UriBuilder u = new UriBuilder(Uri.UriSchemeNetTcp, Util.Node, this.listener.Port);
            this.uri = u.Uri;

            this.address = new Sm.EndpointAddress(this.uri);
            this.endpoint = new Smd.ServiceEndpoint(this.contract, this.binding, this.address);

            this.host = new Sm.ServiceHost(listener.Instance, this.uri);
            this.host.AddServiceEndpoint(this.endpoint);

            // adds metadata 
            Smd.ServiceMetadataBehavior b = new Smd.ServiceMetadataBehavior();
            this.host.Description.Behaviors.Add(b);
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

    }
}
