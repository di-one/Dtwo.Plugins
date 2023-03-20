using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Plugins
{
    public abstract class PluginController
    {
        public PluginInfos Infos { get; private set; }
        public Assembly Assembly { get; private set; }

        public PluginController(PluginInfos infos, Assembly assembly)
        {
            Infos = infos;
            Assembly = assembly;
        }
    }
}
