using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using ZBrad.FabLibs.Utilities.Schema;

namespace ZBrad.FabLibs.Utilities
{
    /// <summary>
    /// cluster types
    /// </summary>
    public enum ClusterType
    {
        /// <summary>
        /// one box development cluster
        /// </summary>
        OneBox,

        /// <summary>
        /// server cluster
        /// </summary>
        Server,

        /// <summary>
        /// Azure cluster
        /// </summary>
        Azure
    }

    /// <summary>
    /// host types
    /// </summary>
    public enum HostType
    {
        /// <summary>
        /// console host
        /// </summary>
        Console,

        /// <summary>
        /// service host
        /// </summary>
        Service
    }

    /// <summary>
    /// maintains cluster settings
    /// </summary>
    public class ClusterSettings
    {
        internal const string DefaultClusterName = "DevEnv-FiveNodes.xml";
        internal const string DefaultClusterResourceFile = "ZBrad.FabLibs.Utilities.Resources." + DefaultClusterName;
        private const string ClusterPathDef = "ClusterPath";
        private const string ClusterFileDef = "ClusterFile";
        private const string ClusterHost = "ClusterHost";
        private const string ClusterPort = "ClusterPort";
        private const string XStorePrefix = "xstore:";

        static string fabricDataRoot = null;
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        static string azureXStorePattern = @"((?<name>\w+)=(?<value>[^;]+))+";
        static Regex azureXStoreRegex = new Regex(azureXStorePattern, RegexOptions.Compiled);
        static string devImageStoreConnection = null;
        static string devImageStoreFolder = null;
        static string argPat = @"(?<cmd>\w+)(=(?<value>[^""]+))?";
        static Regex argRegex = new Regex(argPat);

        private Dictionary<string, string> data = new Dictionary<string, string>();

