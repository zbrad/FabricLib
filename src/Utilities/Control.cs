using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.ServiceProcess;

namespace ZBrad.FabLibs.Utilities
{
    /// <summary>
    /// WindowsFabric control methods and settings
    /// </summary>
    public class Control
    {
        #region private data

        internal const string WindowsFabricRegKey = @"SOFTWARE\Microsoft\Windows Fabric";
        internal const string WindowsFabricHostSvcKey = @"SYSTEM\CurrentControlSet\Services\FabricHostSvc";
        internal const string VsWebProjectKey = @"Software\Microsoft\VisualStudio\12.0\WebProjects";

        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        static bool isEnvValid = false;

        static string[] cleanDirs = { "\\ImageStore", "\\Node1\\Fabric\\work", "\\Node2\\Fabric\\work", "\\Node3\\Fabric\\work", "\\Node4\\Fabric\\work", "\\Node5\\Fabric\\work" };

        static string logmanPath = Environment.ExpandEnvironmentVariables("%SystemRoot%\\system32\\logman.exe");
        //// static string powershellPath = Environment.ExpandEnvironmentVariables("%SystemRoot%\\system32\\WindowsPowerShell\\v1.0\\powershell.exe");

        static string fabricHostPath = null;

        // TODO: replace with psh
        static string fabricDeployerPath = null;
        static string imageClientPath = null;

        static string tracePat = @"(?<Collector>Fabric\w+)\s+(?<Type>\w+)\s+(?<Status>\w+)";
        static Regex traceRegex = new Regex(tracePat, RegexOptions.Compiled);

        private PackageSettings currentPackage = new PackageSettings();
        private ClusterSettings currentCluster = new ClusterSettings();
        private List<string> stoppedTraces;

        #endregion

        static Control()
        {
            try
            {
                using (RegistryKey hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (RegistryKey key = hklm64.OpenSubKey(WindowsFabricRegKey))
                    {
                        FabricDataRoot = ((string)key.GetValue("FabricDataRoot", string.Empty)).TrimEnd('\\'); // remove any trailing backslash
                        FabricBinRoot = ((string)key.GetValue("FabricBinRoot", string.Empty)).TrimEnd('\\'); // remove any trailing backslash
                        FabricCodePath = ((string)key.GetValue("FabricCodePath", string.Empty)).TrimEnd('\\'); // remove any trailing backslash
                    }
                }
                
                fabricHostPath = Path.Combine(FabricBinRoot, "FabricHost.exe");
                fabricDeployerPath = Path.Combine(FabricCodePath, "FabricDeployer.exe");
                imageClientPath = Path.Combine(FabricCodePath, "ImageStoreClient.exe");

                isEnvValid = true;
            }
            catch (Exception e)
            {
                log.Error(e, "Get Variables Failed");
            }
        }

        #region public static properties

        /// <summary>
        /// gets the WindowsFabric data path
        /// </summary>
        public static string FabricDataRoot { get; private set; }

        /// <summary>
        /// gets the WindowsFabric binary path
        /// </summary>
        public static string FabricBinRoot { get; private set; }

        /// <summary>
        /// gets the WindowsFabric code path
        /// </summary>
        public static string FabricCodePath { get; private set; }

        #endregion

        #region public properties

        /// <summary>
        /// gets or sets current package settings
        /// </summary>
        public PackageSettings Package { get { return this.currentPackage; } set { this.currentPackage = value; } }

