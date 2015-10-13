using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace ZBrad.FabricLib.Wcf
{
    public class TcpClient<T>
    {
        Uri uri;
        Uri via;
        Binding binding = new NetTcpBinding(SecurityMode.None);
        EndpointAddress address;
        ServiceEndpoint service;
        ContractDescription contract = ContractDescription.GetContract(typeof(T));
        Partition partition;
        ChannelFactory<T> factory;

        /// <summary>
        /// the current instance
        /// </summary>
        public T Instance { get; private set; }

        private TcpClient(string path, Partition partition)
        {
            binding.OpenTimeout = TimeSpan.FromSeconds(300);
            binding.ReceiveTimeout = TimeSpan.FromSeconds(300);

            uri = getUri(path);
            this.partition = partition;
            address = new EndpointAddress(uri);
            service = new ServiceEndpoint(contract, binding, address);
            if (partition != null)
                service.Behaviors.Add(partition);

            factory = new ChannelFactory<T>(service);
            this.Instance = factory.CreateChannel(address);
        }

        static Uri getUri(string s)
        {
            UriBuilder b = new UriBuilder(s);
            if (b.Scheme.Equals("tcp"))
                b.Scheme = Uri.UriSchemeNetTcp;
            return b.Uri;
        }

        private TcpClient(string path, Partition partition, string viaPath) : this(path, partition)
        {
            uri = getUri(path);
            via = getUri(viaPath);
            service.Behaviors.Add(new ClientViaBehavior(via));
        }

        public static bool TryCreate(string path, out TcpClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, null);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static bool TryCreate(string path, string via, out TcpClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, null, via);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// create an instance of the client wrapper
        /// </summary>
        /// <param name="address">the uri address of the endpoint</param>
        /// <param name="cw">the new instance</param>
        /// <returns>true if successful</returns>
        public static bool TryCreate(string path, Partition partition, out TcpClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, partition);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        /// <summary>
        /// create an instance of the client wrapper
        /// </summary>
        /// <param name="address">the uri address of the endpoint</param>
        /// <param name="viaAddress">via the gateway address</param>
        /// <param name="cw">the new instance</param>
        /// <returns>true if successful</returns>
        public static bool TryCreate(string path, Partition partition, string viaPath, out TcpClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new TcpClient<T>(path, partition, viaPath);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}
