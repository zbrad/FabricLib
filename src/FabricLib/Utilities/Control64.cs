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

namespace ZBrad.FabricLib.Utilities
{
    /// <summary>
    /// Fabric Client control methods for 64-bit
    /// </summary>
    public class Control64 : IDisposable
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
        public Control64(ClusterSettings clusterSettings, Package packageSettings)
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
        public async Task ValidateClusterRunning()
        {
            this.LastException = null;

            if (this.IsOneBoxStopped)
                throw new ControlException("OneBox is stopped");

            if (fc == null)
                fc = getClient();

            await retryOp(async () =>
            {
                var result = await this.fc.PropertyManager.NameExistsAsync(
                    new Uri(Defaults.ApplicationNamespace + "/Any"),
                    Defaults.WaitDelay,
                    CancellationToken.None);
                return true;
            });
        }

        async Task retryOp(Func<Task> f)
        {
            var retryLimit = Defaults.WaitRetryLimit;
            while (retryLimit-- > 0)
            {
                try
                {
                    await f();
                    return;
                }
                catch (TimeoutException)
                {
                    // eat timeouts
                }

                await Task.Delay(Defaults.WaitDelay);
            }

            log.Error("retry failed too many times");
        }

        async Task<bool> retryOp(Func<Task<bool>> f)
        {
            var retryLimit = Defaults.WaitRetryLimit;
            while (retryLimit-- > 0)
            {
                try
                {
                    if (await f())
                        return true;
                }
                catch (TimeoutException)
                {
                    // eat timeouts
                }

                await Task.Delay(Defaults.WaitDelay);
            }

            log.Error("retry failed too many times");
            return false;
        }

        FabricClient getClient()
        {
            if (this.Control.Cluster.Data.ContainsKey("Thumbprint") && this.control.Cluster.Data.ContainsKey("CommonName"))
            {
                var xc = getCertificate();
                return new FabricClient(xc, this.Control.Cluster.Connection);
            }

            return new FabricClient(this.Control.Cluster.Connection);
        }

