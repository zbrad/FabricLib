using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace ZBrad.WcfLib
{
    public class TcpClient<T>
    {
        Uri path;
        Uri via;
        Binding binding = new NetTcpBinding(SecurityMode.None);
        EndpointAddress address;
        ContractDescription contract = ContractDescription.GetContract(typeof(T));
        ChannelFactory<T> factory;

        /// <summary>
        /// the current instance
        /// </summary>
        public T Instance { get; private set; }

        private TcpClient(Uri path, params IEndpointBehavior[] behaviors)
        {
            this.path = Util.GetWcfUri(path);
            address = new EndpointAddress(this.path);

            factory = new ChannelFactory<T>(binding, address);
            if (behaviors != null)
                foreach (var b in behaviors)
                    factory.Endpoint.EndpointBehaviors.Add(b);

            this.Instance = factory.CreateChannel();
        }

        private TcpClient(Uri path, Uri via, params IEndpointBehavior[] behaviors)
        {
            this.path = Util.GetWcfUri(path);
            this.via = Util.GetWcfUri(via);
            address = new EndpointAddress(this.path);

            factory = new ChannelFactory<T>(binding, address);
            factory.Endpoint.Behaviors.Add(new ClientViaBehavior(this.via));
            if (behaviors != null)
                foreach (var b in behaviors)
                    factory.Endpoint.EndpointBehaviors.Add(b);

            this.Instance = factory.CreateChannel();
        }

        public static bool TryCreate(Uri path, out TcpClient<T> cw, params IEndpointBehavior[] behaviors)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, behaviors);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static bool TryCreate(Uri path, Uri via, out TcpClient<T> cw, params IEndpointBehavior[] behaviors)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, via, behaviors);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}
