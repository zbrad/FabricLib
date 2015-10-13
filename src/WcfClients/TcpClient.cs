using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace ZBrad.FabLibs.Wcf
{
    public class TcpClient<T>
    {
        string path;
        Uri uri;
        Uri via;
        Binding binding = new NetTcpBinding(SecurityMode.None);
        EndpointAddress address;
        ServiceEndpoint endpoint;
        ContractDescription contract = ContractDescription.GetContract(typeof(T));
        Partition partition;
        ChannelFactory<T> factory;

        /// <summary>
        /// the current instance
        /// </summary>
        public T Instance { get; private set; }

        private TcpClient(string path, Partition partition)
        {
            this.path = path;
            this.partition = partition;
            var netpath = path.Replace("Tcp:", "net.tcp:");
            if (!Uri.TryCreate(path.Replace("Tcp:", "net.tcp:"), UriKind.Absolute, out uri))
                throw new ArgumentException("failed to create uri", "path");

            address = new EndpointAddress(uri);
            endpoint = new ServiceEndpoint(contract, binding, address);
            if (partition != null)
                endpoint.Behaviors.Add(partition);

            factory = new ChannelFactory<T>(endpoint);
            this.Instance = factory.CreateChannel();
        }

        private TcpClient(string path, Partition partition, string viaPath) : this(path, partition)
        {
            if (!Uri.TryCreate(viaPath.Replace("Tcp:", "net.tcp:"), UriKind.Absolute, out via))
                throw new ArgumentException("failed to create via uri", "path");

            endpoint.Behaviors.Add(new ClientViaBehavior(via));
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
