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
    using Microsoft.Azure.WebJobs.Host.Bindings;
    using Microsoft.Azure.WebJobs.Host.Bindings.Path;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Graph;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute
    {
        [AutoResolve(ResolutionPolicyType = typeof(EasyAuthAccessTokenResolutionPolicy))]  
        public string RequestHeader { get; set; } = "auto"; // Needs to be set to non-null value to be resolved, the return value for the header is stored here

        // **** INPUT PARAMETERS ****

        /// <summary>
        /// If Auth_flag = "msi", Token_url should be full path to token name. If Auth_flag = "user", Token_url should be path up to service name. 
        /// </summary>
        [AutoResolve]
        public string Token_url { get; set; }

        /// <summary>
        /// If Auth_flag = "msi" authentication to Token Store is obtained using the app's identity. If Auth_flag = "user" authentication to Token Store is obtained using the user's credentials. 
        /// </summary>
        [AutoResolve (Default = "msi")]
        public string Auth_flag { get; set; } // options: msi or user 

        /// <summary>
        /// Determines how user credentials are obtained. Login options currently supported are: "aad", "google", or "facebook". 
        /// </summary>
        [AutoResolve(Default = "aad")]
        public string Identity_provider { get; set; } // options: aad, google, or facebook

        // ****************************

        /// <summary>
        /// Constructor for imperative bindings. 
        /// </summary>
        public TokenStoreBindingAttribute(string Token_url_in, string Auth_flag_in, string Identity_provider_in) 
        {
            Token_url = Token_url_in;
            Auth_flag = Auth_flag_in;
            Identity_provider = Identity_provider_in;
        }

        /// <summary>
        /// Constructor for declarative bindings.  
        /// </summary>
        public TokenStoreBindingAttribute()
        {

        }

        /// <summary>
        /// Obtains HttpRequest and header.  
        /// </summary>
        internal class EasyAuthAccessTokenResolutionPolicy : IResolutionPolicy
        {
            public string TemplateBind(PropertyInfo propInfo, Attribute resolvedAttribute, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, object> bindingData) // most important params are resolvedAttribute and bindingData 
            {

                var tokenAttribute = resolvedAttribute as TokenStoreBindingAttribute;
                if (tokenAttribute == null) // If token store binding attribute is NULL 
                {
                    throw new InvalidOperationException($"Can not use {nameof(EasyAuthAccessTokenResolutionPolicy)} as a resolution policy for an attribute that does not implement {nameof(TokenStoreBindingAttribute)}");
                }

                if (tokenAttribute.Auth_flag.ToLower() == "user")
                {
                    if (!(bindingData.ContainsKey("$request") && bindingData["$request"] is HttpRequest)) // If we can't get "request" from http request
                    {
                        throw new InvalidOperationException($"Http request not accessible. An Auth_flag of user requires the use of an Http triggered function.");
                    }
                    var request = (HttpRequest)bindingData["$request"];
                    return GetRequestHeader(request, tokenAttribute.Identity_provider);  // returns to EasyAuthAccessToken variable 
                }
                return "NULL"; // Header value is not required for an Auth_flag of "msi" 
            }

            /// <summary>
            /// Returns a header string depending on the specified login service.  
            /// </summary>
            private string GetRequestHeader(HttpRequest request, string Identity_provider)
            {
                string errorMessage = $"Failed accessing request header. Cannot find an access token for the user. Verify that this endpoint is protected by the specified identity provider: {Identity_provider}.";
                StringValues headerValues;

                switch(Identity_provider.ToLower())
                {
                    case "aad":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-AAD-ID-TOKEN", out headerValues))
                            return headerValues.ToString();
                        break;
                    case "facebook":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-FACEBOOK-ACCESS-TOKEN", out headerValues))
                            return headerValues.ToString();
                        break;
                    case "google":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-GOOGLE-ID-TOKEN", out headerValues))
                            return headerValues.FirstOrDefault();
                        break;
                    default:
                        throw new FormatException("Incorrect usage of Identity_provider parameter. Input must be of type string: \"aad\", \"facebook\", or \"google\" ");
                }

                throw new InvalidOperationException(errorMessage);
            }
  
        }

    }

    public enum ID_Providers { aad, facebook, google }
}