        X509Credentials getCertificate()
        {
            X509Credentials xc = new X509Credentials();

            string shortThumb = this.Control.Cluster.Data["Thumbprint"].Replace(" ", "").ToUpper();
            X509Store store = new X509Store(StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection cc = store.Certificates.Find(X509FindType.FindByThumbprint, shortThumb, true);
            if (cc == null || cc.Count == 0)
                throw new ControlException("certificate not found");

            X509Certificate2 cert = cc[0];
            xc.StoreLocation = store.Location;
            xc.FindType = X509FindType.FindByThumbprint;
            xc.FindValue = cert.Thumbprint;
            xc.RemoteCommonNames.Add(cert.FriendlyName);
            xc.ProtectionLevel = ProtectionLevel.EncryptAndSign;

            return xc;
        }

        bool IsOneBoxStopped
        {
            get
            {
                return this.Control.Cluster.ClusterType == ClusterType.OneBox && !this.control.IsHostRunning;
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
        public async Task Provision()
        {
            this.LastException = null;

            await ValidateClusterRunning();

            string imagePath = Defaults.ProvisionPrefix + "\\" + this.control.Package.ApplicationTypeName;
            log.Info("ProvisionApplication with path={0}", imagePath);

            await retryOp(async () =>
            {
                await this.fc.ApplicationManager
                  .ProvisionApplicationAsync(imagePath, Defaults.WaitDelay, CancellationToken.None);

                log.Info("Application provision successful");
            });
        }

        /// <summary>
        /// try to un-provision application
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task Unprovision()
        {
            this.LastException = null;

            await ValidateClusterRunning();
            ValidatePackage(control.Package);

            await retryOp(async () =>
            {
                await this.fc.ApplicationManager
                  .UnprovisionApplicationAsync(this.control.Package.ApplicationTypeName, this.control.Package.ApplicationVersion, Defaults.WaitDelay, CancellationToken.None);

                log.Info("Application unprovision successful");
            });
        }

        /// <summary>
        /// try to create application
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task ApplicationCreate()
        {
            this.LastException = null;

            await ValidateClusterRunning();
            ValidatePackage(control.Package);

            NameValueCollection nvc = GetApplicationParameters(this.control.Package);
            ApplicationDescription d = new ApplicationDescription(new Uri(this.control.Package.ApplicationAddress), this.control.Package.ApplicationTypeName, this.control.Package.ApplicationVersion, nvc);

            await retryOp(async () =>
            {
                await this.fc.ApplicationManager
                  .CreateApplicationAsync(d, Defaults.WaitDelay, CancellationToken.None);

                log.Info("Application create successful");
                return true;
            });
        }

        /// <summary>
        /// try to delete application
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task ApplicationDelete()
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            await retryOp(async () =>
            {
                await this.fc.ApplicationManager.DeleteApplicationAsync(
                        new Uri(this.control.Package.ApplicationAddress),
                        Defaults.WaitDelay,
                        CancellationToken.None);

                log.Info("Application delete successful");
            });
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
        public async Task<bool> TryUpgrade(Package fromPackage, Package toPackage, UpgradeOptions options)
        {
            this.LastException = null;
            try
            {
                var upgradeDescription = createUpgrade(fromPackage, toPackage);
                upgradeDescription.UpgradePolicyDescription = options.CreatePolicy();

                await this.fc.ApplicationManager.UpgradeApplicationAsync(upgradeDescription, Defaults.UpgradeTimeout, CancellationToken.None);
                return true;
            }
            catch (Exception e)
            {
                log.Error("UpgradeApplication failed, err={0}", e.Message);
                return false;
            }
        }

        ApplicationUpgradeDescription createUpgrade(Package fromPackage, Package toPackage)
        {
            var upgradeDescription = new ApplicationUpgradeDescription();

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

            return upgradeDescription;
        }

        /// <summary>
        /// try to create service
        /// </summary>
        public async Task ServiceCreate()
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            ServiceDescription sd;
            if (!this.TryCreateServiceDescription(out sd))
                throw new PackageException("cloud not create service description for package");

            await retryOp(async () =>
            {
                await this.fc.ServiceManager.CreateServiceAsync(sd, Defaults.WaitDelay, CancellationToken.None);
                log.Info("CreateService succeeded");
                return true;
            });
        }

        public static void ValidatePackage(Package package)
        {
            if (package.ApplicationAddress == null)
                throw new PackageException("ApplicationAddress is null");

            if (package.ServiceAddress == null)
                throw new PackageException("ServiceAddress is null");

            if (package.ServiceType == null)
                throw new PackageException("ServiceType is null");
        }

        /// <summary>
        /// try to delete service
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task ServiceDelete()
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            string address = this.control.Package.ServiceAddress;
            Uri serviceUri = new Uri(address);

            await retryOp(async () =>
                {
                    await this.fc.ServiceManager.DeleteServiceAsync(serviceUri, Defaults.WaitDelay, CancellationToken.None);
                    log.Info("DeleteService succeeded");
                });
        }

        /// <summary>
        /// try to fully create current package
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task PackageCreate()
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            await Provision();
            await ApplicationCreate();
            await ServiceCreate();

            log.Info("PackageCreate succeeded");
        }

        /// <summary>
        /// try to fully delete current package
        /// </summary>
        /// <returns>true if successful</returns>
        public async Task PackageDelete()
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            if (await ServiceExists())
            {
                await ServiceDelete();
                await ApplicationDelete();
                await Unprovision();
                return;
            }

            if (await ApplicationExists())
            {
                await ApplicationDelete();
                await Unprovision();
                return;
            }

            if (await ProvisionExists())
                await Unprovision();

