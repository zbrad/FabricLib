using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Routing;
using System.Text;
using System.Threading.Tasks;
using System;

namespace ZBrad.WcfLib
{
    public interface IRouter
    { 
        //ServiceEndpoint Retry { get; set; }
        Resolver Resolver { get; }
        RoutingConfiguration Configuration { get; set; }
        RoutingBehavior Behavior { get; set; }
        RoutingExtension Extension { get; set; }
    }
}
