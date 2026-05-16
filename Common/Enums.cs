using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{

    [DataContract]
    public enum ResponseStatus
    {
        [EnumMember] 
        ACK,
        [EnumMember] 
        NACK
    }

    [DataContract]
    public enum TransferStatus
    {
        [EnumMember] 
        IN_PROGRESS,
        [EnumMember] 
        COMPLETED
    }

}
