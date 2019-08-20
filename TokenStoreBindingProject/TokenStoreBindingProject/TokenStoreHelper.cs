using Microsoft.Azure.Services.AppAuthentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs
{
    /// <summary>
    /// This class deals with making calls to Token Store.  
    /// </summary>
    public class TokenStoreHelper
    {
        private string tokenResourceUrl { get; set; }
        private string tokenDisplayName { get; set; }
        private TokenStoreInputBindingAttribute attribute { get; set; }
        private static HttpClient client { get; set; }

        /// <summary>
        /// Constructor for declarative bindings.  
        /// </summary>
        public TokenStoreHelper(string tokenResourceUrlIn, string tokenDisplayNameIn, TokenStoreInputBindingAttribute attributeIn)
        {
            tokenResourceUrl = tokenResourceUrlIn;
            tokenDisplayName = tokenDisplayNameIn;
            attribute = attributeIn;
            client = new HttpClient();
        }
        /// <summary>
        /// Get a token if it exists, otherwise create a token and prompt user to navigate to Token Store to login.  
        /// </summary>
        public async Task<String> GetOrCreateToken()
        {
            // Extract path to Token Store resource 
            Uri tokenURI = new Uri(attribute.tokenUrl);
            string path = tokenURI.Authority;
            string tokenStoreResource = $"https://{tokenURI.Authority}"; // Format: "https://{token-store-name}.tokenstore.azure.net"

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource); // Get a token to access Token Store using the Function App's MSI

            // Get token from Token Store
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, tokenResourceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
            HttpResponseMessage response = await client.SendAsync(request);

            Token tokenStoreToken;

            if (response.IsSuccessStatusCode) // Specified token exists and msi has permission to "get" tokens  
            {
                var serviceApiToken = await response.Content.ReadAsStringAsync();
                tokenStoreToken = JsonConvert.DeserializeObject<Token>(serviceApiToken); // Access token to specified service 

                try // Get token if it exists
                {
                    return tokenStoreToken.Value.AccessToken;
                }

                catch
                {
                    throw new SystemException($"Token Store token status message: {tokenStoreToken.Status.Error.Message}"); // If token already exists, but is not authenticated 
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)// Specified token does not exist or msi does not have permission to "get" tokens, so try to create a token keeping in mind that MSI might not have permission to "create" tokens 
            {
                throw(await CreateToken(tokenStoreApiToken, tokenStoreResource));
            }
            else {
                throw new SystemException($"Call to Token Store resource failed. Http response: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Create a token either with specified token name or with the user's ID. Prompt user to navigate to Token Store to authenticate the token. 
        /// </summary>
        private async Task<SystemException> CreateToken(string tokenStoreApiToken, string tokenStoreResource)
        {
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, tokenResourceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);

            var requestContent = JObject.FromObject(new
            {
                displayName = tokenDisplayName
            });

            request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");
            var response = await client.SendAsync(request);
            var responseStr = await response.Content.ReadAsStringAsync(); // Provides details of created token

            if (response.IsSuccessStatusCode) // Token was succesfully created 
                return new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. Specified token did not exist. A new token with displayname {tokenDisplayName} was created. Navigate to {tokenResourceUrl}/login to login. Further details: {responseStr}");
            else  // msi does not have permission to create tokens in this token store 
                return new SystemException($"Http response message status code: {response.StatusCode.ToString()}. MSI does not have \"get\" and/or \"create\" permission for the following token store: {tokenStoreResource}. Further details: {responseStr}");

        }
    }
}
