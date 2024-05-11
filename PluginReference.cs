using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Plugins
{
    [DataContract]
    public class PluginDependency
    {
        [DataMember]
        public string? PluginName { get; set; }

        [DataMember]
        public PluginVersion? PluginVersionMin { get; set; }
    }
}
