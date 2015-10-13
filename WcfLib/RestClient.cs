using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;

namespace ZBrad.WcfLib
{
    public class RestClient<T> where T : class
    {
        string path;
        Uri uri;
        Uri via;
        WebHttpBinding binding = new WebHttpBinding(WebHttpSecurityMode.None);
        EndpointAddress address;
        ServiceEndpoint endpoint;
        ContractDescription contract = ContractDescription.GetContract(typeof(T));
        IPartition partition;
        WebChannelFactory<T> factory;

        /// <summary>
        /// the current instance
        /// </summary>
        public T Instance { get; private set; }

        private RestClient(string path, IPartition partition)
        {
            this.path = path;
            this.partition = partition;
            if (!Uri.TryCreate(path.Replace("Tcp:", "net.tcp:"), UriKind.Absolute, out uri))
                throw new ArgumentException("failed to create uri", "path");

            address = new EndpointAddress(uri);
            endpoint = new ServiceEndpoint(contract, binding, address);
            if (partition != null)
                endpoint.Behaviors.Add(partition);

            factory = new WebChannelFactory<T>(binding, uri);
            this.Instance = factory.CreateChannel();
        }

        private RestClient(string path, IPartition partition, string viaPath) : this(path, partition)
        {
            if (!Uri.TryCreate(viaPath.Replace("Tcp:", "net.tcp:"), UriKind.Absolute, out via))
                throw new ArgumentException("failed to create via uri", "path");

            endpoint.Behaviors.Add(new ClientViaBehavior(via));
        }

        public static bool TryCreate(string path, out RestClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new RestClient<T>(path, null);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public static bool TryCreate(string path, string via, out RestClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new RestClient<T>(path, null, via);
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
        public static bool TryCreate(string path, IPartition partition, out RestClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new RestClient<T>(path, partition);
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
        public static bool TryCreate(string path, IPartition partition, string viaPath, out RestClient<T> cw)
        {
            cw = null;
            try
            {
                cw = new RestClient<T>(path, partition, viaPath);
                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}
