using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Dtwo.Plugins
{
    [DataContract]
    public class PluginVersion
    {
        [DataMember]
        public int Major { get; set; }
        [DataMember]
        public int Minor { get; set; }
        [DataMember]
        public int Patch { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null)
                return false;

            PluginVersion? other = obj as PluginVersion;

            if (other == null)
                return false;

            return other.Major == Major && other.Minor == Minor && other.Patch == Patch;
        }

        public bool IsInferiorTo(PluginVersion other)
        {
            if (Major > other.Major)
            {
                return true;
            }

            if (Minor > other.Minor)
            {
                return true;
            }

            if (Patch > other.Patch)
            {
                return true;
            }

            return false;
        }
    }
}
