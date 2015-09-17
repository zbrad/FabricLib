using F = System.Fabric;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using System.Collections.Generic;
using System;
using System.Text;

namespace ZBrad.FabLibs.Wcf.Gateway
{
    /// <summary>
    /// A custom MessageFilter class with matches based on EndpointAddress
    /// and PartitionKey
    /// </summary>
    public class Filter : MessageFilter
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// create a <see cref="MessageFilter"/>
        /// </summary>
        /// <param name="endpoint">the address to filter</param>
        /// <param name="rsp">the service partition to target</param>
        public Filter(Uri uri, F.ResolvedServicePartition rsp)
        {
            this.EndpointUri = uri;
            this.ResolvedServicePartition = rsp;

            this.ServiceEndpoint = getServiceEndpoint(uri);
            this.Info = rsp.Info;

            log.Info("Creating filter for: " + uri);
            this.Endpoints = getServiceEndpoints();
            this.ServiceEndpoint.Behaviors.Add(new ClientViaBehavior(this.EndpointUri));
            this.Endpoints.Add(this.ServiceEndpoint);
        }

        public F.ResolvedServicePartition ResolvedServicePartition { get; private set; }
        public ServiceEndpoint ServiceEndpoint { get; private set; }
        public Uri EndpointUri { get; private set; }
        public F.ServicePartitionInformation Info { get; private set; }
        public List<ServiceEndpoint> Endpoints { get; private set; }

        public override bool Equals(object obj)
        {
            var f = obj as Filter;
            if (f == null)
                return false;

            return this.ServiceEndpoint.Address.Equals(f.ServiceEndpoint.Address) &&
                this.Info.Id.Equals(f.Info.Id);
        }

        public override int GetHashCode()
        {
            return (this.ServiceEndpoint.Address.GetHashCode() << 7) ^ this.Info.Id.GetHashCode();
        }

        // Checks if the specified message matches the MessageFilter
        public override bool Match(Message message)
        {
            if (message == null)
                return false;
            return this.MatchPartition(message) && this.EndpointUri.Equals(message.Headers.To);
        }

        // Checks if the MessageBuffer matches the MessageFilter
        public override bool Match(MessageBuffer buffer)
        {
            if (buffer == null)
                return false;
        
            Message message = buffer.CreateMessage();
            return this.Match(message);
        }

        // Matches the PatitionKey specified on the message with the MessageFilter
        bool MatchPartition(Message message)
        {
            switch (this.Info.Kind)
            {
                case F.ServicePartitionKind.Singleton:
                    return true;

                case F.ServicePartitionKind.Int64Range:
                    {
                        string key = Resolver.GetPartitionKey(message);
                        if (key == null)
                            return false;

                        long rangeKey;
                        if (!long.TryParse(key, out rangeKey))
                            return false;

                        var ranged = (F.Int64RangePartitionInformation)this.Info;
                        return rangeKey >= ranged.LowKey && rangeKey <= ranged.HighKey;
                    }
                case F.ServicePartitionKind.Named:
                    {
                        string key = Resolver.GetPartitionKey(message);
                        if (key == null)
                            return false;

                        var named = (F.NamedPartitionInformation)this.Info;
                        return key == named.Name;
                    }
            }

            return false;
        }

        ServiceEndpoint getServiceEndpoint(Uri u)
        {
            var b = getBinding(u);
            if (b == null)
                return null;

            var a = new EndpointAddress(u);
            var e = new ServiceEndpoint(GatewayHost.RequestReply, b, a);
            return e;
        }

        Binding getBinding(Uri u)
        {
            if (u.Scheme.Equals(Uri.UriSchemeHttp))
                return new WSHttpBinding(SecurityMode.None);

            if (u.Scheme.Equals(Uri.UriSchemeNetTcp))
                return new NetTcpBinding(SecurityMode.None);

            log.Error("Binding: Unexpected scheme encountered. Scheme {0}", u.Scheme);
            return null;
        }

        List<ServiceEndpoint> getServiceEndpoints()
        {
            var services = new List<ServiceEndpoint>();
            foreach (var e in this.ResolvedServicePartition.Endpoints)
            {
                if (e.Role == F.ServiceEndpointRole.Stateless || e.Role == F.ServiceEndpointRole.StatefulPrimary)
                {
                    Uri u;
                    if (Uri.TryCreate(e.Address.Replace("Tcp:", "net.tcp:"), UriKind.Absolute, out u))
                    {
                        var s = getServiceEndpoint(u);
                        if (s != null)
                            services.Add(s);
                    }
                }
            }

            StringBuilder sb = new StringBuilder("Filter created for endpoints:\n");
            foreach (var s in this.Endpoints)
                sb.AppendLine(s.Address.Uri.AbsoluteUri);
            log.Info(sb.ToString());

            return services;
        }
    }
}
