using Microsoft.ServiceFabric.Services;
using ZBrad.FabricLib;

namespace EchoApp
{
    public class EchoService : StatelessService, IEcho
    {
        protected override ICommunicationListener CreateCommunicationListener()
        {
            var listener = new ZBrad.FabricLib.WcfTcpListener();
            listener.Initialize(this);
            return listener;
        }

        public string Echo(string text)
        {
            return "Echo: " + text;
        }
    }
}
