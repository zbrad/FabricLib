using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public class RestService : WcfServiceBase
    {
        static Smc.Binding binding = new Sm.WebHttpBinding(Sm.WebHttpSecurityMode.None);
        public override Smc.Binding Binding {  get { return binding; } }
    }
}
