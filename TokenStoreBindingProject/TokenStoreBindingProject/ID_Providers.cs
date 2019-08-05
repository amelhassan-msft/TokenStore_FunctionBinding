using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.Azure.WebJobs
{
    [DataContract, Serializable]
    public enum ID_Providers
    {
        [EnumMember(Value = "aad")]
        aad = 1,

        [EnumMember(Value = "facebook")]
        facebook = 2,

        [EnumMember(Value = "google")]
        google = 3
    }
}