        /// <summary>
        /// gets or sets current cluster settings
        /// </summary>
        public ClusterSettings Cluster
        {
            get { return this.currentCluster; }
            set { this.currentCluster = value; }
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
        /// gets status of host running
        /// </summary>
        public bool IsHostRunning
        {
            get
            {
                Process p;
                if (this.IsHostConsoleRunning(out p))
                {
                    this.Cluster.HostType = HostType.Console;
                    return true;
                }

                if (isRunningAsService())
                {
                    this.Cluster.HostType = HostType.Service;
                    return true;
                }

                return false;
            }
        }

        #endregion

        #region public static methods

        /// <summary>
        /// tests if environment is ok
        /// </summary>
        /// <returns>true if valid environment</returns>
        public static bool IsEnvironmentOk()
        {
            return isEnvValid;
        }

        #endregion

        #region public methods

        /// <summary>
        /// test if environment has been initialized
        /// </summary>
        /// <returns></returns>
        public bool IsInitializedForDevelopment()
        {
            if (!isFabricHostSvcManual())
            {
                this.Info("FabricHostSvc is not set to manual");
                return false;
            }

            if (!isUseX64Web())
            {
                this.Info("IISExpress not configured for x64 development");
                return false;
            }

            return true;
        }

        /// <summary>
        /// sets tracing status
        /// </summary>
        /// <param name="isLogging">status of tracing</param>
        public void SetLogging(bool isLogging)
        {
            if (isLogging)
                NLog.LogManager.EnableLogging();
            else
                NLog.LogManager.DisableLogging();
        }

        public bool TryClusterStart()
        {
            if (!IsEnvironmentOk())
            {
                this.Error("environment check failed, development cluster not initialized");
                return false;
            }

            bool wasCleaned = false;
            ServiceController sc;
            if (!this.IsHostServiceRunning(out sc))
            {
                // if running as a service, shut it down (permanently)
                if (!TryServiceStop(sc))
                    return false;

                // make sure we clean up from service run
                this.TryClusterRemove();
                this.TryLogsClean();
                wasCleaned = true;
            }

            // set service start to manual (if not already set)
            // HKLM:\SYSTEM\CurrentControlSet\Services\FabricHostSvc
            object value = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FabricHostSvc", "Start", -1);
            if (value != null && value is int && ((int)value) != 3)
            {
                Microsoft.Win32.Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\FabricHostSvc", "Start", 3);

                if (!wasCleaned)
                {
                    // if possibly previously running as a service, make sure it's fully cleaned up
                    this.TryClusterRemove();
                    this.TryLogsClean();
                }
            }

            return TryHostConsoleStart();            
        }

        /// <summary>
        /// stop currently running cluster, stop/flush any traces
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterStop()
        {
            if (!IsEnvironmentOk())
            {
                this.Error("environment check failed");
                return false;
            }

            if (!this.IsHostRunning)
            {
                this.Error("host is not running");
                return false;
            }

            switch (this.Cluster.HostType)
            {
                case HostType.Console:
                    return this.TryHostConsoleStop();
                case HostType.Service:
                    return this.TryHostServiceStop();
                default:
                    this.Error("unhandled host type {0}", this.Cluster.HostType);
                    return false;
            }
        }

        /// <summary>
        /// try to stop traces
        /// </summary>
        /// <param name="stoppedTraces">the stopped traces</param>
        /// <returns>true if successful</returns>
        public bool TryTracesStop(out List<string> stoppedTraces)
        {
            stoppedTraces = null;

            if (!IsEnvironmentOk())
            {
                return false;
            } 

            if (this.IsHostRunning)
            {
                this.Info("cannot stop traces while host is running");
                return false;
            }

            this.Info("stopping traces");

            List<LogmanTrace> list = this.GetTraces();
            if (list == null || list.Count == 0)
            {
                this.Info("no logman traces found");
                return true;
            }

            stoppedTraces = new List<string>();
            foreach (LogmanTrace lt in list)
            {
                if (lt.Status == "Running")
                {
                    string response;
                    int rc = this.ExecuteProgram(logmanPath, "stop " + lt.DataCollectorSet, out response);
                    if (rc != 0)
                    {
                        this.Info("Could not stop trace " + lt.DataCollectorSet + "\nlogman response:\n" + response);
                        return false;
                    }

                    stoppedTraces.Add(lt.DataCollectorSet);
                }
            }

            this.Info("traces stopped");
            return true;
        }

        /// <summary>
        /// try to stop fabric host service
        /// </summary>
        /// <returns>true if not running or successfully stopped</returns>
        public bool TryHostServiceStop()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            ServiceController sc;
            if (!this.IsHostServiceRunning(out sc))
            {
                this.Error("service is not running");
                return false;
            }

            if (!TryServiceStop(sc))
                return false;

            return true;
        }

        private bool TryServiceStop(ServiceController sc)
        {
            try
            {
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    sc.Stop();
                }

                int limit = Defaults.WaitRetryLimit;
                while (isRunningAsService() && limit >= 0)
                {
                    Task.Delay(Defaults.WaitDelay);
                    this.Info("waiting for service to stop");
                }

                if (limit < 0)
                {
                    this.Error("failed to stop windows fabric service");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                this.Info("service stop failed, error=" + e.Message);
                return false;
            }
        }

