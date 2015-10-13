using Microsoft.ServiceFabric.Services;

namespace EchoApp
{
    public class EchoGatewayService : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ZBrad.FabricLib.Wcf.TcpGateway(this);
        }
    }
}
