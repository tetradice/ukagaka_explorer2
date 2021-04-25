using System.Runtime.Serialization;

namespace GhostExplorer2
{
    [DataContract]
    public class Profile
    {
        [DataMember]
        public string LastBootVersion { get; set; }

        [DataMember]
        public string LastUsePath { get; set; }

        [DataMember]
        public string LastSortType { get; set; }

        [DataMember]
        public int MainWindowWidth { get; set; }

        [DataMember]
        public int MainWindowHeight { get; set; }

        public Profile()
        {
        }
    }
}