        /// <summary>
        /// clean the cluster completely
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterClean()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            } 
            
            if (this.IsHostRunning)
            {
                this.Error("Cannot clean cluster while running");
                return false;
            }

            bool wasSuccessful = true;
            IEnumerator<string> dirs = SafeEnumerator.GetDirectoryEnumerator(Control.FabricDataRoot, "work");
            while (dirs.MoveNext())
            {
                try
                {
                    Directory.Delete(dirs.Current, true);
                }
                catch (Exception e)
                {
                    wasSuccessful = false;
                    this.Error("delete work folder failed: {0}", e.Message);
                }
            }

            if (Directory.Exists(ClusterSettings.DevImageStoreFolder))
            {
                try
                {
                    Directory.Delete(ClusterSettings.DevImageStoreFolder, true);
                    Directory.CreateDirectory(ClusterSettings.DevImageStoreFolder);
                }
                catch (Exception e)
                {
                    wasSuccessful = false;
                    this.Error("delete image store folder failed: {0}", e.InnerException != null ? e.InnerException.Message : e.Message);
                }
            }
            else
            {
                Directory.CreateDirectory(ClusterSettings.DevImageStoreFolder);
            }

            IEnumerator<string> logFiles = SafeEnumerator.GetFileEnumerator(FabricDataRoot, "ReplicatorShared.log");
            while (logFiles.MoveNext())
            {
                try
                {
                    File.Delete(logFiles.Current);
                }
                catch (Exception e)
                {
                    wasSuccessful = false;
                    this.Error("delete replication log failed: {0}", e.Message);
                }
            }

            if (!wasSuccessful)
            {
                this.Info("failed to clean cluster");
                return false;
            }

