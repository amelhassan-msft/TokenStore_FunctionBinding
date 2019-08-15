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

        /// <summary>
        /// Constructor for declarative bindings.  
        /// </summary>
        public TokenStoreHelper(string tokenResourceUrlIn, string tokenDisplayNameIn, TokenStoreInputBindingAttribute attributeIn)
        {
            tokenResourceUrl = tokenResourceUrlIn;
            tokenDisplayName = tokenDisplayNameIn;
            attribute = attributeIn;
        }
        /// <summary>
        /// Get a token if it exists, otherwise create a token and prompt user to navigate to Token Store to login.  
        /// </summary>
        public async Task<String> get_or_create_token()
        {
            // Extract path to Token Store resource 
            Uri tokenURI = new Uri(attribute.tokenUrl);
            string path = tokenURI.Authority;
            string tokenStoreResource = $"https://{tokenURI.Authority}"; // Format: "https://{token-store-name}.tokenstore.azure.net"

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource); // Get a token to access Token Store

            // Get token from Token Store
            var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, tokenResourceUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
            HttpClient client = new HttpClient();
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
                    throw new ArgumentException($"Token Store token status message: {tokenStoreToken.Status.Error.Message}"); // If token already exists, but is not authenticated 
                }
            }
            else // Specified token does not exist or msi does not have permission to "get" tokens, so try to create a token keeping in mind that msi might not have permission to "create" tokens 
            {
                throw (await create_token(tokenStoreApiToken, tokenStoreResource));
            }
        }

        /// <summary>
        /// Create a token either with specified token name or with the user's ID. Prompt user to navigate to Token Store to authenticate the token. 
        /// </summary>
        public async Task<Exception> create_token(string tokenStoreApiToken, string tokenStoreResource)
        {
            HttpClient client = new HttpClient();
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
                throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. Specified token does not exist. A new token with displayname {tokenDisplayName} was created. Navigate to {tokenResourceUrl}/login to login. Further details: {responseStr}");
            else  // msi does not have permission to create tokens in this token store 
                throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. MSI does not have \"get\" and/or \"create\" permission for the following token store: {tokenStoreResource}. Further details: {responseStr}");

        }
    }
}
