using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember]
        public List<string> Headers { get; set; }

        [DataMember]
        public Dictionary<string, string> Units { get; set; }
    }
}