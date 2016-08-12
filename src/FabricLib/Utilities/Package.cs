using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZBrad.FabricLib.Utilities.Schema;

namespace ZBrad.FabricLib.Utilities
{
    /// <summary>
    /// service partition types
    /// </summary>
    public enum ServicePartitionType
    {
        /// <summary>
        /// no partition type selected
        /// </summary>
        None,

        /// <summary>
        /// singleton type
        /// </summary>
        Singleton,

        /// <summary>
        /// named type
        /// </summary>
        Named,

        /// <summary>
        /// uniform type
        /// </summary>
        Uniform
    }

    /// <summary>
    /// class contains the package settings
    /// </summary>
    public class Package
    {
        /// <summary>
        /// the split character used to split and join application parameter settings
        /// </summary>
        public const char ApplicationParameterSplitChar = ';';

        /// <summary>
        /// the split character used for Named partitions
        /// </summary>
        public const char NamedPartitionSplitChar = ',';

        internal const string InputPath = "Path";
        internal const string ProcessName = "ProcessName";
        internal const string Pid = "Pid";

        internal const string AppNamespace = "ApplicationNamespace";
        internal const string AppName = "ApplicationName";
        internal const string AppVersion = "ApplicationVersion";
        internal const string AppAddress = "ApplicationAddress";

        internal const string SvcName = "ServiceName";
        internal const string SvcType = "ServiceType";
        internal const string SvcAddress = "ServiceAddress";

        private const string AppSettings = "applicationSettings";
        private const string PropertySettingsSuffix = ".Properties.Settings";

        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // required properties
        static string[] minConfigProps = { AppNamespace, AppName, AppVersion, AppAddress, SvcName, SvcType, SvcAddress };
        static string[] minXmlProps = { AppName, AppVersion, SvcName, SvcType };

        static string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        static string argPat = @"(?<cmd>\w+)(=(?<value>[^""]+))?";
        static Regex argRegex = new Regex(argPat);
        static string varPat = @"\$\((?<var>\w+)\)";
        static Regex varReg = new Regex(varPat, RegexOptions.Compiled);

        private ConcurrentDictionary<string, string> data = new ConcurrentDictionary<string, string>();

        private string imageStoreRelativePath = null;
        private ServicePartitionType servicePartitionType = ServicePartitionType.Singleton; // default to singleton

        /// <summary>
        /// gets or sets image store path
        /// </summary>
        public string ImageStoreRelativePath
        {
            get
            {
                if (this.imageStoreRelativePath == null)
                {
                    this.imageStoreRelativePath = "incoming\\" + this.ApplicationTypeName;
                }

                return this.imageStoreRelativePath;
            }

            set
            {
                this.imageStoreRelativePath = value;
            }
        }

