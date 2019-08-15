using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config; // must include for IExtensionConfigProvider 
using Microsoft.Graph;
using Newtonsoft.Json;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using System.Text;
using Facebook;
using System.Collections.Generic;

[Extension("TokenStore")]
public class TokenStoreInputBinding_ExtensionProvider : IExtensionConfigProvider
{
    private string tokenDisplayName = "empty";
    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreInputBindingAttribute>();
        rule.BindToInput<String>(BuildItemFromAttribute); // <{output type of binding}>
    }

    // ******************************************** Token Store Helper Functions ********************************************************

    /// <summary>
    /// Get a token if it exists, otherwise create a token and prompt user to navigate to Token Store to login.  
    /// </summary>
    public async Task<String> get_or_create_token(string tokenResourceUrl, string tokenStoreResource, string Identity_provider)
    {
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
                throw(await create_token(tokenResourceUrl, tokenStoreApiToken, tokenStoreResource, Identity_provider));
        }
    }

    /// <summary>
    /// Create a token either with specified token name or with the user's ID. Prompt user to navigate to Token Store to authenticate the token. 
    /// </summary>
    public async Task<Exception> create_token(string tokenResourceUrl, string tokenStoreApiToken, string tokenStoreResource, string Identity_provider)
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

    // ******************************************* MAIN FUNCTION ***********************************************************
    /// <summary>
    /// Main function for TokenStoreInputBinding logic. Returns an access token for specified service, or creates a token if it does not exist. 
    /// </summary>
    private async Task<String> BuildItemFromAttribute(TokenStoreInputBindingAttribute attribute, ValueBindingContext arg2)
    {
        attribute.CheckValidity_URL();
        string tokenResourceUrl = attribute.tokenUrl; // for user scienario only specify path up to service, for msi scienario specify path up to token name  

        // Deafault token display name 
        int pos = tokenResourceUrl.LastIndexOf("/") + 1;
        var tokenId = tokenResourceUrl.Substring(pos, tokenResourceUrl.Length - pos); 

        try
        {
            if (attribute.authFlag.ToLower() == "user") // If Flag = "USER" or "user" 
            {
                GetTokenName getTokenName = new GetTokenName(attribute);
                tokenResourceUrl = getTokenName.tokenResourceUrl;
                tokenDisplayName = getTokenName.tokenDisplayName;
            }

            // Shared logic for If authFlag = "msi" or "user"
            if (attribute.authFlag.ToLower() == "msi" || attribute.authFlag.ToLower() == "user")
            {
                try
                {
                    // Use TokenStoreHelper to interact with your Token Store resource 
                    TokenStoreHelper tokenStoreHelper = new TokenStoreHelper(tokenResourceUrl, tokenDisplayName, attribute);
                    return await tokenStoreHelper.get_or_create_token();
                }
                catch(Exception exp)
                {
                    throw new ArgumentException($"{exp}");
                }
            }
            else // error, incorrect usage of authFlag binding input  
                throw new ArgumentException("Incorrect usage of authFlag binding input: Choose \"msi\" or \"user\" ");
        }
        catch (Exception exp) // Overall catch statement 
        {
            throw new ArgumentException($"{exp}");
        }

    }

}
