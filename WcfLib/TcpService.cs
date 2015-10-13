using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public class TcpService : WcfServiceBase
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        protected override Smc.Binding GetBinding()
        {
            return new Sm.NetTcpBinding(Sm.SecurityMode.None);
        }
    }
}