            log.Info("PackageDelete succeeded");
        }

        async Task<bool> ServiceExists()
        {           
            var app = new Uri(control.Package.ApplicationAddress);
            var svc = new Uri(control.Package.ServiceAddress);
            var list = await fc.QueryManager.GetServiceListAsync(app, svc);
            return list != null && list.Count > 0;
        }

        async Task<bool> ApplicationExists()
        {
            var app = new Uri(control.Package.ApplicationAddress);
            var list = await fc.QueryManager.GetApplicationListAsync(app);
            return list != null && list.Count > 0;
        }

        async Task<bool> ProvisionExists()
        {
            var list = await fc.QueryManager.GetApplicationTypeListAsync(control.Package.ApplicationTypeName);
            if (list == null || list.Count == 0)
                return false;

            foreach (var p in list)
            {
                if (p.ApplicationTypeName == control.Package.ApplicationTypeName && p.ApplicationTypeVersion == control.Package.ApplicationVersion)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// get the status for the specified instance
        /// </summary>
        /// <param name="instance">the instance to use</param>
        /// <param name="status">the current status</param>
        /// <returns>true if retrieved</returns>
        public async Task<string> GetStatus(ApplicationInstance instance)
        {
            ValidatePackage(control.Package);
            await ValidateClusterRunning();

            var list = await this.fc.QueryManager.GetApplicationListAsync(instance.Name);
            if (list == null && list.Count == 0)
                return "instance not found";

            return list[0].ApplicationStatus.ToString();
        }

        /// <summary>
        /// try to get application instances
        /// </summary>
        /// <param name="instances">the instance list</param>
        /// <returns>true if successful</returns>
        public async Task<List<ApplicationInstance>> GetApplicationInstances()
        {
            await ValidateClusterRunning();

            var list = await this.fc.QueryManager.GetApplicationListAsync(null, Defaults.WaitDelay, CancellationToken.None);
            var instances = new List<ApplicationInstance>();
            foreach (Application a in list)
               instances.Add(new ApplicationInstance(a.ApplicationName, a.ApplicationTypeName, a.ApplicationTypeVersion, a.ApplicationStatus.ToString()));

            if (instances.Count == 0)
                return null;

            return instances;
        }

        static NameValueCollection GetApplicationParameters(Package package)
        {
            NameValueCollection nvc = new NameValueCollection();
            if (package.ApplicationParameters != null && !string.IsNullOrWhiteSpace(package.ApplicationParameters))
            {
                string[] values = package.ApplicationParameters.Split(Package.ApplicationParameterSplitChar);
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

        bool TryCreatePartitionDescription(out PartitionSchemeDescription psd)
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
                    log.Error("named partition specified, but no named list was found");
                    return false;
                }

                string[] names = nameListValue.Split(Package.NamedPartitionSplitChar);
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

        bool TryCreateStatelessServiceDescription(out StatelessServiceDescription sd)
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
                            log.Warn("CreateService: special case override of instanceCount==-1 for onebox environment");
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
                log.Error("failed to create StatelessServiceDescription, exception={0}", e.Message);
                return false;
            }
        }

        bool TryGetData(string name, out int value)
        {
            value = 0;
            string s;
            if (!this.control.Package.Data.TryGetValue(name, out s))
            {
                log.Error("could not find {0} in application package", name);
                return false;
            }

            if (!int.TryParse(s, out value))
            {
                log.Error("invalid {0} was found", name);
                return false;
            }

            return true;
        }

        bool TryGetData(string name, out long value)
        {
            value = 0;
            string s;
            if (!this.control.Package.Data.TryGetValue(name, out s))
            {
                log.Error("could not find {0} in application package", name);
                return false;
            }

            if (!long.TryParse(s, out value))
            {
                log.Error("invalid {0} was found", name);
                return false;
            }

            return true;
        }

        bool TryCreateStatefulServiceDescription(out StatefulServiceDescription sd)
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
                log.Error("failed to create StatelessServiceDescription, exception={0}", e.Message);
                return false;
            }
        }

        bool TryCreateServiceDescription(out ServiceDescription sd)
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (fc != null)
                        fc.Dispose();
                    fc = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
