using Facebook;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// In the case of the "user" scenario, build the full url path to the token using naming conventions and user credentials 
    /// </summary>
    public class GetTokenName
    {
        public string tokenDisplayName { get; set; } = "empty";
        public string tokenResourceUrl { get; set; }

        /// <summary>
        /// Retrieve full path to token in Token Store using the signed in user's credentials 
        /// </summary>
        public GetTokenName(TokenStoreInputBindingAttribute attribute)
        {
            string header_token = attribute.requestHeader; // Access or ID token retrieved from header
                                                           // Build path to token 
            switch (attribute.identityProvider.ToLower())
            {
                case "aad":
                    GetAadTokenPath(header_token, attribute);
                    break;
                case "facebook":
                    GetFacebookTokenPath(header_token, attribute);
                    break;
                case "google":
                    GetGoogleTokenPath(header_token, attribute);
                    break;
                default:
                    throw new InvalidOperationException("Incorrect usage of identityProvider parameter. Input must be one of the following strings: aad, facebook, or google");
            }
        }


        // ***************************** Functions to deal with headers and extract token name based on identityProvider ******************************************

        /// <summary>
        /// If Id_provider = "aad", the token name is built from the tenant Id and object Id. 
        /// If Id_provider = "aad", the token name is built from the tenant Id and object Id. 
        /// </summary>
        private void GetAadTokenPath(string header_token, TokenStoreInputBindingAttribute attribute) // Token name based on tenant and object IDs 
        {
            JwtSecurityTokenHandler JwtHandler = new JwtSecurityTokenHandler();
            if (!JwtHandler.CanReadToken(header_token))
                throw new ArgumentException("AAD ID token cannot be read as JWT");

            var securityToken = JwtHandler.ReadJwtToken(header_token);
            var payload = securityToken.Payload; // extract payload data 
            string tenantId = null;
            string objectId = null;

            foreach (Claim claim in payload.Claims)
            {
                if (claim.Type == "tid")
                    tenantId = claim.Value;
                if (claim.Type == "oid")
                    objectId = claim.Value;
                if (claim.Type == "upn")
                    tokenDisplayName = claim.Value;
            }

            if (tenantId == null)
                throw new ArgumentException("AAD Token Error: TenantID cannot be read");
            else if (objectId == null)
                throw new ArgumentException("AAD Token Error: ObjectID cannot be read");
            else
                tokenResourceUrl = $"{attribute.tokenUrl}/tokens/{tenantId}-{objectId}"; // token uri
        }

        /// <summary>
        /// If Id_provider = "facebook", the token name is built from the facebook id. 
        /// </summary>
        private void GetFacebookTokenPath(string header_token, TokenStoreInputBindingAttribute attribute)
        {
            try
            {
                var fb = new FacebookClient(header_token);
                var result = (IDictionary<string, object>)fb.Get("/me?fields=id");
                var username = (IDictionary<string, object>)fb.Get("/me?fields=name");
                tokenDisplayName = "Facebook: " + (string)username["name"];
                tokenResourceUrl = $"{attribute.tokenUrl}/tokens/{(string)result["id"]}"; // Token uri 
            }
            catch (FacebookOAuthException)
            {
                throw new ArgumentException("Could not read user id from Facebook access token.");
            }
        }

        /// <summary>
        /// If Id_provider = "google", the token name is built from the sub id. 
        /// </summary>
        private void GetGoogleTokenPath(string header_token, TokenStoreInputBindingAttribute attribute) // Token name based on sub (i.e. the google user id) 
        {
            JwtSecurityTokenHandler JwtHandler = new JwtSecurityTokenHandler();
            if (!JwtHandler.CanReadToken(header_token))
                throw new ArgumentException("Google ID token cannot be read as JWT");

            var securityToken = JwtHandler.ReadJwtToken(header_token);
            var payload = securityToken.Payload; // extract payload data 

            string user_id = null;

            foreach (Claim claim in payload.Claims)
            {
                if (claim.Type == "sub")
                    user_id = claim.Value;
                if (claim.Type == "email")
                    tokenDisplayName = claim.Value;
            }

            if (user_id == null)
                throw new ArgumentException("Could not read user id from Google ID token.");
            tokenResourceUrl = $"{attribute.tokenUrl}/tokens/{user_id}"; // Token uri 
        }
    }
}