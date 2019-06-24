// Test for tokenstore binding attribute 
namespace Microsoft.Azure.WebJobs
{
    using Microsoft.Azure.WebJobs.Description;
    using System;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute
    {
        //[AutoResolve]
        public string TokenStore_Name { get; set; }
        public string TokenStore_Service { get; set; }
        public string TokenStore_TokenName { get; set; }
        public string TokenStore_Location { get; set; } // i.e. westcentralus
        public string Obj_ID { get; set; }
        public string Tenant_ID { get; set; }
      

        public TokenStoreBindingAttribute()
        {

        }
    }
}