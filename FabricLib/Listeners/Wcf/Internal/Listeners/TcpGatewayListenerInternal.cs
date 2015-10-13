using System;
using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf.Internal
{
    internal class TcpGatewayListenerInternal : TcpListenerInternal
    {
        protected override WcfServiceBase GetWcfService()
        {
            // modify path for gateway to just use base
            var b = new UriBuilder(this.Path);
            b.Path = "/";

            var x = new TcpGatewayService();
            var r = new FabricResolver();
            x.Initialize(b.Uri, r);
            return x;
        }
    }
}
