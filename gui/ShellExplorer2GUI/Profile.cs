using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ShellExplorer2
{
    [DataContract]
    public class Profile
    {
        [DataMember]
        public string LastBootVersion { get; set; }

        [DataMember]
        public int MainWindowWidth { get; set; }

        [DataMember]
        public int MainWindowHeight { get; set; }

        public Profile()
        {
        }
    }
}
