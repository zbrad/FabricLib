using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public class RestService : WcfServiceBase
    {
        protected override Smc.Binding GetBinding()
        {
            return new Sm.WebHttpBinding(Sm.WebHttpSecurityMode.None);
        }
    }
}
