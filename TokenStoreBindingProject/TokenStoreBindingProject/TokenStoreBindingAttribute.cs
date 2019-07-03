/*
 //////// Token Store Binding //////////

 Usage: 

If Auth_Flag = MSI
    1. Token Store can be used as an imperative binding or a declarative binding (i.e. can be used as direct input param)
    2. Token_url should be the full path to the token
    3. Req can be NULL
If Auth_Flag = user
    1. Token Store binding can ONLY be used as an imperative binding  
    2. Your Azure Function must have an HTTP trigger 
    3. Token Store binding can only be executed at runtime because the req parameters depends on the HTTP trigger output to access the headers 
    4. Token_url should be path to service (i.e. dropbox) (without a trailing slash) 

Reference: https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library
*/

namespace Microsoft.Azure.WebJobs
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.WebJobs.Description;
    using System;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute, IDisposable
    {
        [AutoResolve]
        public string Token_url { get; set; }
        public string Auth_flag { get; set; }
        public HttpRequest Req { get; set; }


        public TokenStoreBindingAttribute(string Token_url_in, string Auth_flag_in, HttpRequest req_in) // For imperative bindings, constructor
        {
            Token_url = Token_url_in;
            Auth_flag = Auth_flag_in;
            Req = req_in;
        }
        public TokenStoreBindingAttribute() // For imperative bindings, constructor
        {

        }

        // Dispose method needed for implicit use of Token Store binding 
        public void Dispose()
        {

        }
    }
}