            this.Info("Cluster clean successful");
            return true;
        }

        /// <summary>
        /// try to delete the cluster
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterDelete()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (!this.TryClusterStop())
            {
                return false;
            }

            this.TryClusterRemove();
            this.TryLogsClean();

            return true;
        }

        /// <summary>
        /// Initialize this environment for Windows Fabric Development
        /// 
        /// Actions are:
        ///   - set WinFabSvc to manual start
        ///   - set Visual Studio IISExpress to default to x64 (for vs2013)
        /// </summary>
        public bool TryInitializeForDevelopment()
        {
            if (IsInitializedForDevelopment())
                return true;

            if (!IsEnvironmentOk())
            {
                Error("Windows Fabric not installed");
                return false;
            }

            if (this.IsHostRunning)
            {
                if (!TryClusterStop())
                    return false;
            }

            if (!this.TryClusterRemove())
                return false;

            if (!this.TryLogsClean())
                return false;

            if (!trySetFabricHostSvcManual())
            {
                Error("could not set fabric host service to manual start");
                return false;
            }

            if (!trySetUseX64Web())
            {
                Error("could not set iisexpress default to x64");
                return false;
            }

            return IsInitializedForDevelopment();
        }

        static bool isFabricHostSvcManual()
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                using (RegistryKey key = hklm.OpenSubKey(WindowsFabricHostSvcKey))
                {
                    if (key == null)
                        return false;

                    // get service start options
                    object o = key.GetValue("Start");
                    if (o != null && o is int)
                    {
                        ServiceStartMode startValue = (ServiceStartMode)o;
                        return startValue == ServiceStartMode.Manual;
                    }

                    return false;
                }
            }
        }

        static bool trySetFabricHostSvcManual()
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default))
            {
                using (RegistryKey key = hklm.OpenSubKey(WindowsFabricHostSvcKey, true))
                {
                    if (key == null)
                        return false;

                    // set service to manual start
                    key.SetValue("Start", ((int) ServiceStartMode.Manual));
                    return true;
                }
            }
        }

        static bool trySetUseX64Web()
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (RegistryKey key = hklm.OpenSubKey(VsWebProjectKey, true))
                {
                    if (key == null)
                        return false;

                    // set iisexpress default to x64
                    key.SetValue("Use64BitIISExpress", 1);
                    return true;
                }
            }
        }

        static bool isUseX64Web()
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default))
            {
                using (RegistryKey key = hklm.OpenSubKey(VsWebProjectKey, true))
                {
                    if (key == null)
                        return false;

                    // get iisexpress default  HKEY_CURRENT_USER\Software\Microsoft\VisualStudio\12.0\WebProjects  Use64BitIISExpress
                    object o = key.GetValue("Use64BitIISExpress");
                    if (o == null)
                        return false;

                    int value = (int)o;
                    bool result = value == 1;
                    return result;
                }
            }
        }

        static bool isRunningAsService()
        {
            ServiceController sc = GetFabricService();
            if (sc == null)
                return false;

            return sc.Status == ServiceControllerStatus.Running;
        }

        /// <summary>
        /// gets service host info
        /// </summary>
        /// <param name="sc">the service controller</param>
        /// <returns>true if running</returns>
        public bool IsHostServiceRunning(out ServiceController sc)
        {
            sc = null;

            if (!IsEnvironmentOk())
            {
                return false;
            } 
            
            sc = GetFabricService();
            if (sc == null)
            {
                return false;
            }

            if (sc.Status == ServiceControllerStatus.Running)
            {
                this.currentCluster.HostType = HostType.Service;
                return true;
            }

            return false;
        }

        /// <summary>
        /// removes tickets
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryTicketsClean()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (this.IsHostRunning)
            {
                return false;
            }

            bool wasDeleted = false;
            string[] exts = { "*.tkt", "*.sni" };
            foreach (string ext in exts)
            {
                IEnumerable<string> files = Directory.EnumerateFiles(FabricDataRoot, ext, SearchOption.AllDirectories);
                foreach (string s in files)
                {
                    try
                    {
                        File.Delete(s);
                        wasDeleted = true;
                    }
                    catch (Exception e)
                    {
                        this.Error("failed to delete ticket {0}, exception: {1}", s, e.Message);
                        return false;
                    }
                }
            }

            if (wasDeleted)
            {
                this.Info("Deleted tickets successfully");
            }
            else
            {
                this.Info("%No tickets found to delete");
            }

            return true;
        }

        /// <summary>
        /// try to remove from image store
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryImageStoreRemove()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (this.Package.ApplicationTypeName == null)
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(" Delete -c \"");
            sb.Append(this.Cluster.ImageStoreConnection);
            sb.Append("\" -x \"");
            sb.Append(this.Package.ImageStoreRelativePath);
            sb.Append("\"");
            string args = sb.ToString();

            string response;
            int rc = this.ExecuteProgram(imageClientPath, args, out response);

            if (rc != 0)
            {
                this.Error("ImageStoreClient failed, response:\n{0}", response);
                return false;
            }

            this.Info("ImageStoreClient successfully removed application");
            return true;
        }

        /// <summary>
        /// try to upload to image store
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryImageStoreUpload()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            } 
            
            StringBuilder sb = new StringBuilder();

#if POWERSHELL
            sb.Append(" -Command Copy-WindowsFabricApplicationPackage -ImageStoreConnectionString \"");
            sb.Append(this.Cluster.ImageStoreConnection);
            sb.Append("\" -RelativePath \"");
            sb.Append(this.Package.ImageStoreRelativePath);
            sb.Append("\" -ApplicationPackagePath \"");
            sb.Append(this.Package.PackagePath.TrimEnd('\\'));
            sb.Append("\" -Force");
