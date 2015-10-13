using System;
using System.ServiceModel.Description;
using System.ServiceModel.Routing;

namespace ZBrad.WcfLib
{
    public interface IRouter
    {
        RoutingConfiguration Configuration { get; set; }
        RoutingExtension Extension { get; }
        Resolver Resolver { get; }

        void Initialize(Uri path, Resolver resolver);
    }
}