// Test for tokenstore binding attribute 
namespace Microsoft.Azure.WebJobs
{
    using Microsoft.Azure.WebJobs.Description;
    using System;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute
    {
        //[AutoResolve]
        public string Token_url { get; set; }
        public string Auth_flag { get; set; }


        public TokenStoreBindingAttribute()
        {

        }
    }
}