using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.FabLibs.Utilities
{
    public static class Defaults
    {
        public const string ProvisionPrefix = "incoming";
        public static readonly TimeSpan WaitMaximum = TimeSpan.FromSeconds(59);
        public const int WaitRetryLimit = 5;
        public static readonly TimeSpan WaitDelay = TimeSpan.FromSeconds(7);
        public static readonly TimeSpan UpgradeTimeout = TimeSpan.FromSeconds(7);
        public const string ProvisionData = "FabricDataRoot";
        public const string ProvisionApp = "FabricDoePath";
        public const string EnvProvisionData = "FabricDataRoot";
        public const string EnvProvisionApp = "FabricCodePath";
        public const string EnvFabricPath = "FabricRoot";
        public const string ConnectionHost = "localhost";
        public const string ConnectionPort = "19000";

        public const string ApplicationName = "ApplicationName";
        public const string ApplicationNamespace = "ApplicationNamespace";
        public const string ApplicationAddress = "ApplicationAddress";
        public const string ApplicationVersion = "ApplicationVersion";
        public const string ApplicationParameters = "ApplicationParameters";

        public const string ServiceName = "ServiceName";
        public const string ServiceType = "ServiceType";
        public const string ServiceAddress = "ServiceAddress";
        public const string ServiceEndpointName = "ServiceEndpointName";
        public const string ReplicaEndpointName = "ReplicaEndpointName";

        public const string ServiceExeHost = "ServiceExeHost";
        public const string ServiceCodeFolder = "ServiceCodeFolder";

    }
}
