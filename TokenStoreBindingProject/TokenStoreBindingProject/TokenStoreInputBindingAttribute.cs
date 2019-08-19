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

    /*
        //////// TokenStoreInputBinding //////////

       Usage: 

       If authFlag = MSI
           1. Your Azure Function can be of any type 
           2. tokenUrl should be the full path to the token ( i.e. https://{example-tokenstore-name}.tokenstore.azure.net/services/{example-service}/tokens/{example-token-name} )
       If authFlag = user 
           1. Your Azure Function must have an HTTP trigger 
           2. tokenUrl should be path up to service ( i.e. https://{example-tokenstore-name}.tokenstore.azure.net/services/{example-service} )
           3. Be sure to specify the "Id_provider" parameter

       Returns: An access token to the specified service if it exists and is authenticated. 
   */
    [Binding]
    public sealed class TokenStoreInputBindingAttribute : Attribute
    {
        [AutoResolve(ResolutionPolicyType = typeof(EasyAuthAccessTokenResolutionPolicy))]  
        public string requestHeader { get; set; } = "auto"; // Needs to be set to non-null value to be resolved, the return value for the header is stored here

        // **** INPUT PARAMETERS ****

        /// <summary>
        /// If authFlag = "msi", tokenUrl should be full path to token name. If authFlag = "user", tokenUrl should be path up to service name. 
        /// </summary>
        [AutoResolve]
        public string tokenUrl { get; set; }

        /// <summary>
        /// If authFlag = "msi" token is retrieved using the given full path url. If authFlag = "user" the token is retrieved by appending the given url with the logged in user's credentials. 
        /// </summary>
        [AutoResolve (Default = "msi")]
        public string authFlag { get; set; } // options: msi or user 

        /// <summary>
        /// Determines how user credentials are obtained. Login options currently supported are: "aad", "facebook", or "google" . 
        /// </summary>
        [AutoResolve(Default = "aad")]
        public string identityProvider { get; set; } // options: "aad", "facebook", or "google"

        /// <summary>
        /// Check that the token url is well formatted and matches with the choice of the authFlag 
        /// </summary>
        public void CheckValidity_URL()
        {
            var msi_regex = "^https://[a-zA-Z0-9_.-]*.tokenstore.azure.net/services/[a-zA-Z0-9_.-]*/tokens/[a-zA-Z0-9_.-]*$";
            var user_regex = "^https://[a-zA-Z0-9_.-]*.tokenstore.azure.net/services/[a-zA-Z0-9_.-]*$";

            switch (this.authFlag.ToLower())
            {
                case "msi":
                    Match match_msi = Regex.Match(this.tokenUrl, msi_regex);
                    if (!match_msi.Success)
                        throw new FormatException("When using an authFlag of \"msi\" specify the token url up to the token name. Format: \"https://{token-store-name}.tokenstore.azure.net/services/{service-name}/tokens/{token-name}\" ");
                    break;
                case "user":
                    Match match_user = Regex.Match(this.tokenUrl, user_regex);
                    if (!match_user.Success)
                        throw new FormatException("When using an authFlag of \"user\" specify the token url up to the service name. Format: \"https://{token-store-name}.tokenstore.azure.net/services/{service-name}\" ");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Constructor for imperative bindings. 
        /// </summary>
        public TokenStoreInputBindingAttribute(string tokenUrlIn, string authFlagIn, string identityProviderIn) 
        {
            tokenUrl = tokenUrlIn;
            authFlag = authFlagIn;
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
            // TODO: IResolutionPolicy says obsolete?

            public string TemplateBind(PropertyInfo propInfo, Attribute resolvedAttribute, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, object> bindingData) // most important params are resolvedAttribute and bindingData 
            {

                var tokenAttribute = resolvedAttribute as TokenStoreInputBindingAttribute;
                if (tokenAttribute == null) // If token store binding attribute is NULL 
                {
                    throw new InvalidOperationException($"Can not use {nameof(EasyAuthAccessTokenResolutionPolicy)} as a resolution policy for an attribute that does not implement {nameof(TokenStoreInputBindingAttribute)}");
                }

                if (tokenAttribute.authFlag.ToLower() == "user")
                {
                    if (!(bindingData.ContainsKey("$request") && bindingData["$request"] is HttpRequest)) // If we can't get "request" from http request
                    {
                        throw new InvalidOperationException($"Http request not accessible. An authFlag of user requires the use of an Http triggered function.");
                    }
                    var request = (HttpRequest)bindingData["$request"];
                    return GetRequestHeader(request, tokenAttribute.identityProvider);  // returns to RequestHeader variable 
                }
                return null; // Header value is not required for an authFlag of "msi" 
            }

            /// <summary>
            /// Returns a header string depending on the specified login service.  
            /// </summary>
            private string GetRequestHeader(HttpRequest request, string identityProvider)
            {
                //TODO: A better name for this method might be GetIdentityProviderToken
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