using F = System.Fabric;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Description;
using System.ServiceModel.Routing;
using System.Collections.Generic;
using System;
using System.Text;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf
{
    /// <summary>
    /// A custom MessageFilter class with matches based on EndpointAddress
    /// and PartitionKey
    /// </summary>
    internal class FabricFilter : Filter, IEquatable<FabricFilter>
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Initialize(Uri retry, PartInfo part, F.ResolvedServicePartition rsp)
        {
            this.ResolvedServicePartition = rsp;
            this.Info = rsp.Info;
            this.Part = part;

            var uris = getUris();
            base.Initialize(part.Message.Headers.To, uris);

            // now add a router retry endpoint
            var retryEndpoint = new RouterEndpoint(
                this.Endpoints[0].Contract,
                this.Endpoints[0].Binding,
                new EndpointAddress(part.Message.Headers.To));
            retryEndpoint.Behaviors.Add(new ClientViaBehavior(retry));
            this.Endpoints.Add(retryEndpoint);
        }

        List<Uri> getUris()
        {
            List<Uri> list = new List<Uri>();
            foreach (var rse in this.ResolvedServicePartition.Endpoints)
                if (rse.Role == System.Fabric.ServiceEndpointRole.Stateless || rse.Role == System.Fabric.ServiceEndpointRole.StatefulPrimary)
                    list.Add(new Uri(rse.Address));

            return list;
        }

        public F.ResolvedServicePartition ResolvedServicePartition { get; private set; }
        public F.ServicePartitionInformation Info { get; private set; }
        public PartInfo Part { get; private set; }
        public bool Equals(FabricFilter ff)
        {
            if (!base.Equals(ff))
                return false;
            return this.Info.Id.Equals(ff.Info.Id);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FabricFilter);
        }

        public override int GetHashCode()
        {
            return (base.GetHashCode() << 7) ^ this.Info.Id.GetHashCode();
        }

        // Checks if the specified message matches the MessageFilter
        public override bool Match(Message message)
        {
            return base.Match(message) && this.MatchPartition(message);
        }

        // Extracts the PartitionKey from the Message
        public static string GetPartitionKey(Message request)
        {
            string partitionKey = null;

            int partitionHeaderIndex = request.Headers.FindHeader(Wcf.Partition.KeyHeader, "");
            if (partitionHeaderIndex != -1)
            {
                partitionKey = request.Headers.GetHeader<string>(partitionHeaderIndex);
            }

            return partitionKey;
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
                        string key = GetPartitionKey(message);
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
                        string key = GetPartitionKey(message);
                        if (key == null)
                            return false;

                        var named = (F.NamedPartitionInformation)this.Info;
                        return key == named.Name;
                    }
            }

            return false;
        }
    }
}