#endif

            sb.Append(" Upload -c \"");
            sb.Append(this.Cluster.ImageStoreConnection);
            sb.Append("\" -x \"");
            sb.Append(this.Package.ImageStoreRelativePath);
            sb.Append("\" -l \"");
            sb.Append(this.Package.PackagePath.TrimEnd('\\'));
            sb.Append("\" -g AtomicCopy");

            string args = sb.ToString();

            // TODO: replace with powershell command
            string response;
            int rc = this.ExecuteProgram(imageClientPath, args, out response);

            if (rc != 0)
            {
                this.Error("ImageStoreClient failed, response:\n{0}", response);
                return false;
            }

            this.Info("ImageStoreClient upload successful");
            return true;
        }

        /// <summary>
        /// test if cluster has been deployed
        /// </summary>
        /// <returns>true if cluster has been deployed</returns>
        public bool IsClusterDeployed()
        {
            if (File.Exists(FabricDataRoot + "\\FabricHostSettings.xml"))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// try to create dev cluster
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterCreate()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (!this.TryClusterRemove())
            {
                return false;
            }

            if (this.Cluster.FilePath == null)
            {
                if (!this.TryClusterFileCreate())
                {
                    this.Error("could not find or create default cluster file");
                    return false;
                }
            }

            if (!TestClusterManifest())
                return false;

            return this.TryClusterDeploy();
        }

        private bool TryClusterDeploy()
        {
            FileInfo fi = new FileInfo(this.Cluster.FilePath);
            if (!fi.Exists)
            {
                this.Error("Could not find cluster manifest at: " + this.Cluster.FilePath);
                return false;
            }

            StringBuilder sb = new StringBuilder("/operation:");
            string operation;
            if (this.Cluster.Data.TryGetValue("DeploymentType", out operation))
            {
                this.Info("deployment operation specified: {0}", operation);
            }
            else
            {
                operation = "CREATE";
                this.Info("defaulting deployment operation to: CREATE");
            }

            sb.Append(operation);
            sb.Append(" \"/cm:");
            sb.Append(fi.FullName);
            sb.Append('"');

            string args = sb.ToString();

            string response;
            int rc = this.ExecuteProgram(fabricDeployerPath, args, out response);

            if (rc != 0)
            {
                this.Error("FabricDeployer failed, response:\n{0}", response);
                return false;
            }

            this.Info("Cluster create successful");
            return true;
        }

        /// <summary>
        /// try to remove cluster
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterRemove()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (this.IsHostRunning)
            {
                this.Info("Cannot remove cluster with running host");
                return false;
            }

            string args = "/operation:Remove";
            string response;
            int rc = this.ExecuteProgram(fabricDeployerPath, args, out response);

            if (rc != 0)
            {
                this.Error("FabricDeployer failed, response:\n{0}", response);
                return false;
            }

            if (File.Exists(FabricDataRoot + @"\FabricHostSettings.xml"))
                File.Delete(FabricDataRoot + @"\FabricHostSettings.xml");

            this.Info("Cluster remove successful");
            return true;
        }

        /// <summary>
        /// try to clean logs
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryLogsClean()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            } 
            
                if (this.IsHostRunning)
                {
                    this.Error("Cannot clean logs while cluster running");
                    return false;
                }

                if (!this.TryTracesStop(out stoppedTraces))
                {
                    this.Error("Unable to stop traces");
                    return false;
                }
    
            string logFolder = FabricDataRoot + @"\log";
            if (Directory.Exists(logFolder))
            {
                try
                {
                    Directory.Delete(logFolder, true);
                }
                catch (Exception e)
                {
                    this.Error("failed to clean trace logs, exception: {0}", e.Message);
                    return false;
                }

                if (Directory.Exists(logFolder))
                {
                    this.Error("unable to remove log folder");
                    return false;
                }
            }

            string diagFolder = FabricDataRoot + @"\DiagnosticsStore";
            if (Directory.Exists(diagFolder))
            {
                try
                {
                    Directory.Delete(diagFolder, true);
                }
                catch (Exception e)
                {
                    this.Error("failed to clean diagnostics logs, exception: {0}", e.Message);
                    return false;
                }
            }

            this.Info("Logs removed successfully");
            return true;
        }

        /// <summary>
        /// start the specified traces
        /// </summary>
        /// <param name="tracesToStart">list of traces</param>
        /// <returns>true if successful</returns>
        public bool TryTracesStart(List<string> tracesToStart)
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            this.Info("starting traces");

            if (tracesToStart == null || tracesToStart.Count == 0)
            {
                this.Info("No traces specified to start");
                return true;
            }

            foreach (string traceName in tracesToStart)
            {
                string response;
                int rc = this.ExecuteProgram(logmanPath, "start " + traceName, out response);
                if (rc != 0)
                {
                    this.Error("Could not start trace {0} logman response: {1}", traceName, response);
                    return false;
                }
            }

            this.Info("traces started");
            return true;
        }

        /// <summary>
        /// try to start traces
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryTracesStart()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            List<LogmanTrace> list = this.GetTraces();
            if (list == null || list.Count == 0)
            {
                this.Info("%No logman traces found");
                return true;
            }

            List<string> tracesToStart = new List<string>();
            foreach (LogmanTrace lt in list)
            {
                if (lt.Status == "Stopped")
                {
                    tracesToStart.Add(lt.DataCollectorSet);
                }
            }

            return this.TryTracesStart(tracesToStart);
        }

        /// <summary>
        /// gets host console process
        /// </summary>
        /// <param name="p">the <see cref="Process"/> instance</param>
        /// <returns>true if running</returns>
        public bool IsHostConsoleRunning(out Process p)
        {
            p = null;

            if (!IsEnvironmentOk())
            {
                return false;
            }

            // filter out condition of service FabricHost is running
            if (isRunningAsService())
                return false;

            try
            {
                Process[] list = null;
                try 
                {
                    list = Process.GetProcessesByName("FabricHost");
                }
                catch
                {
                    return false;
                }

                if (list == null || list.Length == 0)
                {
                    return false;
                }

                p = list[0];
                this.currentPackage.Data[PackageSettings.Pid] = p.Id.ToString(CultureInfo.InvariantCulture);
                this.currentPackage.Data[PackageSettings.ProcessName] = p.ProcessName;
                return true;
            }
            catch 
            {
                return false;
            }
        }

        /// <summary>
        /// try to start service host
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryHostServiceStart()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            if (this.IsHostRunning)
            {
                this.Error("Host already running");
                return false;
            }

            // if no process or service running, then first delete tickets (for faster startup)
            if (!this.TryTicketsClean())
            {
                this.Error("Ticket clean failed");
                return false;
            }

            this.Cluster.HostType = HostType.Service;
            ServiceController sc = GetFabricService();

            try
            {
                sc.Start();
            }
            catch (Exception e)
            {
                this.LastException = e;

                if (e.InnerException != null)
                {
                    this.Error("Start failed, exception: {0}", e.InnerException.Message);
                }
                else
                {
                    this.Error("Start failed, exception: {0}", e.Message);
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// try to start console host
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryHostConsoleStart()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            try
            {
                if (this.IsHostRunning)
                {
                    this.Error("Host already running");
                    return false;
                }

                // if no process or service running, then first delete tickets (for faster startup)
                if (!this.TryTicketsClean())
                {
                    this.Error("Ticket clean failed");
                    return false;
                }

                this.Cluster.HostType = HostType.Console;

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = fabricHostPath;
                psi.Arguments = "-c -activateHidden"; // console

                this.Info("tryStartHost cmd=" + psi.FileName + " " + psi.Arguments);
                Process p = Process.Start(psi);
                this.currentPackage.Data[PackageSettings.ProcessName] = p.ProcessName;
                this.currentPackage.Data[PackageSettings.Pid] = p.Id.ToString(CultureInfo.InvariantCulture);

                Task.Factory.StartNew(() =>
                    {
                        p.WaitForExit();
                        string temp;
                        this.currentPackage.Data.TryRemove(PackageSettings.ProcessName, out temp);
                        this.currentPackage.Data.TryRemove(PackageSettings.Pid, out temp);
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// try to stop fabric host console process
        /// </summary>
        /// <returns>true if host was not running or was successfully stopped</returns>
        public bool TryHostConsoleStop()
        {
            if (!IsEnvironmentOk())
            {
                return false;
            }

            Process p;
            if (!this.IsHostConsoleRunning(out p))
            {
                return true;
            }

            try
            {
                p.Kill();
                int retry = Defaults.WaitRetryLimit;
                while (!p.HasExited)
                {
                    if (retry <= 0)
                    {
                        return false;
                    }

                    Task.Delay(Defaults.WaitDelay).Wait();
                    retry--;
                }

                return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// try to create cluster file
        /// </summary>
        /// <returns>true if successful</returns>
        public bool TryClusterFileCreate()
        {
            if (File.Exists(this.Cluster.FilePath))
            {
                return true;
            }

            this.Info("No default cluster found, creating default cluster file");

            // see if we know the file?
            Assembly assembly = Assembly.GetExecutingAssembly();
            if (this.Cluster.ClusterPath == null)
            {
                this.Cluster.ClusterPath = Path.GetDirectoryName(assembly.Location);
            }

            if (this.Cluster.ClusterFile == null)
            {
                this.Cluster.ClusterFile = ClusterSettings.DefaultClusterName;
            }
            else
            {
                if (this.Cluster.ClusterFile != ClusterSettings.DefaultClusterName)
                {
                    this.Error("Unknown cluster file '{0}' cannot be created", this.Cluster.ClusterFile);
                    return false;
                }
            }

            // uncomment this code if you need to find out what the embedded assembly resource name is
            // string[] names = assembly.GetManifestResourceNames();

            try
            {
                Stream stream = assembly.GetManifestResourceStream(ClusterSettings.DefaultClusterResourceFile);
                if (stream == null)
                {
                    return false;
                }

                // else get it out of our resources
                using (StreamReader sr = new StreamReader(stream))
                using (StreamWriter sw = new StreamWriter(this.Cluster.FilePath))
                {
                    sw.Write(sr.ReadToEnd());
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region static methods
        static ServiceController GetFabricService()
        {
            return new ServiceController("FabricHostSvc");
        }

        /// <summary>
        /// executes the provided program
        /// </summary>
        /// <param name="program">program path</param>
        /// <param name="arg">program arguments</param>
        /// <param name="response">any console responses</param>
        /// <returns>exit code</returns>
        private int ExecuteProgram(string program, string arg, out string response)
        {
            string args = string.Empty;
            response = string.Empty;
            if (arg != null)
            {
                args = arg;
            }

            program = GetNative(program);
            if (program == null)
                return -1;

            this.Info("ExecuteProgram: calling '{0}' with '{1}'", program, args);

            string progInfo = null;
            string progError = null;
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = program;
            psi.Arguments = args;
            psi.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process p = new Process();
            p.StartInfo = psi;
            p.Start();
            Task<string> asyncOut = p.StandardOutput.ReadToEndAsync();
            Task<string> asyncErr = p.StandardError.ReadToEndAsync();
            p.WaitForExit();

            progInfo = asyncOut.Result;
            if (!string.IsNullOrWhiteSpace(progInfo))
            {
                this.Info(progInfo);
            }

            progError = asyncErr.Result;
            if (!string.IsNullOrWhiteSpace(progError))
            {
                this.Error(progError);
            }

            response = progInfo + progError;
            return p.ExitCode;
        }

        static string windir = System.Environment.GetEnvironmentVariable("WINDIR");
        private string GetNative(string program)
        {
            FileInfo fi = new FileInfo(program);
            if (fi.Exists)
                return fi.FullName;

            string path = windir + @"\sysnative" + program;
            fi = new FileInfo(path);
            if (fi.Exists)
                return fi.FullName;
            
            path = windir + @"\system32" + program;
            fi = new FileInfo(path);
            if (fi.Exists)
                return fi.FullName;

            this.Error("could not find program: {0}", program);
            return null;
        }
        private List<LogmanTrace> GetTraces()
        {
            string response;
            int exitCode = this.ExecuteProgram(logmanPath, string.Empty, out response);
            if (exitCode != 0)
            {
                return null;
            }

            List<LogmanTrace> list = new List<LogmanTrace>();
            using (StringReader sr = new StringReader(response))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    Match m = traceRegex.Match(line);
                    if (m.Success)
                    {
                        LogmanTrace lt = new LogmanTrace();
                        lt.DataCollectorSet = m.Groups["Collector"].Value;
                        lt.Type = m.Groups["Type"].Value;
                        lt.Status = m.Groups["Status"].Value;
                        list.Add(lt);
                    }
                }
            }

            return list;
        }

        #endregion

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

        private const string PowershellExe = @"\WindowsPowerShell\v1.0\Powershell.exe";
        private const string CmdExe = @"\cmd.exe";

        public bool TestClusterManifest()
        {
            if (!this.IsClusterDeployed())
            {
                if (!this.TryClusterFileCreate())
                    return false;
            }

            // test is bad, in that if it returns false, then the reason shows up in console out
            string response;
            int rc = ExecuteProgram(PowershellExe, "Test-WindowsFabricClusterManifest '" + this.Cluster.FilePath + "'", out response);
            if (rc != 0)
                return false;

            string[] results = response.Split('\r', '\n');
            if (results.Length > 0 && results[0].StartsWith("True"))
                return true;

            if (results.Length > 2 && results[2].StartsWith("False"))
                this.Error(results[0]); // reason is in first line
            else
                this.Error("unexpected response from Test-WindowsFabricClusterManifest");

            return false;
        }


        #region private classes

        private class LogmanTrace
        {
            /// <summary>
            /// data collector set
            /// </summary>
            public string DataCollectorSet { get; set; }

            /// <summary>
            /// trace type
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// trace status
            /// </summary>
            public string Status { get; set; }
        }

        #endregion
    }
}
