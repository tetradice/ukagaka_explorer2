using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GhostExplorer2
{
    [DataContract]
    public class Profile
    {
        [DataMember]
        public string LastBootVersion { get; set; }

        [DataMember]
        public string LastUsePath { get; set; }

        public Profile()
        {
        }
    }
}
