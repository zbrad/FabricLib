using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZBrad.FabLibs.Utilities.x64
{
    /// <summary>
    /// utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// get the node context ip or Fully Qualified Domain Name
        /// </summary>
        /// <returns>the node ip</returns>
        public static string GetNodeIp()
        {
            string nodeIp = null;

            try
            {
                NodeContext ctx = FabricRuntime.GetNodeContext();
                if (ctx != null)
                {
                    return ctx.IPAddressOrFQDN;
                }
            }
            catch (Exception)
            {
                // suppress exception, just return null
            }

            return nodeIp;
        }

        /// <summary>
        /// enumerate all fabric names
        /// </summary>
        /// <param name="client">fabric client to use</param>
        /// <param name="subName">base namespace</param>
        /// <returns>list of Uri names</returns>
        public static List<Uri> EnumerateAndPrintSubNames(FabricClient client, Uri subName)
        {
            List<Uri> result = new List<Uri>();

            Console.WriteLine("Enumerating all fabric names which matches {0}:", subName);

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
                        Console.WriteLine("\t{0}", name);
                        result.Add(name);
                    }
                }
                while (nameResult.HasMoreData);
            }
            catch (AggregateException e)
            {
                if (e.InnerException is TimeoutException)
                {
                    Console.WriteLine("EnumerateSubNamesAsync timed out");
                }
                else if (e.InnerException is FabricElementNotFoundException)
                {
                    // FabricElementNotFoundException indicates that the name in the argument did not exist in the Naming name tree.
                    Console.WriteLine("Name (\"{0}\") does not exist", subName);
                }
                else
                {
                    Console.WriteLine("Unexpected exception logged");
                    Console.WriteLine(e.Message);
                }
            }
            catch (ArgumentException e)
            {
                // One of the common reasons for ArgumentException is that the specified URI is not a valid Windows Fabric name.
                Console.WriteLine("Not a valid argument");
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception logged");
                Console.WriteLine(e.Message);
            }

            if (result.Count == 0)
            {
                Console.WriteLine("\t{0} does not have any child names.", subName);
            }

            return result;
        }

        /// <summary>
        /// Try to create an endpoint address referencing an endpoint name.  Endpoint addresses will be different for Input versus Internal Endpoint types.
        /// </summary>
        /// <param name="initParams">initialization parameters</param>
        /// <param name="endpointName">endpoint address name</param>
        /// <param name="address">the endpoint uri address</param>
        /// <returns>true if successful</returns>
        public static bool TryCreateAddress(StatefulServiceInitializationParameters initParams, string endpointName, out string address)
        {
            address = null;
            if (initParams == null)
            {
                return false;
            }

            return TryCreateAddress(initParams.CodePackageActivationContext, initParams.ReplicaId, initParams.PartitionId, endpointName, out address);
        }

        /// <summary>
        /// Try to create an endpoint address referencing an endpoint name.  Endpoint addresses will be different for Input versus Internal Endpoint types.
        /// </summary>
        /// <param name="initParams">initialization parameters</param>
        /// <param name="endpointName">endpoint address name</param>
        /// <param name="address">the endpoint uri address</param>
        /// <returns>true if successful</returns>
        public static bool TryCreateAddress(StatelessServiceInitializationParameters initParams, string endpointName, out string address)
        {
            address = null;
            if (initParams == null)
            {
                return false;
            }

            return TryCreateAddress(initParams.CodePackageActivationContext, initParams.InstanceId, initParams.PartitionId, endpointName, out address);
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

        static bool TryCreateAddress(CodePackageActivationContext context, long id, Guid partitionId, string endpointName, out string address)
        {
            address = null;
            if (context == null)
            {
                return false;
            }

            KeyedCollection<string, EndpointResourceDescription> d = context.GetEndpoints();
            if (d == null)
            {
                return false;
            }

            if (!d.Contains(endpointName))
            {
                return false;
            }

            EndpointResourceDescription erd = d[endpointName];
            StringBuilder sb = new StringBuilder();
            if (erd.EndpointType == EndpointType.Input)
            {
                sb.Append(erd.Protocol);
                sb.Append("://");
            }

            // get the node ip from fabric runtime node context
            string nodeIp = GetNodeIp();
            if (nodeIp != null)
            {
                sb.Append(nodeIp);
            }
            else
            {
                // if we don't have fabric runtime node context, default to localhost
                sb.Append("localhost");
            }

            sb.Append(':');
            sb.Append(erd.Port);
            if (erd.EndpointType == EndpointType.Input)
            {
                sb.Append('/');
                sb.Append(partitionId);
                sb.Append('_');
                sb.Append(id);
            }

            address = sb.ToString();
            return true;
        }
    }
}
