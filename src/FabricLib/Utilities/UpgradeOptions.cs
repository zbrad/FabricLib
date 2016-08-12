using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Fabric;
using System.Fabric.Description;

namespace ZBrad.FabricLib.Utilities
{
    public sealed class UpgradeOptions
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(600);

        public bool IsMonitored { get; set; } = true;
        public TimeSpan Timeout { get; set; } = DefaultTimeout;

        public UpgradePolicyDescription CreatePolicy()
        {

            var policy = new RollingUpgradePolicyDescription
            {
                ForceRestart = true,
                UpgradeMode = RollingUpgradeMode.Monitored,
                UpgradeReplicaSetCheckTimeout = Timeout
            };

            if (!IsMonitored)
                policy.UpgradeMode = RollingUpgradeMode.UnmonitoredAuto;

            return policy;
        }

    }
}