        static ClusterSettings()
        {
            try
            {
                using (RegistryKey hklm64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (RegistryKey key = hklm64.OpenSubKey(Control.WindowsFabricRegKey))
                    {
                        fabricDataRoot = ((string)key.GetValue("FabricDataRoot", string.Empty)).TrimEnd('\\'); // remove any trailing backslash
                        devImageStoreFolder = ClusterSettings.fabricDataRoot + "\\ImageStore";
                        devImageStoreConnection = "file:" + devImageStoreFolder;
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e, "Get Variables Failed: {0}");
            }
        }

        /// <summary>
        /// gets the folder used for image store when running in developer cluster
        /// </summary>
        public static string DevImageStoreFolder { get { return devImageStoreFolder; } }

        /// <summary>
        /// gets or sets cluster path
        /// </summary>
        public string ClusterPath
        {
            get
            {
                string v;
                if (!this.data.TryGetValue(ClusterPathDef, out v))
                {
                    return null;
                }

                return v;
            }

            set
            {
                this.data[ClusterPathDef] = value;
            }
        }

        /// <summary>
        /// gets or set cluster file
        /// </summary>
        public string ClusterFile
        {
            get
            {
                string v;
                if (!this.data.TryGetValue(ClusterFileDef, out v))
                {
                    return null;
                }

                return v;
            }

            set
            {
                this.data[ClusterFileDef] = value;
            }
        }

        /// <summary>
        /// gets or sets connection host
        /// </summary>
        public string ConnectionHost
        {
            get
            {
                string v;
                if (this.data.TryGetValue(ClusterHost, out v))
                {
                    return v;
                }

                return Defaults.ConnectionHost;
            }

            set
            {
                this.data[ClusterHost] = value;
            }
        }

        /// <summary>
        /// gets or sets connection port
        /// </summary>
        public string ConnectionPort
        {
            get
            {
                string v;
                if (this.data.TryGetValue(ClusterPort, out v))
                {
                    return v;
                }

                return Defaults.ConnectionPort;
            }

            set
            {
                this.data[ClusterPort] = value;
            }
        }

        /// <summary>
        /// gets cluster connection string
        /// </summary>
        public string Connection
        {
            get
            {
                if (string.IsNullOrEmpty(this.ConnectionHost) || string.IsNullOrEmpty(this.ConnectionPort))
                {
                    return string.Empty;
                }

                return this.ConnectionHost + ":" + this.ConnectionPort;
            }
        }

        /// <summary>
        /// gets cluster full file path
        /// </summary>
        public string FilePath
        {
            get
            {
                string p = this.ClusterPath;
                string f = this.ClusterFile;
                if (p == null || f == null)
                {
                    return null;
                }

                return Path.Combine(p, f);
            }
        }

        /// <summary>
        /// gets or sets cluster type
        /// </summary>
        public ClusterType ClusterType
        {
            get
            {
                string type;
                if (!this.data.TryGetValue("ClusterType", out type))
                {
                    // default to onebox if not specified
                    this.ClusterType = ClusterType.OneBox;
                    return ClusterType.OneBox;
                }

                switch (type)
                {
                    case "Azure":
                        return ClusterType.Azure;
                    case "OneBox":
                        return ClusterType.OneBox;
                    case "Server":
                        return ClusterType.Server;
                    default:
                        // always default to onebox
                        this.data["ClusterType"] = "OneBox";
                        return ClusterType.OneBox;
                }
            }

            set
            {
                switch (value)
                {
                    case ClusterType.Azure:
                        this.data["ClusterType"] = "Azure";
                        break;
                    case ClusterType.OneBox:
                        this.data["ClusterType"] = "OneBox";
                        break;
                    case ClusterType.Server:
                        this.data["ClusterType"] = "Server";
                        break;
                    default:
                        throw new ApplicationException("unknown cluster type");
                }
            }
        }

        /// <summary>
        /// gets or sets image store connection
        /// </summary>
        public string ImageStoreConnection
        {
            get
            {
                switch (this.ClusterType)
                {
                    case ClusterType.OneBox:
                        return devImageStoreConnection;
                    case ClusterType.Azure:
                        return this.GetXStoreConnection();
                    case ClusterType.Server:
                        return this.GetServerConnection();
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// gets or sets host type
        /// </summary>
        public HostType HostType 
        {
            get
            {
                string type;
                if (!this.data.TryGetValue("HostType", out type))
                {
                    // default to console
                    this.HostType = HostType.Console;
                    return HostType.Console;
                }

                switch (type)
                {
                    case "Service":
                        return HostType.Service;
                    case "Console":
                        return HostType.Console;
                    default:
                        this.data["HostType"] = "Console";
                        return HostType.Console;
                }
            }
            
            set
            {
                switch (value)
                {
                    case HostType.Service:
                        this.data["HostType"] = "Service";
                        break;
                    case HostType.Console:
                        this.data["HostType"] = "Console";
                        break;
                    default:
                        throw new ApplicationException("unhandled host type");
                }
            }
        }

        /// <summary>
        /// gets or sets cluster data
        /// </summary>
        public Dictionary<string, string> Data { get { return this.data; } set { this.data = value; } }

        /// <summary>
        /// try to create a new <see cref="PackageSettings"/>
        /// </summary>
        /// <param name="args">arguments to use</param>
        /// <param name="clusterSettings">the new package settings</param>
        /// <returns>true if successful</returns>
        public static bool TryCreate(string[] args, out ClusterSettings clusterSettings)
        {
            clusterSettings = null;

            if (args == null)
            {
                return false;
            }

            clusterSettings = new ClusterSettings();
            clusterSettings.InitDefs(args);

            return true;
        }

        /// <summary>
        /// checks for valid cluster manifest
        /// </summary>
        /// <returns>true if valid cluster manifest</returns>
        public bool IsValidForControl()
        {
            if (!this.TryLoadClusterManifest())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// sets image store value
        /// </summary>
        /// <param name="value">value to set</param>
        public void SetServerImageStore(string value)
        {
            this.data["ImageStoreServer"] = value;
        }

        #region private methods

        static void SetImageStoreParams(Dictionary<string, string> d, ClusterManifestType manifest)
        {
            var settings = new List<SettingsOverridesTypeSection>(manifest.FabricSettings);
            var management = settings.Find((s) => s.Name == "Management");
            var parameters = new List<SettingsOverridesTypeSectionParameter>(management.Parameter);
            var store = parameters.Find((s) => s.Name == "ImageStoreConnectionString");
            var connectionString = store.Value;

            if (connectionString == "_default_" || connectionString == "fabric:ImageStore")
                return;

            MatchCollection mc = azureXStoreRegex.Matches(connectionString);
            if (mc.Count == 0)
                return;

            foreach (Match m in mc)
            {
                d["ImageStore" + m.Groups["name"].Value] = m.Groups["value"].Value;
            }
        }

        private bool TryLoadClusterManifest()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                return false;
            }

            FileInfo fi;

            try
            {
                fi = new FileInfo(this.FilePath);
            }
            catch (Exception e)
            {
                log.Error("Get file info failed: {0}", e.Message);
                return false;
            }

            if (!fi.Exists)
            {
                log.Error("File not found: {0}", this.FilePath);
                return false;
            }

            var manifest = Load.ClusterManifest(fi.FullName);
            if (manifest == null)
                return false;

            try
            {
                SetImageStoreParams(this.data, manifest);
                return true;
            }
            catch (Exception e)
            {
                log.Error(e, "unable to load cluster data");
                return false;
            }
        }

        /*
         * Azure connection:
         *  @"xstore:DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};Container={2}"
         */
        private string GetXStoreConnection()
        {
            try
            {
                StringBuilder sb = new StringBuilder(XStorePrefix);
                sb.Append("DefaultEndpointsProtocol=");
                sb.Append(this.data["ImageStoreDefaultEndpointsProtocol"]);
                sb.Append(";AccountName=");
                sb.Append(this.data["ImageStoreAccountName"]);
                sb.Append(";AccountKey=");
                sb.Append(this.data["ImageStoreAccountKey"]);
                sb.Append(";Container=");
                sb.Append(this.data["ImageStoreContainer"]);
                return sb.ToString();
            }
            catch
            {
                log.Error("could not create xstore connection");
                return string.Empty;
            }
        }

        private string GetServerConnection()
        {
            string store;
            if (this.data.TryGetValue("ImageStoreServer", out store))
            {
                return store;
            }

            return string.Empty;
        }

        private void InitDefs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Match m = argRegex.Match(args[i]);
                if (m.Success)
                {
                    // let's always enforce lowercase for easier matching of command line arguments
                    string cmd = m.Groups["cmd"].Value;
                    if (m.Groups["value"].Success)
                    {
                        string value = m.Groups["value"].Value;

                        // let's trim ANY dangling '\'s
                        if (value[value.Length - 1] == '\\')
                        {
                            value = value.Substring(0, value.Length - 1);
                        }

                        this.data[cmd] = value;
                    }
                    else
                    {
                        this.data[cmd] = "true";
                    }
                }
            }
        }

        #endregion
    }
}