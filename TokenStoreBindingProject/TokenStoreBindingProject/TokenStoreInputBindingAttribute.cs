namespace Microsoft.Azure.WebJobs
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.WebJobs.Description;
    using Microsoft.Azure.WebJobs.Host.Bindings;
    using Microsoft.Azure.WebJobs.Host.Bindings.Path;
    using Microsoft.Extensions.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// TokenStoreInputBinding 
    /// Usage: 
    ///     If scenario = "tokenName" 
    ///         1. Your Azure Function can be of any type 
    ///         2. tokenUrl should be the full path to the token ( i.e. https://{example-tokenstore-name}.tokenstore.azure.net/services/{example-service}/tokens/{example-token-name} )
    ///         3. The "identityProvider" parameter can be null
    ///     If scenario = "user" 
    ///         1. Your Azure Function must have an HTTP trigger 
    ///         2. tokenUrl should be path up to service ( i.e. https://{example-tokenstore-name}.tokenstore.azure.net/services/{example-service} )
    ///         3. Be sure to specify the "identityProc" parameter
    /// Returns: An access token to the specified service if it exists and is authenticated. 
    /// </summary>
    /// 

    [Binding]
    public sealed class TokenStoreInputBindingAttribute : Attribute
    {
        [AutoResolve(ResolutionPolicyType = typeof(EasyAuthAccessTokenResolutionPolicy))]  
        public string requestHeader { get; set; } = "auto"; // Needs to be set to non-null value to be resolved, the return value for the header is stored here

        // **** INPUT PARAMETERS ****

        /// <summary>
        /// If scenario = "tokenName", tokenUrl should be full path to token name. If scenario = "user", tokenUrl should be path up to service name. 
        /// </summary>
        [AutoResolve]
        public string tokenUrl { get; set; }

        /// <summary>
        /// If scenario = "tokenName" token is retrieved using the given full path url. If scenario = "user" the token is retrieved by appending the given url with the logged in user's credentials. 
        /// </summary>
        [AutoResolve (Default = "tokenName")]
        public string scenario { get; set; } // options: "tokenName" or "user" 

        /// <summary>
        /// Determines how user credentials are obtained. Login options currently supported are: "aad", "facebook", or "google" . 
        /// </summary>
        [AutoResolve(Default = "aad")]
        public string identityProvider { get; set; } // options: "aad", "facebook", or "google"

        /// <summary>
        /// Check that the token url is well formatted and matches with the choice of the scenario 
        /// </summary>
        public void CheckValidity_URL()
        {
            var tokenName_regex = "^https://[a-zA-Z0-9_.-]*.tokenstore.azure.net/services/[a-zA-Z0-9_.-]*/tokens/[a-zA-Z0-9_.-]*$";
            var user_regex = "^https://[a-zA-Z0-9_.-]*.tokenstore.azure.net/services/[a-zA-Z0-9_.-]*$";

            switch (this.scenario.ToLower())
            {
                case "tokenName":
                    Match match_tokenName = Regex.Match(this.tokenUrl, tokenName_regex);
                    if (!match_tokenName.Success)
                        throw new FormatException("When using a scenario of \"tokenName\" specify the token url up to the token name. Format: \"https://{token-store-name}.tokenstore.azure.net/services/{service-name}/tokens/{token-name}\" ");
                    break;
                case "user":
                    Match match_user = Regex.Match(this.tokenUrl, user_regex);
                    if (!match_user.Success)
                        throw new FormatException("When using a scenario of \"user\" specify the token url up to the service name. Format: \"https://{token-store-name}.tokenstore.azure.net/services/{service-name}\" ");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Constructor for imperative bindings. 
        /// </summary>
        public TokenStoreInputBindingAttribute(string tokenUrlIn, string scenarioIn, string identityProviderIn) 
        {
            tokenUrl = tokenUrlIn;
            scenario = scenarioIn;
            identityProvider = identityProviderIn;
        }

        /// <summary>
        /// Constructor for declarative bindings.  
        /// </summary>
        public TokenStoreInputBindingAttribute()
        {

        }

        /// <summary>
        /// Obtains httpRequest and header.  
        /// </summary>
        internal class EasyAuthAccessTokenResolutionPolicy : IResolutionPolicy
        {
            public string TemplateBind(PropertyInfo propInfo, Attribute resolvedAttribute, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, object> bindingData) // most important params are resolvedAttribute and bindingData 
            {

                var tokenAttribute = resolvedAttribute as TokenStoreInputBindingAttribute;
                if (tokenAttribute == null) // If token store binding attribute is NULL 
                {
                    throw new InvalidOperationException($"Can not use {nameof(EasyAuthAccessTokenResolutionPolicy)} as a resolution policy for an attribute that does not implement {nameof(TokenStoreInputBindingAttribute)}");
                }

                if (tokenAttribute.scenario.ToLower() == "user")
                {
                    if (!(bindingData.ContainsKey("$request") && bindingData["$request"] is HttpRequest)) // If we can't get "request" from http request
                    {
                        throw new InvalidOperationException($"Http request not accessible. A scenario of \"user\" requires the use of an Http triggered function.");
                    }
                    var request = (HttpRequest)bindingData["$request"];
                    return GetIdentityProviderToken(request, tokenAttribute.identityProvider);  // returns to RequestHeader variable 
                }
                return null; // Header value is not required for an scenario of "tokenName" 
            }

            /// <summary>
            /// Returns a header string depending on the specified login service.  
            /// </summary>
            private string GetIdentityProviderToken(HttpRequest request, string identityProvider)
            {
                string errorMessage = $"Failed accessing request header. Cannot find an access token for the user. Verify that this endpoint is protected by the specified identity provider: {identityProvider}.";
                StringValues headerValues;

                switch(identityProvider.ToLower())
                {
                    case "aad":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-AAD-ID-TOKEN", out headerValues))
                            return headerValues.FirstOrDefault();
                        break;
                    case "facebook":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-FACEBOOK-ACCESS-TOKEN", out headerValues))
                            return headerValues.FirstOrDefault();
                        break;
                    case "google":
                        if (request.Headers.TryGetValue("X-MS-TOKEN-GOOGLE-ID-TOKEN", out headerValues))
                            return headerValues.FirstOrDefault();
                        break;
                    default:
                        throw new FormatException("Incorrect usage of {identityProvider} parameter. Input must be of type string: \"aad\", \"facebook\", or \"google\"");
                }

                throw new InvalidOperationException(errorMessage);
            }
  
        }
    }

}