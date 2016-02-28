using Microsoft.ServiceFabric.Services;

namespace EchoApp
{
    public class EchoGateway : StatelessService
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ZBrad.FabLibs.Wcf.TcpGateway(this);
        }
    }
}
