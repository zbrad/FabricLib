using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace ZBrad.FabLibs.Utilities.Schema
{
    public static class Load
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static ClusterManifestType ClusterManifest(string path)
        {
            return load<ClusterManifestType>(path);
        }

        public static ApplicationManifestType ApplicationManifest(string path)
        {
            return load<ApplicationManifestType>(path);
        }

        public static ServiceManifestType ServiceManifest(string path)
        {
            return load<ServiceManifestType>(path);
        }


        static T load<T>(string path) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));
            try
            {
                using (var sr = new StreamReader(path))
                {
                    var manifest = (T)serializer.Deserialize(sr);
                    return manifest;
                }
            }
            catch (Exception e)
            {
                log.Error("Invalid manifest file fomat, exception: {0}", e.Message);
                return null;
            }
        }
    }
}