        /// <summary>
        /// gets or sets partition type
        /// </summary>
        public ServicePartitionType ServicePartitionType
        {
            get
            {
                if (this.data.ContainsKey("PartitionSingleton"))
                {
                    this.servicePartitionType = ServicePartitionType.Singleton;
                }
                else if (this.data.ContainsKey("PartitionNamed"))
                {
                    this.servicePartitionType = ServicePartitionType.Named;
                }
                else if (this.data.ContainsKey("PartitionUniform"))
                {
                    this.servicePartitionType = ServicePartitionType.Uniform;
                }
                else
                {
                    this.servicePartitionType = ServicePartitionType.None;
                }

                return this.servicePartitionType;
            }

            set
            {
                try
                {
                    string temp;

                    // first cleanup from current type
                    switch (this.servicePartitionType)
                    {
                        case ServicePartitionType.Singleton:
                            this.data.TryRemove("PartitionSingleton", out temp);
                            break;
                        case ServicePartitionType.Named:
                            this.data.TryRemove("PartitionNamed", out temp);
                            this.data.TryRemove("PartitionNamedList", out temp);
                            break;
                        case ServicePartitionType.Uniform:
                            this.data.TryRemove("PartitionUniform", out temp);
                            this.data.TryRemove("PartitionUniformLowKey", out temp);
                            this.data.TryRemove("PartitionUniformHighKey", out temp);
                            this.data.TryRemove("PartitionUniformCount", out temp);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    log.Info("ignoring removal errors, exception={0}", e.Message);
                }

                // now set new type
                switch (value)
                {
                    case ServicePartitionType.Singleton:
                        this.data["PartitionSingleton"] = "true";
                        break;
                    case ServicePartitionType.Named:
                        this.data["PartitionNamed"] = "true";
                        break;
                    case ServicePartitionType.Uniform:
                        this.data["PartitionUniform"] = "true";
                        break;
                }

                this.servicePartitionType = value;
            }
        }

        /// <summary>
        /// gets or sets application type name
        /// </summary>
        public string ApplicationTypeName
        {
            get { return this.Expand(Defaults.ApplicationName); }
            set { this.data[Defaults.ApplicationName] = value; }
        }

        /// <summary>
        /// gets or sets application namespace
        /// </summary>
        public string ApplicationNamespace
        {
            get { return this.Expand(Defaults.ApplicationNamespace); }
            set { this.data[Defaults.ApplicationNamespace] = value; }
        }

        /// <summary>
        /// gets or sets application instance address
        /// </summary>
        public string ApplicationAddress
        {
            get { return this.Expand(Defaults.ApplicationAddress); }
            set { this.data[Defaults.ApplicationAddress] = value; }
        }

        /// <summary>
        /// gets or sets application version
        /// </summary>
        public string ApplicationVersion
        {
            get { return this.Expand(Defaults.ApplicationVersion); }
            set { this.data[Defaults.ApplicationVersion] = value; }
        }

        /// <summary>
        /// gets or sets application parameters (comma separated)
        /// </summary>
        public string ApplicationParameters
        {
            get { return this.Expand(Defaults.ApplicationParameters); }
            set { this.data[Defaults.ApplicationParameters] = value; } // don't expand user values
        }

        /// <summary>
        /// gets or sets service name
        /// </summary>
        public string ServiceName
        {
            get { return this.Expand(Defaults.ServiceName); }
            set { this.data[Defaults.ServiceName] = value; }
        }

        /// <summary>
        /// gets or sets service type
        /// </summary>
        public string ServiceType
        {
            get { return this.Expand(Defaults.ServiceType); }
            set { this.data[Defaults.ServiceType] = value; }
        }

        /// <summary>
        /// gets or sets service instance address
        /// </summary>
        public string ServiceAddress
        {
            get { return this.Expand(Defaults.ServiceAddress); }
            set { this.data[Defaults.ServiceAddress] = value; }
        }

        /// <summary>
        /// gets or sets replica endpoint name
        /// </summary>
        public string ReplicaEndpointName
        {
            get { return this.Expand(Defaults.ReplicaEndpointName); }
            set { this.data[Defaults.ReplicaEndpointName] = value; }
        }

        /// <summary>
        /// gets or sets service endpoint name
        /// </summary>
        public string ServiceEndpointName
        {
            get { return this.Expand(Defaults.ServiceEndpointName); }
            set { this.data[Defaults.ServiceEndpointName] = value; }
        }

        /// <summary>
        /// gets or sets service exe
        /// </summary>
        public string ServiceExeHost
        {
            get { return this.Expand(Defaults.ServiceExeHost); }
            set { this.data[Defaults.ServiceExeHost] = value; }
        }

        /// <summary>
        /// gets or sets service code folder
        /// </summary>
        public string ServiceCodeFolder
        {
            get { return this.Expand(Defaults.ServiceCodeFolder); }
            set { this.data[Defaults.ServiceCodeFolder] = value; }
        }

        /// <summary>
        /// gets or sets package path
        /// </summary>
        public string PackagePath
        {
            get { return this.Data[InputPath]; }
            set { this.data[InputPath] = value; }
        }

        /// <summary>
        /// gets the package folder
        /// </summary>
        public string PackageFolder
        {
            get { return (new DirectoryInfo(this.PackagePath)).Name; }
        }

        /// <summary>
        /// gets or sets this package's data
        /// </summary>
        public ConcurrentDictionary<string, string> Data
        {
            get { return this.data; }
            set { this.data = value; }
        }

        /// <summary>
        /// try to create a new <see cref="Package"/>
        /// </summary>
        /// <param name="appPath">path to load application manifest</param>
        /// <param name="packageSettings">the new package settings</param>
        /// <returns>true if successful</returns>
        public static bool TryCreate(string appPath, out Package packageSettings)
        {
            packageSettings = null;

            if (appPath == null)
            {
                return false;
            }

            packageSettings = new Package();
            packageSettings.data[InputPath] = appPath;

            return packageSettings.IsValidForCreate();
        }

        /// <summary>
        /// try to create a new <see cref="Package"/>
        /// </summary>
        /// <param name="args">arguments to use</param>
        /// <param name="packageSettings">the new package settings</param>
        /// <returns>true if successful</returns>
        public static bool TryCreate(string[] args, out Package packageSettings)
        {
            packageSettings = null;

            if (args == null)
            {
                return false;
            }

            packageSettings = new Package();
            packageSettings.InitDefs(args);

            return true;
        }

        /// <summary>
        /// determines if we have enough information to create an application and service
        /// </summary>
        /// <returns>true if valid</returns>
        public bool IsValidForCreate()
        {
            if (!this.TryLoadValues())
            {
                log.Error("could not load all required values");
                return false;
            }

            string folderList;
            string exeList;

            if (!this.Data.TryGetValue(Defaults.ServiceCodeFolder, out folderList))
            {
                log.Error("could not find ServiceCodeFolder list");
                return false;
            }

            if (!this.Data.TryGetValue(Defaults.ServiceExeHost, out exeList))
            {
                log.Error("could not find ServiceExeHost list");
                return false;
            }

            string[] folders = folderList.Split(',');
            string[] exes = exeList.Split(',');

            if (folders.Length != exes.Length)
            {
                log.Error("ServiceCodeFolder does not match ServiceExeHost count");
                return false;
            }

            for (int i = 0; i < folders.Length && i < exes.Length; i++)
            {
                string codeFolder = Path.Combine(this.Data["ServiceManifestDir"], folders[i]);
                if (!Directory.Exists(codeFolder))
                {
                    log.Error("could not find code folder at {0}", codeFolder);
                    return false;
                }

                string codeExe = Path.Combine(codeFolder, exes[i]);
                if (!File.Exists(codeExe))
                {
                    log.Error("could not find service host exe");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// trace property settings
        /// </summary>
        public void TraceProps()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Using application and service with these properties:");
            for (int i = 0; i < minConfigProps.Length; i++)
            {
                sb.Append('\t');
                sb.Append(minConfigProps[i]);
                sb.Append('=');
                sb.AppendLine(this.data[minConfigProps[i]]);
            }

            log.Info(sb.ToString());
        }

        static bool HasRequiredValues(Dictionary<string, string> d, string[] requiredKeys)
        {
            for (int i = 0; i < requiredKeys.Length; i++)
            {
                if (!d.ContainsKey(requiredKeys[i]))
                {
                    log.Error("required definition '{0}' is missing", requiredKeys[i]);
                    return false;
                }
            }

            return true;
        }

        static bool HasRequiredValues(ConcurrentDictionary<string, string> d, string[] requiredKeys)
        {
            for (int i = 0; i < requiredKeys.Length; i++)
            {
                if (!d.ContainsKey(requiredKeys[i]))
                {
                    log.Error("required definition '{0}' is missing", requiredKeys[i]);
                    return false;
                }
            }

            return true;
        }

        static void UpdateDest(Dictionary<string, string> dest, Dictionary<string, string> source)
        {
            if (dest == null || source == null)
            {
                return;
            }

            // now copy into dest, where there is not already an entry (allowing command line to override)
            foreach (string k in source.Keys)
            {
                if (!dest.ContainsKey(k))
                {
                    dest[k] = source[k];
                }
            }
        }

        static bool IsValidated(Dictionary<string, string> xml, ConcurrentDictionary<string, string> table)
        {
            foreach (string s in table.Keys)
            {
                string v;

                // if we have it in xml, then validate it
                if (xml.TryGetValue(s, out v))
                {
                    if (!string.Equals(v, table[s]))
                    {
                        log.Error("Xml value '{0}' does not match config value '{1}' for property '{2}'", v, table[s], s);
                        return false;
                    }
                }
            }

            return true;
        }

        static string GetAppNamespace(ConcurrentDictionary<string, string> tbl1, Dictionary<string, string> tbl2)
        {
            string appNs;
            if (!tbl2.TryGetValue(AppNamespace, out appNs) && !tbl1.TryGetValue(AppNamespace, out appNs))
            {
                appNs = Defaults.ApplicationNamespace;
            }

            return appNs;
        }

        static string GetAppAddress(ConcurrentDictionary<string, string> tbl1, Dictionary<string, string> tbl2)
        {
            string appNs = GetAppNamespace(tbl1, tbl2);
            string appname;
            if (!tbl2.TryGetValue(AppName, out appname) && !tbl1.TryGetValue(AppName, out appname))
            {
                return null;
            }

            return appNs + "/" + appname;
        }

        static string GetSvcAddress(ConcurrentDictionary<string, string> tbl1, Dictionary<string, string> tbl2)
        {
            string appAddress = GetAppAddress(tbl1, tbl2);
            if (appAddress == null)
            {
                return null;
            }

            string svcname;
            if (!tbl2.TryGetValue(SvcName, out svcname) && !tbl1.TryGetValue(SvcName, out svcname))
            {
                return null;
            }

            return appAddress + "/" + svcname;
        }

        static string GetServiceTypeName(object o)
        {
            string svcManSvcType = null;
            if (o is StatefulServiceTypeType)
            {
                StatefulServiceTypeType sstt = (StatefulServiceTypeType)o;
                svcManSvcType = sstt.ServiceTypeName;
            }
            else if (o is StatelessServiceTypeType)
            {
                StatelessServiceTypeType sstt = (StatelessServiceTypeType)o;
                svcManSvcType = sstt.ServiceTypeName;
            }
            else if (o is StatefulServiceGroupTypeType)
            {
                StatefulServiceGroupTypeType ssgtt = (StatefulServiceGroupTypeType)o;
                svcManSvcType = ssgtt.ServiceGroupTypeName;
            }
            else if (o is StatelessServiceGroupTypeType)
            {
                StatelessServiceGroupTypeType ssgtt = (StatelessServiceGroupTypeType)o;
                svcManSvcType = ssgtt.ServiceGroupTypeName;
            }

            return svcManSvcType;
        }

        // default formula for AppAddress is:
        // $(ApplicationNamespace)/$(ApplicationName)
        // default formula for SvcAddress is:
        // $(ApplicationAddress)/$(ServiceName)
        private void FillMissing(ConcurrentDictionary<string, string> dest, Dictionary<string, string> source)
        {
            foreach (string s in source.Keys)
            {
                if (!dest.ContainsKey(s))
                {
                    dest[s] = source[s];
                }
            }

            for (int i = 0; i < minConfigProps.Length; i++)
            {
                string k = minConfigProps[i];

                if (dest.ContainsKey(k))
                {
                    continue;
                }

                // else we may need fill, so far we know how to do 2
                switch (k)
                {
                    case Package.AppNamespace:
                        string appNs = GetAppNamespace(dest, source);
                        if (appNs != null)
                        {
                            dest[Package.AppNamespace] = appNs;
                        }

                        break;
                    case Package.AppAddress:
                        string appAddress = GetAppAddress(dest, source);
                        if (appAddress != null)
                        {
                            dest[Package.AppAddress] = appAddress;
                        }

                        break;
                    case Package.SvcAddress:
                        string svcAddress = GetSvcAddress(dest, source);
                        if (svcAddress != null)
                        {
                            dest[Package.SvcAddress] = svcAddress;
                        }

                        break;
                    default:
                        log.Error("unhandled missing property '{0}'", k);
                        break;
                }
            }
        }

        private void InitDefs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                log.Info("Arg[" + i + "]=" + args[i]);
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

        private void UpdatePartitionInfo(Dictionary<string, string> xml, ServiceType sst)
        {
            if (sst.UniformInt64Partition != null && sst.UniformInt64Partition.LowKey != null && sst.UniformInt64Partition.HighKey != null && sst.UniformInt64Partition.PartitionCount != null)
            {
                xml["PartitionUniform"] = "true";
                xml["PartitionUniformLowKey"] = sst.UniformInt64Partition.LowKey;
                xml["PartitionUniformHighKey"] = sst.UniformInt64Partition.HighKey;
                xml["PartitionUniformCount"] = sst.UniformInt64Partition.PartitionCount;
                return;
            }

            if (sst.NamedPartition != null && sst.NamedPartition.Partition != null && sst.NamedPartition.Partition.Count > 0)
            {
                xml["PartitionNamed"] = "true";
                StringBuilder sb = new StringBuilder();
                bool once = false;
                foreach (ServiceTypeNamedPartitionPartition pp in sst.NamedPartition.Partition)
                {
                    if (once)
                    {
                        sb.Append(Package.NamedPartitionSplitChar);
                    }

                    sb.Append(pp.Name);
                    once = true;
                }

                xml["PartitionNamedList"] = sb.ToString();
                return;
            }

            // default to singleton
            xml["PartitionSingleton"] = "true";
        }

        private bool TryLoadXml(string pkgDir, out Dictionary<string, string> xml)
        {
            xml = new Dictionary<string, string>();
            xml["ApplicationManifestDir"] = pkgDir;

            // first load ApplicationManifest
            string appFile = new FileInfo(Path.Combine(pkgDir, "ApplicationManifest.xml")).FullName;
            xml["ApplicationManifestFile"] = appFile;
            if (!File.Exists(appFile))
            {
                log.Error("Cannot find ApplicationManifest at '" + appFile + "'");
                return false;
            }

            var appMan = Load.ApplicationManifest(appFile);
            if (appMan == null)
            {
                return false;
            }

            xml[AppName] = appMan.ApplicationTypeName;
            xml[AppVersion] = appMan.ApplicationTypeVersion;

            if (appMan.Parameters != null && appMan.Parameters.Count > 0)
            {
                bool paramOnce = false;
                StringBuilder sb = new StringBuilder();
                foreach (var p in appMan.Parameters)
                {
                    if (paramOnce)
                    {
                        sb.Append(Package.ApplicationParameterSplitChar);
                    }

                    sb.Append(p.Name);
                    sb.Append('=');
                    sb.Append(p.DefaultValue);

                    paramOnce = true;
                }

                xml[Defaults.ApplicationParameters] = sb.ToString();
            }

            ServiceType svcTempl0 = null;
            // get servicetemplate info
            if (appMan.ServiceTemplates != null && appMan.ServiceTemplates.Count > 0)
            {
                svcTempl0 = appMan.ServiceTemplates[0];
                if (svcTempl0 is StatefulServiceType)
                {
                    StatefulServiceType sst = (StatefulServiceType)svcTempl0;
                    xml["ServiceStateful"] = "true";
                    xml["ServiceType"] = sst.ServiceTypeName;
                    xml["ServiceStatefulMinReplica"] = sst.MinReplicaSetSize.ToString();
                    xml["ServiceStatefulTargetReplica"] = sst.TargetReplicaSetSize.ToString();

                    this.UpdatePartitionInfo(xml, sst);
                }
                else if (svcTempl0 is StatelessServiceType)
                {
                    StatelessServiceType sst = (StatelessServiceType)svcTempl0;
                    xml["ServiceStateless"] = "true";
                    xml["ServiceType"] = sst.ServiceTypeName;
                    xml["ServiceStatelessCount"] = sst.InstanceCount.ToString();
                    this.UpdatePartitionInfo(xml, sst);
                }
            }

            // next load ServiceManifest
            if (appMan.ServiceManifestImport.Count != 1)
            {
                log.Error("Can only verify 1 service manifest");
                return false;
            }

            ServiceManifestRefType manRef = appMan.ServiceManifestImport[0].ServiceManifestRef;
            xml["ServiceManifestName"] = manRef.ServiceManifestName;

            string svcDir = Path.Combine(pkgDir, manRef.ServiceManifestName);
            xml["ServiceManifestDir"] = svcDir;
            if (!Directory.Exists(svcDir))
            {
                log.Error("Could not find service manifest folder at '{0}'", svcDir);
                return false;
            }

            string svcFile = Path.Combine(svcDir, "ServiceManifest.xml");
            xml["ServiceManifestFile"] = svcFile;

            if (!File.Exists(svcFile))
            {
                log.Error("Cannot find ServiceManifest at '{0}'", svcFile);
                return false;
            }

            var svcMan = Load.ServiceManifest(svcFile);
            if (svcMan == null)
            {
                log.Error("Service manifest was not able to be loaded");
                return false;
            }

            if (!manRef.ServiceManifestName.Equals(svcMan.Name))
            {
                log.Error("Service names don't match, ApplicationManifest ref is '{0}' but ServiceManifest is '{1}'", manRef.ServiceManifestName, svcMan.Name);
                return false;
            }

            xml[SvcName] = svcMan.Name;

            /*
            if (svcMan.ServiceTypes.Count != 1)
            {
                log.Error("Only expected 1 service type in manifest, found={0}", svcMan.ServiceTypes.Count);
                return false;
            }
            */

            object o = svcMan.ServiceTypes[0];
            if (o is StatefulServiceTypeType)
            {
                StatefulServiceTypeType sstt = (StatefulServiceTypeType)o;
                if (sstt.HasPersistedState)
                {
                    xml["ServiceStatefulPersisted"] = "true";
                }
            }

            string svcManSvcType = GetServiceTypeName(o);

            if (svcTempl0 != null && !svcManSvcType.Equals(svcTempl0.ServiceTypeName))
            {
                log.Error("Service type names don't match, Application ServiceTemplate name is '{0}' but ServiceManifest SerciveTypeName is '{1}'", svcTempl0.ServiceTypeName, svcManSvcType);
                return false;
            }

            xml["ServiceVersion"] = svcMan.Version;

            StringBuilder exeSb = new StringBuilder();
            StringBuilder folderSb = new StringBuilder();
            bool once = false;
            foreach (CodePackageType cpt in svcMan.CodePackage)
            {
                EntryPointDescriptionType entDesc = cpt.EntryPoint;
                EntryPointDescriptionTypeExeHost exeHost = entDesc.Item as EntryPointDescriptionTypeExeHost;
                if (exeHost == null)
                {
                    log.Error("Could not find ExeHost description");
                    return false;
                }

                if (once)
                {
                    exeSb.Append(',');
                    folderSb.Append(',');
                }

                folderSb.Append(cpt.Name);
                exeSb.Append(exeHost.Program);

                once = true;
            }

            xml[Defaults.ServiceCodeFolder] = folderSb.ToString();
            xml[Defaults.ServiceExeHost] = exeSb.ToString();
            return true;
        }

        private bool TryLoadValues()
        {
            // require path for create
            if (!this.data.ContainsKey(InputPath))
            {
                log.Error("required parameter -Path not specified");
                return false;
            }

            // first load ApplicationManifest and ServiceManifest
            Dictionary<string, string> xml;
            if (!this.TryLoadXml(this.data[InputPath], out xml))
            {
                return false;
            }

            if (!HasRequiredValues(xml, minXmlProps))
            {
                log.Error("unable to fill in minimum properties from ApplicationManifest and ServiceManifest");
                return false;
            }

            // ok we have some args, do the ones we have match the xml values
            if (!IsValidated(xml, this.data))
            {
                log.Error("Your xml and args don't match");
                return false;
            }

            // if not all values are provided on command line, let's see if we can supply them from xml
            this.FillMissing(this.data, xml);

            if (!HasRequiredValues(this.data, minConfigProps))
            {
                log.Error("missing some properties, and cannot determine default from xml");
                return false;
            }

            return true;
        }

        private string Expand(string defName)
        {
            string value;
            if (!this.data.TryGetValue(defName, out value))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder(value);
            Match m;
            while ((m = varReg.Match(sb.ToString())).Success)
            {
                string name = m.Groups["var"].Value;
                sb.Remove(m.Index, m.Length);   // first we remove the prop reference
                if (this.data.TryGetValue(name, out value))
                {
                    sb.Insert(m.Index, value);  // insert a new value if we find one
                }
            }

            return sb.ToString();
        }
    }
}
