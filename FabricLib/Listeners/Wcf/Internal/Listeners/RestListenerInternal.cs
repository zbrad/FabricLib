using ZBrad.WcfLib;

namespace ZBrad.FabricLib.Wcf
{
    internal class RestListenerInternal : WcfListenerBase
    { 
        protected override WcfServiceBase GetWcfService()
        {
            var service = new RestService();
            service.Initialize(this.Path, this.Instance);
            return service;
        }
    }
}
