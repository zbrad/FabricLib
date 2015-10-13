using Microsoft.ServiceFabric.Services;

namespace EchoApp
{
    public class EchoService : StatelessService, IEcho
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            return new ZBrad.FabricLib.Wcf.TcpListener(this);
        }

        public string Echo(string text)
        {
            return "Echo: " + text;
        }
    }
}
