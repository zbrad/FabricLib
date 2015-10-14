using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services;
using ZBrad.FabricLib.Utilities;
using ZBrad.WcfLib;
using ZBrad.FabricLib.Gateway;
using Smr = System.ServiceModel.Routing;

namespace ZBrad.FabricLib
{
    public class FabricRouter<T> : Router<T> where T : WcfServiceBase, new()
    {
        public FabricRouter()
        {
            this.Service = new T();
        }

        public override void Initialize(Uri path, Resolver resolver)
        {
            base.Initialize(path, resolver);

            var retry = this.Service.Host.AddServiceEndpoint(
                typeof(Smr.IRequestReplyRouter),
                this.Service.Binding,
                "retry"
                );

            var fr = (FabricResolver) resolver;
            fr.Initialize(retry.Address.Uri);
            retry.Behaviors.Add(resolver);
        }
    }

    public class WcfTcpGatewayListener : Listener
    {
        FabricResolver resolver = new FabricResolver();
        FabricRouter<TcpService> router = new FabricRouter<TcpService>();

        //TcpRouter router = new TcpRouter();

        public void Initialize(StatelessService stateless)
        {
            base.Initialize(stateless, router.Service);
            this.Path = Utility.GetDefaultServiceUri(stateless.ServiceInitializationParameters).Uri;
            router.Initialize(this.Path, resolver);
        }
    }
}
