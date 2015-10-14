using Microsoft.ServiceFabric.Services;

namespace EchoApp
{
    public class EchoGatewayService : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            var listener = new ZBrad.FabricLib.WcfTcpGatewayListener();
            listener.Initialize(this);
            return listener;
        }
    }
}
