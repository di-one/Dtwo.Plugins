using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Plugins
{
    [DataContract]
    public class PluginInfos
    {
        [DataMember]
        public string? Name { get; set; }
        [DataMember]
        public string? Description { get; set; }
        [DataMember]
        public string? AssemblyPath { get; set; }
        [DataMember]
        public PluginVersion? Version { get; set; }

        [DataMember]
        public int DofusVersion { get; set; }

        [DataMember]
        public List<PluginDependency>? Dependencies { get; set; }
    }
}
