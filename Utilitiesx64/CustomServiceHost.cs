using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;

namespace ZBrad.FabLibs.Utilities.x64
{

    /// <summary>
    /// custom service host
    /// </summary>
    public static class CustomServiceHost
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// provides a service host
        /// </summary>
        /// <param name="serviceTypeName">the service type name</param>
        /// <param name="serviceTypeImplementation">the assembly type</param>
        public static void RegisterServiceTypeAndWait(string serviceTypeName, Type serviceTypeImplementation)
        {
            // Create a Windows Fabric Runtime
            using (FabricRuntime fabricRuntime = FabricRuntime.Create())
            {
                try
                {
                    // Register ServiceFactory with the runtime
                    log.Info("ServiceHost {0} registering service type: {1}", Process.GetCurrentProcess().Id, serviceTypeName);

                    fabricRuntime.RegisterServiceType(serviceTypeName, serviceTypeImplementation);

                    // Wait for WindowsFabric to place services in this process
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                }
            }  
        }

        /// <summary>
        /// create a service group
        /// </summary>
        /// <param name="serviceGroupTypeName">service group type name</param>
        /// <param name="serviceMemberTypeNames">list of type names</param>
        /// <param name="serviceTypeImplementations">list of assembly types</param>
        public static void RegisterServiceGroupTypeAndWait(string serviceGroupTypeName, List<string> serviceMemberTypeNames, List<Type> serviceTypeImplementations)
        {
            // Create a Windows Fabric Runtime
            using (FabricRuntime fabricRuntime = FabricRuntime.Create())
            {
                try
                {
                    var serviceGroupFactory = new ServiceGroupFactory();

                    for (int i = 0; i < serviceMemberTypeNames.Count; i++)
                    {
                        serviceGroupFactory.AddServiceType(serviceMemberTypeNames[i], serviceTypeImplementations[i]);
                    }

                    // Register ServiceFactory with the runtime
                    Console.WriteLine("ServiceHost is registering serviceGroup factory: {0}", serviceGroupTypeName);
                    fabricRuntime.RegisterServiceGroupFactory(serviceGroupTypeName, serviceGroupFactory);

                    // Wait for WindowsFabric to place services in this process
                    Console.ReadLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.ReadLine();
                }
            }
        }
    }
}
