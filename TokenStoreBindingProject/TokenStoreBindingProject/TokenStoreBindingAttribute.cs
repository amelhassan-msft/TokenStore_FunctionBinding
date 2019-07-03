/*
 //////// Token Store Binding //////////

Usage: 

If Auth_Flag = MSI
    1. Token Store can be used as an imperative binding or a declarative binding (i.e. can be used as direct input param to your Azure Function)
    2. Token_url should be the full path to the token ( i.e. https://{example-tokenstore-name}.{example-location}.tokenstore.azure.net/services/{example-service}/tokens/{example-token-name} )
    3. Do NOT set the "Req" parameter 
If Auth_Flag = user
    1. Token Store binding can ONLY be used as an imperative binding  
    2. Your Azure Function must have an HTTP trigger 
    3. Token Store binding can only be executed at runtime because the req parameters depends on the HTTP trigger output to access the headers 
    4. Token_url should be path upto service ( i.e. https://{example-tokenstore-name}.{example-location}.tokenstore.azure.net/services/{example-service} )

Reference (declartive vs. imperative bindings): https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library

*/

namespace Microsoft.Azure.WebJobs
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.WebJobs.Description;
    using System;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute
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
        public TokenStoreBindingAttribute() // For declarative bindings, constructor
        {

        }

    }
}