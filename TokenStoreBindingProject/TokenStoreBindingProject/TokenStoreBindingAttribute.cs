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
    using System.Reflection;

    [Binding]
    public sealed class TokenStoreBindingAttribute : Attribute
    {
        [AutoResolve(ResolutionPolicyType = typeof(EasyAuthAccessTokenResolutionPolicy))]  
        public string EasyAuthAccessToken { get; set; } = "auto"; // Needs to be set to non-null value to be resolved, where the return value for token is stored 

        // INPUT PARAMETERS 
        [AutoResolve]
        public string Token_url { get; set; }
        [AutoResolve (Default = "msi")]
        public string Auth_flag { get; set; }


        public TokenStoreBindingAttribute(string Token_url_in, string Auth_flag_in) // For imperative bindings, constructor
        {
            Token_url = Token_url_in;
            Auth_flag = Auth_flag_in;
        }
        public TokenStoreBindingAttribute() // For declarative bindings, constructor
        {

        }

        internal class EasyAuthAccessTokenResolutionPolicy : IResolutionPolicy
        {
            public string TemplateBind(PropertyInfo propInfo, Attribute resolvedAttribute, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, object> bindingData) // most important params are resolvedAttribute and bindingData 
            {
                var tokenAttribute = resolvedAttribute as TokenStoreBindingAttribute;
                if (tokenAttribute == null) // If token store binding attribute is NULL 
                {
                    throw new InvalidOperationException($"Can not use {nameof(EasyAuthAccessTokenResolutionPolicy)} as a resolution policy for an attribute that does not implement {nameof(TokenStoreBindingAttribute)}");
                }

                if (!(bindingData.ContainsKey("$request") && bindingData["$request"] is HttpRequest)) // If we can't get "request" from http request
                {
                    throw new InvalidOperationException($"Can not use \"Auth_Flag = user\" with a non-HTTP triggered function.");
                }
                var request = (HttpRequest)bindingData["$request"];
                return GetEasyAuthAccessToken(request);  // returns to EasyAuthAccessToken variable 
            }

            private string GetEasyAuthAccessToken(HttpRequest request)
            {
                string errorMessage = "Failed accessing request header. Cannot find an access token for the user. Verify that this endpoint is protected by Azure App Service Authentication/Authorization.";
                StringValues headerValues;
                if (request.Headers.TryGetValue("X-MS-TOKEN-AAD-ID-TOKEN", out headerValues)) // TODO: get header depending on provider used to sign in users (i.e. AAD, facebook, google...)
                    return headerValues.ToString(); 
                throw new InvalidOperationException(errorMessage);
            }

        }

    }
}