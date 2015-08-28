﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using System.Fabric;
using System.Fabric.Description;

namespace ZBrad.ServiceFabric.WcfLib
{
    internal interface ILibListener
    {
        ServiceInitializationParameters Init { get; }
        IEventLog Log { get; }
    }

    internal static class Util
    {
        public static readonly string Node = FabricRuntime.GetNodeContext().IPAddressOrFQDN;

        public static int GetPort(ILibListener listener, string section)
        {
            int port = GetPortFromConfig(listener, section);
            if (port > 0)
                return port;

            return GetPortFromEndpoints(listener);
        }

        static int GetPortFromConfig(ILibListener listener, string name)
        {
            try
            {
                var context = listener.Init.CodePackageActivationContext;
                var config = context.GetConfigurationPackageObject("Config");
                if (config == null)
                    return 0;
                var section = config.Settings.Sections[name];
                if (section == null)
                    return 0;
                var property = section.Parameters["Port"];
                if (property == null)
                    return 0;
                int port = int.Parse(property.Value);
                return port;
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                    listener.Log.Warn("Failed to get host port from config, aggregate exception: {0}", e.InnerException.Message);
                else
                    listener.Log.Warn("Failed to get host port from config, exection: {0}", e.Message);
            }

            return 0;
        }

        static int GetPortFromEndpoints(ILibListener listener)
        {
            try
            {
                var context = listener.Init.CodePackageActivationContext;
                foreach (var ep in context.GetEndpoints())
                {
                    if (ep.EndpointType == EndpointType.Input && ep.Name.Contains("Service"))
                    {
                        listener.Log.Info("Using endpoint name {0} protocol {1} port {2} from service endpoints", ep.Name, ep.Protocol, ep.Port);
                        return ep.Port;
                    }
                }
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                    listener.Log.Warn("Failed to get host port from endpoints, aggregate exception: {0}", e.InnerException.Message);
                else
                    listener.Log.Warn("Failed to get host port from endpoints, exection: {0}", e.Message);
            }

            return 0;
        }

    }
}