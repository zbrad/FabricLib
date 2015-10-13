using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.WcfLib
{
    public interface IStartable
    {
        Task StartAsync();
        Task StopAsync();
    }
}
