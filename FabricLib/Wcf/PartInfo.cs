using System;
using System.Collections.Generic;
using System.Fabric;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;
using System.Text;
using System.Threading.Tasks;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf
{
    internal class PartInfo
    { 
        public Message Message { get; set; }
        public ServicePartitionKind Kind { get; set; }
        public string KindName { get { return Enum.GetName(typeof(ServicePartitionKind), this.Kind); } }
        public string NameKey { get; set; }
        public long RangeKey { get; set; }

        public PartInfo(Message m)
        {
            this.Message = m;
            this.Kind = ServicePartitionKind.Singleton;

            var key = FabricFilter.GetPartitionKey(m);
            if (key == null)
                return;

            long ranged;
            if (long.TryParse(key, out ranged))
            {
                this.Kind = ServicePartitionKind.Int64Range;
                this.RangeKey = ranged;
                return;
            }

            this.NameKey = key;
            this.Kind = ServicePartitionKind.Named;
        }

        public override string ToString()
        {
            switch (this.Kind)
            {
                case ServicePartitionKind.Singleton:
                    return "S";
                case ServicePartitionKind.Named:
                    return "N-" + this.NameKey;
                case ServicePartitionKind.Int64Range:
                    return "R-" + this.RangeKey;
            }

            throw new ApplicationException("invalid kind state");
        }
    }

}
