using System;
using Sm = System.ServiceModel;
using Smc = System.ServiceModel.Channels;
using Smd = System.ServiceModel.Description;
using System.Collections.Generic;

namespace ZBrad.WcfLib
{
    public sealed class TcpService : WcfServiceBase
    {
        static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        static Smc.Binding binding = new Sm.NetTcpBinding(Sm.SecurityMode.None);

        public override Smc.Binding Binding { get { return binding; } }

        public override void Initialize(Uri path, object instance)
        {
            if (path.Scheme.Equals("tcp"))
            {
                var b = new UriBuilder(path);
                b.Scheme = Uri.UriSchemeNetTcp;
                path = b.Uri;
            }

            base.Initialize(path, instance);
        }
    }
}
