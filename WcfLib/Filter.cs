using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using System.ServiceModel.Routing;
using System.Collections.Generic;
using System;
using System.Text;

namespace ZBrad.WcfLib
{
    public class RouterEndpoint : ServiceEndpoint, IEquatable<RouterEndpoint>
    {
        public RouterEndpoint(ContractDescription contract, Binding binding, EndpointAddress address)
            : base(contract, binding, address)
        {

        }

        public bool Equals(RouterEndpoint other)
        {
            if (other == null)
                return false;

            return this.Address.Uri.Equals(other.Address.Uri);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RouterEndpoint);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }

    public sealed class RoutedClientEndpoint : RouterEndpoint
    {
        public RoutedClientEndpoint(ContractDescription contract, Binding binding, EndpointAddress address)
            : base(contract, binding, address)
        {

        }
    }

    /// <summary>
    /// A custom MessageFilter class which matches based on the overridable method Match
    /// </summary>
    public abstract class Filter : MessageFilter, IEquatable<Filter>
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// create a <see cref="MessageFilter"/>
        /// </summary>
        public Filter()
        {
            this.Endpoints = new List<RouterEndpoint>();
        }

        public List<RouterEndpoint> Endpoints { get; private set; }
        public Uri Requested { get; private set; }
        public virtual void Initialize(Uri requested, IList<Uri> endpoints)
        {
            this.Requested = requested;

            foreach (var u in endpoints)
            {
                var path = Util.GetWcfUri(u);
                log.Info("Creating filter for: " + path);

                var client = getClientEndpoint(path);
                this.Endpoints.Add(client);
            }
        }

        public virtual bool Equals(Filter f)
        {
            if (f == null)
                return false;

            return Util.Equals(this.Endpoints, f.Endpoints);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Filter);
        }

        public override int GetHashCode()
        {
            int code = 0;
            foreach (var e in this.Endpoints)
                code = (code << 7) ^ e.Address.GetHashCode();
            return code;
        }

        // Checks if the specified message matches the MessageFilter
        public override bool Match(Message message)
        {
            if (message == null)
                return false;

            return this.Requested.Equals(message.Headers.To);
        }

        // Checks if the MessageBuffer matches the MessageFilter
        public override bool Match(MessageBuffer buffer)
        {
            if (buffer == null)
                return false;
        
            Message message = buffer.CreateMessage();
            return this.Match(message);
        }

        static ContractDescription routerContract = ContractDescription.GetContract(typeof(IRequestReplyRouter));
        static RoutedClientEndpoint getClientEndpoint(Uri u)
        {
            var b = getBinding(u);
            if (b == null)
                return null;

            var a = new EndpointAddress(u);
            var e = new RoutedClientEndpoint(routerContract, b, a);
            return e;
        }

        static Binding getBinding(Uri u)
        {
            if (u.Scheme.Equals(Uri.UriSchemeHttp))
                return new WSHttpBinding(SecurityMode.None);

            if (u.Scheme.Equals(Uri.UriSchemeNetTcp))
                return new NetTcpBinding(SecurityMode.None);

            log.Error("Binding: Unexpected scheme encountered. Scheme {0}", u.Scheme);
            return null;
        }
    }
}
