using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using Q = System.Fabric.Query;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Reflection;
using NLog.Layouts;
using NLog.Config;

namespace ZBrad.FabricLib.Utilities
{
    /// <summary>
    /// utility methods
    /// </summary>
    public static class Utility
    {
        public static readonly string Node = FabricRuntime.GetNodeContext().IPAddressOrFQDN;
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// enumerate all fabric names
        /// </summary>
        /// <param name="client">fabric client to use</param>
        /// <param name="subName">base namespace</param>
        /// <returns>list of Uri names</returns>
        public static List<Uri> EnumerateAndPrintSubNames(FabricClient client, Uri subName)
        {
            List<Uri> result = new List<Uri>();

            log.Info("Enumerating all fabric names which matches {0}:", subName);

            try
            {
                NameEnumerationResult nameResult = null;
                do
                {
                    Task<NameEnumerationResult> enumTask = client.PropertyManager.EnumerateSubNamesAsync(subName, nameResult, true);
                    enumTask.Wait();
                    nameResult = enumTask.Result;

                    // Each batch has two flags: HasMore and Consistent. 
                    // Consistent indicates whether the relevant naming data in the cluster is currently being updated 
                    // HasMore indicates whether there are othe batches that remain.
                    // If there are other batches, the user needs to recall the EnumerateSubNamesAsync and give the latest batch as the previousResult.
                    // PreviousResult makes sure that the subsequent batch is returned.                    
                    foreach (Uri name in nameResult)
                    {
                        log.Info("\t{0}", name);
                        result.Add(name);
                    }
                }
                while (nameResult.HasMoreData);
            }
            catch (AggregateException e)
            {
                if (e.InnerException is TimeoutException)
                {
                    log.Error(e, "EnumerateSubNamesAsync timed out");
                }
                else if (e.InnerException is FabricElementNotFoundException)
                {
                    // FabricElementNotFoundException indicates that the name in the argument did not exist in the Naming name tree.
                    log.Info("Name '{0}' does not exist", subName);
                }
                else
                {
                    log.Error(e, "Unexpected exception");
                }
            }
            catch (ArgumentException e)
            {
                // One of the common reasons for ArgumentException is that the specified URI is not a valid Windows Fabric name.
                log.Error(e, "Not a valid argument");
            }
            catch (Exception e)
            {
                log.Error(e, "Unexpected exception");
            }

            if (result.Count == 0)
            {
                log.Info("\t{0} does not have any child names.", subName);
            }

            return result;
        }

        public static UriBuilder GetDefaultPath(StatefulService service)
        {
            var uri = GetDefaultServiceUri(service.ServiceInitializationParameters);
            var id = service.ServiceInitializationParameters.ReplicaId;
            var part = GetPartitionDescription(service.ServicePartition.PartitionInfo);

            uri.Path += "/" + part + "/" + id + "/";
            return uri;
        }

        public static string GetPartitionDescription(ServicePartitionInformation info)
        {
            switch (info.Kind)
            {
                case ServicePartitionKind.Int64Range:
                    var p = info as Int64RangePartitionInformation;
                    return "R" + p.LowKey + "-" + p.HighKey;
                case ServicePartitionKind.Named:
                    var n = info as NamedPartitionInformation;
                    return "N-" + n.Name;
                case ServicePartitionKind.Singleton:
                    return "S";
                default:
                    throw new ApplicationException("unknown parition kind");
            }
        }

        public static UriBuilder GetDefaultPartitionUri(StatelessService service)
        {
            var uri = GetDefaultServiceUri(service.ServiceInitializationParameters);
            var id = service.ServiceInitializationParameters.InstanceId;
            var part = GetPartitionDescription(service.ServicePartition.PartitionInfo);

            uri.Path += "/" + part + "/" + id + "/";
            return uri;
        }


        /// <summary>
        /// Create correct address for either stateless or stateful
        /// </summary>
        /// <param name="initParams"></param>
        /// <param name="endpointName"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool TryCreateAddress(ServiceInitializationParameters initParams, string endpointName, out string address)
        {
            if (initParams is StatelessServiceInitializationParameters)
                return TryCreateAddress((StatelessServiceInitializationParameters)initParams, endpointName, out address);

            return TryCreateAddress((StatefulServiceInitializationParameters)initParams, endpointName, out address);
        }

        /// <summary>
        /// gets uri names
        /// </summary>
        /// <param name="fc">fabric client</param>
        /// <param name="baseName">base namespace</param>
        /// <returns>list of uri names</returns>
        public static List<Uri> GetNames(FabricClient fc, string baseName)
        {
            Uri u;
            if (!Uri.TryCreate(baseName, UriKind.Absolute, out u))
            {
                return null;
            }

            return GetNames(fc, u);
        }

        /// <summary>
        /// get URI names
        /// </summary>
        /// <param name="fc">fabric client</param>
        /// <param name="baseUri">base uri</param>
        /// <returns>list of uri names</returns>
        public static List<Uri> GetNames(FabricClient fc, Uri baseUri)
        {
            List<Uri> names = new List<Uri>();
            int limit = Defaults.WaitRetryLimit;
            NameEnumerationResult results = null;
            Task<NameEnumerationResult> t;
            while (limit-- > 0)
            {
                try
                {
                    while ((results =
                           (t = fc.PropertyManager.EnumerateSubNamesAsync(baseUri, results, true, Defaults.WaitDelay, CancellationToken.None)).Result).HasMoreData)
                    {
                        foreach (Uri name in results)
                        {
                            names.Add(name);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            if (limit < 0 || names.Count == 0)
            {
                return null;
            }

            return names;
        }

        public static void Register<T>()
        {
            log.Info("Start Registration for type: {0}", typeof(T).Name);

            try
            {
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    // This is the name of the ServiceType that is registered with FabricRuntime. 
                    // This name must match the name defined in the ServiceManifest. If you change
                    // this name, please change the name of the ServiceType in the ServiceManifest.
                    fabricRuntime.RegisterServiceType(typeof(T).Name + "Type", typeof(T));

                    log.Info("registered process {0} name {1}", Process.GetCurrentProcess().Id, typeof(T).Name);
                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                log.Error(e, "registration failed");
            }
        }

        public static UriBuilder GetDefaultServiceUri(ServiceInitializationParameters sip)
        {
            var context = sip.CodePackageActivationContext;
            var service = sip.ServiceName;

            try
            {
                foreach (var ep in context.GetEndpoints())
                {
                    if (ep.Name.Contains("Service"))
                    {
                        var b = new UriBuilder(service);
                        if (ep.Protocol == EndpointProtocol.Tcp)
                            b.Scheme = Uri.UriSchemeNetTcp;
                        else
                            b.Scheme = Enum.GetName(typeof(EndpointProtocol), ep.Protocol);
                        b.Host = Node;
                        b.Port = ep.Port;

                        log.Info("Found endpoint name {0}, created uri {1}", ep.Name, b);
                        return b;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                    log.Warn("Failed to get host port from endpoints, aggregate exception: {0}", e.InnerException.Message);
                else
                    log.Warn("Failed to get host port from endpoints, exection: {0}", e.Message);
            }

            return null;
        }


    }
}
