using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace ZBrad.FabLibs.Utilities.x64
{
    /// <summary>
    /// upgrade monitor options
    /// </summary>
    public enum UpgradeMonitor
    {
        /// <summary>
        /// auto upgrade
        /// </summary>
        Auto,

        /// <summary>
        /// manual upgrade
        /// </summary>
        Manual
    }

    /// <summary>
    /// Fabric Client control methods for 64-bit
    /// </summary>
    public class Control64
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        static Dictionary<string, string> emptyArgs = new Dictionary<string, string>();

        private FabricClient fc = null;
        private Control control = new Control();

        /// <summary>
        /// a default empty container
        /// </summary>
        public Control64()
        {
        }

        /// <summary>
        /// creates a control container
        /// </summary>
        /// <param name="clusterSettings">cluster settings to use</param>
        /// <param name="packageSettings">package settings to use</param>
        public Control64(ClusterSettings clusterSettings, PackageSettings packageSettings)
        {
            if (clusterSettings != null)
            {
                foreach (string k in clusterSettings.Data.Keys)
                {
                    this.control.Cluster.Data[k] = clusterSettings.Data[k];
                }
            }

            if (packageSettings != null)
            {
                foreach (string k in packageSettings.Data.Keys)
                {
                    this.control.Package.Data[k] = packageSettings.Data[k];
                }
            }
        }

        /// <summary>
        /// gets the last exception
        /// </summary>
        public Exception LastException { get; private set; }

        /// <summary>
        /// gets the last error text
        /// </summary>
        public string LastError { get; private set; }

        /// <summary>
        /// gets the last informational text
        /// </summary>
        public string LastInfo { get; private set; }

        /// <summary>
        /// gets current control instance
        /// </summary>
        /// <value>control instance</value>
        public Control Control { get { return this.control; } }

        /// <summary>
        /// test if fabric cluster is running
        /// </summary>
        /// <returns>true if running</returns>
        #pragma warning disable 612,618
        public bool IsClusterRunning
        {
            get
            {
                this.LastException = null;

                if (this.IsOneBoxStopped)
                {
                    return false;
                }


                if (this.Control.Cluster.Data.ContainsKey("Thumbprint") && this.control.Cluster.Data.ContainsKey("CommonName"))
                {
                    X509Credentials xc = new X509Credentials();

                    string shortThumb = this.Control.Cluster.Data["Thumbprint"].Replace(" ", "").ToUpper();
                    X509Store store = new X509Store(StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection cc = store.Certificates.Find(X509FindType.FindByThumbprint, shortThumb, true);
                    if (cc == null || cc.Count == 0)
                    {
                        this.Error("Could not find certificate by thumbprint");
                        return false;
                    }

                    X509Certificate2 cert = cc[0];
                    xc.StoreLocation = store.Location;
                    xc.FindType = X509FindType.FindByThumbprint;
                    xc.FindValue = cert.Thumbprint;
                    xc.AllowedCommonNames.Add(cert.FriendlyName);
                    xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;

                    this.fc = new FabricClient(xc, this.Control.Cluster.Connection);
                }
                else
                {
                    this.fc = new FabricClient(this.Control.Cluster.Connection);
                }

                int retryLimit = Defaults.WaitRetryLimit;
                while (retryLimit-- > 0)
                {
                    try
                    {
                        bool nameExistsResult = this.fc.PropertyManager.NameExistsAsync(
                            new Uri(Defaults.ApplicationNamespace + "/Any"),
                            Defaults.WaitDelay,
                            CancellationToken.None).Result;
                        if (!nameExistsResult)
                        {
                            // this is success!
                            return true;
                        }

                        return true;
                    }
                    catch (AggregateException ae)
                    {
                        this.LastException = ae;
                        if (!this.IsRetry("NameExists", ae.InnerException))
                        {
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        this.LastException = e;
                        if (!this.IsRetry("NameExists", e))
                        {
                            return false;
                        }
                    }

                    this.Info("FabricClient failed to connect, retrying...");
                    Task.Delay(Defaults.WaitDelay).Wait();
                }

                this.Error("FabricClient connect failed too many times");
                return false;
            }
        }

        private bool IsOneBoxStopped
        {
            get
            {
                if (this.Control.Cluster.ClusterType == ClusterType.OneBox && !this.control.IsHostRunning)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// sets tracing status
        /// </summary>
        /// <param name="isLogging">true to start tracing</param>
        public void SetLogging(bool isLogging)
        {
            if (isLogging)
                NLog.LogManager.EnableLogging();
            else
                NLog.LogManager.DisableLogging();
        }

        /// <summary>
        /// set the current package definitions
        /// </summary>
        /// <param name="packageData">definitions to use</param>
        public void SetPackage(ConcurrentDictionary<string, string> packageData)
        {
            this.control.Package.Data = packageData;
        }

        /// <summary>
        /// set the current cluster definitions
        /// </summary>
        /// <param name="clusterData">definitions to use</param>
        public void SetCluster(Dictionary<string, string> clusterData)
        {
            this.control.Cluster.Data = clusterData;
        }

        /// <summary>
        /// try to provision the application
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryApplicationProvision()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            int limit = Defaults.WaitRetryLimit;
            while (limit-- > 0)
            {
                string imagePath = Defaults.ProvisionPrefix + "\\" + this.control.Package.ApplicationTypeName;
                try
                {
                    this.Info("ProvisionApplication with path={0}", imagePath);
                    this.fc.ApplicationManager
                      .ProvisionApplicationAsync(imagePath, Defaults.WaitDelay, CancellationToken.None)
                      .Wait();

                    this.Info("Application provision successful");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;
                    if (!this.IsRetry("Provision", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("Provision", e))
                    {
                        return false;
                    }
                }

                this.Info("Application provision failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            this.Error("Provision failed too many times");
            return false;
        }

        /// <summary>
        /// try to un-provision application
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryApplicationUnprovision()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            if (string.IsNullOrEmpty(this.control.Package.ApplicationTypeName) || string.IsNullOrEmpty(this.control.Package.ApplicationVersion))
            {
                return false;
            }

            int limit = Defaults.WaitRetryLimit;
            while (limit-- > 0)
            {
                try
                {
                    this.fc.ApplicationManager
                      .UnprovisionApplicationAsync(this.control.Package.ApplicationTypeName, this.control.Package.ApplicationVersion, Defaults.WaitDelay, CancellationToken.None)
                      .Wait();

                    this.Info("Application unprovision successful");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;

                    if (!this.IsRetry("Unprovision", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("Unprovision", e))
                    {
                        return false;
                    }
                }

                this.Info("Application unprovision failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            this.Error("Unprovision failed too many times");
            return false;
        }

        /// <summary>
        /// try to create application
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryApplicationCreate()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                return false;
            }

            if (this.control.Package.ApplicationAddress == null)
            {
                this.Error("ApplicationAddress is null");
                return false;
            }

            if (this.control.Package.ApplicationTypeName == null)
            {
                this.Error("ApplicationTypeName is null");
                return false;
            }

            if (this.control.Package.ApplicationVersion == null)
            {
                this.Error("ApplicationVersion is null");
                return false;
            }

            NameValueCollection nvc = GetApplicationParameters(this.control.Package);
            ApplicationDescription d = new ApplicationDescription(new Uri(this.control.Package.ApplicationAddress), this.control.Package.ApplicationTypeName, this.control.Package.ApplicationVersion, nvc);

            int retryLimit = Defaults.WaitRetryLimit;
            while (retryLimit-- > 0)
            {
                try
                {
                    this.fc.ApplicationManager
                      .CreateApplicationAsync(d, Defaults.WaitDelay, CancellationToken.None)
                      .Wait();

                    this.Info("Application create successful");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;

                    if (!this.IsRetry("CreateApplication", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("CreateApplication", e))
                    {
                        return false;
                    }
                }

                this.Info("Application create failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            this.Error("CreateApplication failed too many times");
            return false;
        }

        /// <summary>
        /// try to delete application
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryApplicationDelete()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            if (string.IsNullOrEmpty(this.control.Package.ApplicationAddress))
            {
                this.Error("Could not find application address in package settings");
                return false;
            }

            int limit = Defaults.WaitRetryLimit;
            while (limit-- > 0)
            {
                try
                {
                    Task t = this.fc.ApplicationManager.DeleteApplicationAsync(
                        new Uri(this.control.Package.ApplicationAddress),
                        Defaults.WaitDelay,
                        CancellationToken.None);
                    t.Wait();

                    this.Info("Application delete successful");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;

                    if (!this.IsRetry("DeleteApplication", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("DeleteApplication", e))
                    {
                        return false;
                    }
                }

                this.Info("Application delete failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            this.Error("Application delete failed too many times");
            return false;
        }

        /// <summary>
        /// start an application upgrade
        /// </summary>
        /// <param name="fromPackage">from package</param>
        /// <param name="toPackage">to package</param>
        /// <param name="isRolling">specify if rolling</param>
        /// <param name="mode">monitor mode</param>
        /// <param name="timeout">timeout for upgrade start</param>
        /// <returns>true if upgrade started</returns>
        public bool TryApplicationUpgrade(PackageSettings fromPackage, PackageSettings toPackage, bool isRolling, UpgradeMonitor mode, TimeSpan timeout)
        {
            this.LastException = null;

            try
            {
                ApplicationUpgradeDescription upgradeDescription = new ApplicationUpgradeDescription();

                // we use the "from" package's address
                upgradeDescription.ApplicationName = new Uri(fromPackage.ApplicationAddress);

                // we use the "to" package's version
                upgradeDescription.TargetApplicationTypeVersion = toPackage.ApplicationVersion;

                // split parameters if any are specified
                NameValueCollection upgradeParams = Control64.GetApplicationParameters(toPackage);
                if (upgradeParams != null && upgradeParams.Count > 0)
                {
                    upgradeDescription.ApplicationParameters.Add(upgradeParams);
                }

                RollingUpgradePolicyDescription policy = new RollingUpgradePolicyDescription();
                policy.ForceRestart = true;
                policy.UpgradeMode = RollingUpgradeMode.UnmonitoredManual;

                if (isRolling)
                {
                    switch (mode)
                    {
                        case UpgradeMonitor.Auto:
                            policy.UpgradeMode = RollingUpgradeMode.UnmonitoredAuto;
                            break;
                        case UpgradeMonitor.Manual:
                            policy.UpgradeMode = RollingUpgradeMode.UnmonitoredManual;
                            break;
                    }

                    policy.UpgradeReplicaSetCheckTimeout = timeout;
                }

                upgradeDescription.UpgradePolicyDescription = policy;
                this.fc.ApplicationManager.UpgradeApplicationAsync(upgradeDescription, Defaults.UpgradeTimeout, CancellationToken.None).Wait();

                return true;
            }
            catch (Exception e)
            {
                this.LastException = e;

                if (e is AggregateException)
                {
                    if (e is FabricException)
                    {
                        this.Error("Upgrade failed: " + e.InnerException.Message);
                        return false;
                    }

                    this.Error("UpgradeApplication Aggregate failure, err={0}", e.InnerException.Message);
                }
                else
                {
                    this.Error("UpgradeApplication failed, err={0}", e.Message);
                }
            }

            return false;
        }

        /// <summary>
        /// try to create service
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryServiceCreate()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            if (this.control.Package.ApplicationAddress == null)
            {
                this.Error("ApplicationAddress is null");
                return false;
            }

            if (this.control.Package.ServiceAddress == null)
            {
                this.Error("ServiceAddress is null");
                return false;
            }

            if (this.control.Package.ServiceType == null)
            {
                this.Error("ServiceType is null");
                return false;
            }

            int limit = Defaults.WaitRetryLimit;
            while (limit-- > 0)
            {
                try
                {
                    ServiceDescription sd;
                    if (!this.TryCreateServiceDescription(out sd))
                    {
                        return false;
                    }

                    this.fc.ServiceManager.CreateServiceAsync(sd, Defaults.WaitDelay, CancellationToken.None).Wait();
                    this.Info("CreateService succeeded");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;

                    if (!this.IsRetry("CreateService", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("CreateService", e))
                    {
                        return false;
                    }
                }

                this.Info("Application service create failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            return false;
        }

        /// <summary>
        /// try to delete service
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryServiceDelete()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            if (string.IsNullOrEmpty(this.control.Package.ServiceAddress))
            {
                this.Error("ServiceAddress not found in package settings");
                return false;
            }

            int limit = Defaults.WaitRetryLimit;
            while (limit-- > 0)
            {
                try
                {
                    string address = this.control.Package.ServiceAddress;
                    Uri serviceUri = new Uri(address);
                    this.fc.ServiceManager.DeleteServiceAsync(serviceUri, Defaults.WaitDelay, CancellationToken.None)
                      .Wait();
                    this.Info("DeleteService succeeded");
                    return true;
                }
                catch (AggregateException ae)
                {
                    this.LastException = ae;

                    if (!this.IsRetry("DeleteService", ae.InnerException))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    this.LastException = e;

                    if (!this.IsRetry("DeleteService", e))
                    {
                        return false;
                    }
                }

                this.Info("Application service delete failed, retrying...");
                Task.Delay(Defaults.WaitDelay).Wait();
            }

            return false;
        }

        /// <summary>
        /// try to fully create current package
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryPackageCreate()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                return false;
            }

            if (!this.TryApplicationProvision())
            {
                this.Error("Provision application failed");
                return false;
            }

            if (!this.TryApplicationCreate())
            {
                this.Error("Create application failed");
                return false;
            }

            if (!this.TryServiceCreate())
            {
                this.Error("Create service failed");
                return false;
            }

            this.Info("PackageCreate succeeded");
            return true;
        }

        /// <summary>
        /// try to fully delete current package
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryPackageDelete()
        {
            this.LastException = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            bool hasError = false;
            if (!this.TryServiceDelete())
            {
                this.Error("delete service failed");
                hasError = true;
            }

            if (!this.TryApplicationDelete())
            {
                this.Error("delete application failed");
                hasError = true;
            }

            if (!this.TryApplicationUnprovision())
            {
                this.Error("unprovision failed");
                hasError = true;
            }

            if (hasError)
                this.Info("PackageDelete completed with errors");
            else
                this.Info("PackageDelete succeeded");
            return true;
        }

        /// <summary>
        /// get the status for the specified instance
        /// </summary>
        /// <param name="instance">the instance to use</param>
        /// <param name="status">the current status</param>
        /// <returns>true if retrieved</returns>
        public bool TryGetStatus(ApplicationInstance instance, out string status)
        {
            status = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            try
            {
                ApplicationList list = this.fc.QueryManager.GetApplicationListAsync(instance.Name).Result;
                if (list != null && list.Count > 0)
                {
                    Application a = list[0];
                    status = a.ApplicationStatus.ToString();
                    return true;
                }
            }
            catch (AggregateException ae)
            {
                this.Error("GetApplicationList failed, exception: {0}", ae.InnerException.Message);
            }
            catch (Exception e)
            {
                this.Error("GetApplicationList failed, exception: {0}", e.Message);
            }

            return false;
        }

        /// <summary>
        /// try to get application instances
        /// </summary>
        /// <param name="instances">the instance list</param>
        /// <returns>true if successful</returns>
        public bool TryGetApplicationInstances(out List<ApplicationInstance> instances)
        {
            instances = null;

            if (!this.IsClusterRunning)
            {
                this.Error("Cluster is not running");
                return false;
            }

            try
            {
                ApplicationList list = this.fc.QueryManager.GetApplicationListAsync(null, Defaults.WaitDelay, CancellationToken.None).Result;
                instances = new List<ApplicationInstance>();
                foreach (Application a in list)
                {
                    instances.Add(new ApplicationInstance(a.ApplicationName, a.ApplicationTypeName, a.ApplicationTypeVersion, a.ApplicationStatus.ToString()));
                }

                return true;
            }
            catch (AggregateException ae)
            {
                this.Error("GetApplicationList failed, exception: {0}", ae.InnerException.Message);
            }
            catch (Exception e)
            {
                this.Error("GetApplicationList failed, exception: {0}", e.Message);
            }

            return false;
        }

        static NameValueCollection GetApplicationParameters(PackageSettings package)
        {
            NameValueCollection nvc = new NameValueCollection();
            if (package.ApplicationParameters != null && !string.IsNullOrWhiteSpace(package.ApplicationParameters))
            {
                string[] values = package.ApplicationParameters.Split(PackageSettings.ApplicationParameterSplitChar);
                for (int i = 0; i < values.Length; i++)
                {
                    string[] nv = values[i].Split('=');
                    if (nv.Length == 2)
                    {
                        nvc[nv[0]] = nv[1];
                    }
                }
            }

            return nvc;
        }

        private bool TryCreatePartitionDescription(out PartitionSchemeDescription psd)
        {
            psd = null;

            if (this.control.Package.Data.ContainsKey("PartitionSingleton"))
            {
                psd = new SingletonPartitionSchemeDescription();
                return true;
            }

            if (this.control.Package.Data.ContainsKey("PartitionNamed"))
            {
                NamedPartitionSchemeDescription d = new NamedPartitionSchemeDescription();

                string nameListValue;
                if (!this.control.Package.Data.TryGetValue("PartitionNamedList", out nameListValue))
                {
                    this.Error("named partition specified, but no named list was found");
                    return false;
                }

                string[] names = nameListValue.Split(PackageSettings.NamedPartitionSplitChar);
                for (int i = 0; i < names.Length; i++)
                {
                    d.PartitionNames.Add(names[i]);
                }

                psd = d;
                return true;
            }

            if (this.control.Package.Data.ContainsKey("PartitionUniform"))
            {
                UniformInt64RangePartitionSchemeDescription d = new UniformInt64RangePartitionSchemeDescription();

                long low;
                long high;
                int count;

                if (!this.TryGetData("PartitionUniformLowKey", out low)
                    || !this.TryGetData("PartitionUniformHighKey", out high)
                    || !this.TryGetData("PartitionUniformCount", out count))
                {
                    return false;
                }

                d.LowKey = low;
                d.HighKey = high;
                d.PartitionCount = count;
                psd = d;
                return true;
            }

            return false;
        }

        private bool TryCreateStatelessServiceDescription(out StatelessServiceDescription sd)
        {
            sd = null;

            try
            {
                sd = new StatelessServiceDescription();
                sd.ApplicationName = new Uri(this.control.Package.ApplicationAddress, UriKind.Absolute);
                sd.ServiceName = new Uri(this.control.Package.ServiceAddress, UriKind.Absolute);
                sd.ServiceTypeName = this.control.Package.ServiceType;

                int count;
                if (!this.TryGetData("ServiceStatelessCount", out count))
                {
                    return false;
                }

                sd.InstanceCount = count;
                // add special case for -1 when running in dev one-box, only start 1 instance
                if (sd.InstanceCount == -1)
                {
                    if (this.control.Cluster.ClusterType == ClusterType.OneBox)
                    {
                        log.Info("special case override of -1 for onebox environment");
                        if (Environment.UserInteractive)
                        {
                            Console.WriteLine("CreateService: special case override of instanceCount==-1 for onebox environment");
                        }
                        sd.InstanceCount = 1;
                    }
                }

                PartitionSchemeDescription psd;
                if (!this.TryCreatePartitionDescription(out psd))
                {
                    return false;
                }

                sd.PartitionSchemeDescription = psd;
                return true;
            }
            catch (Exception e)
            {
                this.Error("failed to create StatelessServiceDescription, exception={0}", e.Message);
                return false;
            }
        }

        private bool TryGetData(string name, out int value)
        {
            value = 0;
            string s;
            if (!this.control.Package.Data.TryGetValue(name, out s))
            {
                this.Error("could not find {0} in application package", name);
                return false;
            }

            if (!int.TryParse(s, out value))
            {
                this.Error("invalid {0} was found", name);
                return false;
            }

            return true;
        }

        private bool TryGetData(string name, out long value)
        {
            value = 0;
            string s;
            if (!this.control.Package.Data.TryGetValue(name, out s))
            {
                this.Error("could not find {0} in application package", name);
                return false;
            }

            if (!long.TryParse(s, out value))
            {
                this.Error("invalid {0} was found", name);
                return false;
            }

            return true;
        }

        private bool TryCreateStatefulServiceDescription(out StatefulServiceDescription sd)
        {
            sd = null;
            try
            {
                sd = new StatefulServiceDescription();
                sd.ApplicationName = new Uri(this.control.Package.ApplicationAddress);
                sd.ServiceName = new Uri(this.control.Package.ServiceAddress);
                sd.ServiceTypeName = this.control.Package.ServiceType;

                int minRep;
                if (!this.TryGetData("ServiceStatefulMinReplica", out minRep))
                {
                    return false;
                }

                sd.MinReplicaSetSize = minRep;

                int tgtSize;
                if (!this.TryGetData("ServiceStatefulTargetReplica", out tgtSize))
                {
                    return false;
                }

                sd.TargetReplicaSetSize = tgtSize;
                sd.HasPersistedState = false;
                string hasPersistedValue;
                if (this.control.Package.Data.TryGetValue("ServiceStatefulPersisted", out hasPersistedValue))
                {
                    if (hasPersistedValue == "true")
                    {
                        sd.HasPersistedState = true;
                    }
                }

                PartitionSchemeDescription psd;
                if (!this.TryCreatePartitionDescription(out psd))
                {
                    return false;
                }

                sd.PartitionSchemeDescription = psd;
                return true;
            }
            catch (Exception e)
            {
                this.Error("failed to create StatelessServiceDescription, exception={0}", e.Message);
                return false;
            }
        }

        private bool TryCreateServiceDescription(out ServiceDescription sd)
        {
            sd = null;
            bool result = false;

            if (this.Control.Package.Data.ContainsKey("ServiceStateless"))
            {
                StatelessServiceDescription ssd;
                result = this.TryCreateStatelessServiceDescription(out ssd);
                sd = ssd;
            }

            if (this.Control.Package.Data.ContainsKey("ServiceStateful"))
            {
                StatefulServiceDescription ssd;
                result = this.TryCreateStatefulServiceDescription(out ssd);
                sd = ssd;
            }

            return result;
        }

        private bool TryEnumNames(List<Uri> list, Uri uri, NameEnumerationResult result)
        {
            bool hasResult = false;
            try
            {
                this.fc.PropertyManager.EnumerateSubNamesAsync(uri, result, true).ContinueWith((t) =>
                    {
                        if (t.Exception != null)
                        {
                            if (t.Exception is AggregateException)
                            {
                                this.Error("EnumerateSubNames failed, exception: {0}", t.Exception.InnerException.Message);
                            }
                            else
                            {
                                this.Error("EnumerateSubNames failed, exception: {0}", t.Exception.Message);
                            }
                        }
                        else
                        {
                            list.AddRange(t.Result);
                            if (t.Result.HasMoreData)
                            {
                                this.TryEnumNames(list, uri, t.Result);
                            }

                            hasResult = true;
                        }
                    }).Wait();
            }
            catch (AggregateException ae)
            {
                this.Error("EnumerateSubNames failed, exception: {0}", ae.InnerException.Message);
            }
            catch (Exception e)
            {
                this.Error("EnumerateSubNames failed, exception: {0}", e.Message);
            }

            return hasResult;
        }

        private bool IsRetry(string function, Exception e)
        {
            // if anything other than timeout, fail now
            if (!(e is System.TimeoutException))
            {
                if (this.IsOneBoxStopped)
                {
                    this.Error("{0} failed: Host has stopped", function);
                    return false;
                }

                this.Error("{0} failed: {1}", function, e.Message);
                return false;
            }

            this.Info("{0} failed: {1}", function, e.Message);
            return true;
        }

        #region error and info messages

        private void Error(string s)
        {
            this.LastError = s;
            log.Error(s);
        }

        private void Error(string s, params object[] args)
        {
            this.LastError = string.Format(s, args);
            log.Error(s, args);
        }

        private void Info(string s)
        {
            this.LastInfo = s;
            log.Info(s);
        }

        private void Info(string s, params object[] args)
        {
            this.LastInfo = string.Format(s, args);
            log.Info(s, args);
        }

        #endregion
    }
}
