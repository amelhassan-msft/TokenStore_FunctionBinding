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

[Extension("TokenStoreTest")]
public class TokenStoreBinding_ExtensionProvider : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreBindingAttribute>();
        rule.BindToInput<String>(BuildItemFromAttribute); // <{output type of binding}>
    }

    // ******************************************** Token Store Helper Functions ********************************************************

    /// <summary>
    /// Get a token if it exists, otherwise create a token and prompt user to navigate to Token Store to login.  
    /// </summary>
    public async Task<String> get_or_create_token(string tokenResourceUrl, string tokenStoreResource)
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
                throw(await create_token(tokenResourceUrl, tokenStoreApiToken, tokenStoreResource));
        }
    }

    /// <summary>
    /// Create a token either with specified token name or with the user's ID. Prompt user to navigate to Token Store to authenticate the token. 
    /// </summary>
    public async Task<Exception> create_token(string tokenResourceUrl, string tokenStoreApiToken, string tokenStoreResource)
    {
        HttpClient client = new HttpClient();
        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Put, tokenResourceUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);

        // Get token name based on url 
        int pos = tokenResourceUrl.LastIndexOf("/") + 1;
        var tokenId = tokenResourceUrl.Substring(pos, tokenResourceUrl.Length - pos); // TODO: Should the display name differ from the token name ? 
 
        var requestContent = JObject.FromObject(new
        {
            displayName = tokenId
        });

        request.Content = new StringContent(requestContent.ToString(), Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        var responseStr = await response.Content.ReadAsStringAsync(); // Provides details of created token

        if (response.IsSuccessStatusCode) // Token was succesfully created 
            throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. Specified token does not exist. A new token with displayname {tokenId} was created. Navagiate to your Token Store to login. Further details: {responseStr}");
        else  // msi does not have permission to create tokens in this token store 
            throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. MSI does not have \"get\" and/or \"create\" permission for the following token store: {tokenStoreResource}. Further details: {responseStr}");
        
        // tokenStoreToken = JsonConvert.DeserializeObject<Token>(responseStr);
        // outputToken = tokenStoreToken.Value.AccessToken;
        // would it be possible to redirect user to something like this: https://ameltokenstore.tokenstore.azure.net/services/dropbox/tokens/a86c35dd-68a5-4668-9fae-2e7743c4feef/login
    }

    // ***************************************************************************************************************************************************

    // ***************************** Functions to deal with headers and extract token name based on ID Provider ******************************************
    /// <summary>
    /// If Id_provider = "aad", the token name is built from the tenant Id and object Id. 
    /// </summary>
    public string get_aad_tokenpath(string header_token, TokenStoreBindingAttribute attribute) // Token name based on tenant and object IDs 
    {
        JwtSecurityTokenHandler JwtHandler = new JwtSecurityTokenHandler();
        if (!JwtHandler.CanReadToken(header_token))
            throw new ArgumentException("AAD ID token cannot be read as JWT");

        var securityToken = JwtHandler.ReadJwtToken(header_token);
        var payload = securityToken.Payload; // extract payload data 
        string tenantID = null;
        string objectID = null;

        foreach (Claim claim in payload.Claims)
        {
            if (claim.Type == "tid")
                tenantID = claim.Value;
            if (claim.Type == "oid")
                objectID = claim.Value;
        }

        if (tenantID == null)
            throw new ArgumentException("AAD Token Error: TenantID cannot be read");
        else if (objectID == null)
            throw new ArgumentException("AAD Token Error: ObjectID cannot be read");
        else
            return $"{attribute.Token_url}/tokens/{objectID}"; // token uri, TODO: Naming convention should be {tenantID}-{objectID} (currently too long)
    }

    /// <summary>
    /// If Id_provider = "facebook", the token name is built from the facebook id. 
    /// </summary>
    public string get_facebook_tokenpath(string header_token, TokenStoreBindingAttribute attribute)
    {
        try
        {
            var fb = new FacebookClient(header_token);
            var result = (IDictionary<string, object>)fb.Get("/me?fields=id");
            return $"{attribute.Token_url}/tokens/{(string)result["id"]}"; // Token uri 
        }
        catch (FacebookOAuthException)
        {
            throw new ArgumentException("Could not read user id from Facebook access token.");
        }
    }

    /// <summary>
    /// If Id_provider = "google", the token name is built from the sub id. 
    /// </summary>
    public string get_google_tokenpath(string header_token, TokenStoreBindingAttribute attribute) // Token name based on sub (i.e. the google user id) 
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
        }

        if (user_id == null)
            throw new ArgumentException("Could not read user id from Google ID token.");
        return $"{attribute.Token_url}/tokens/{user_id}"; // Token uri 
    }

    // ***********************************************************************************************************************************************

    // ******************************************* MAIN FUNCTION ***********************************************************
    /// <summary>
    /// Main function for Token Store Binding logic. Returns an access token for specified service, or creates a token if it does not exist. 
    /// </summary>
    private async Task<String> BuildItemFromAttribute(TokenStoreBindingAttribute attribute, ValueBindingContext arg2)
    {
        attribute.CheckValidity_URL();
        // Extract resource url from provided url path to token 
        Uri tokenURI = new Uri(attribute.Token_url);
        string path = tokenURI.Authority;
        string tokenStoreResource = $"https://{tokenURI.Authority}"; // Format: "https://{token-store-name}.tokenstore.azure.net"
        string tokenResourceUrl = attribute.Token_url; // for user scienario only specify path up to service, for msi scienario specify path up to token name  

        try
        {
            if (attribute.Auth_flag.ToLower() == "user") // If Flag = "USER" or "user" 
            {
                string header_token = attribute.RequestHeader; // Access or ID token retrieved from header
                // Build path to token 
                switch (attribute.Identity_provider.ToLower()) 
                {
                    case "aad":
                        tokenResourceUrl = get_aad_tokenpath(header_token, attribute);
                        break;
                    case "facebook":
                        tokenResourceUrl = get_facebook_tokenpath(header_token, attribute);
                        break;
                    case "google":
                        tokenResourceUrl = get_google_tokenpath(header_token, attribute);
                        break;
                    default:
                        throw new InvalidOperationException("Incorrect usage of Identity_provider parameter. Input must be of type ID_providers enum which currently supports aad, facebook, and google");
                }
            }

            // Shared logic for If Auth_Flag = "msi" or "user"
            if (attribute.Auth_flag.ToLower() == "msi" || attribute.Auth_flag.ToLower() == "user")
            {
                try
                {
                    return await get_or_create_token(tokenResourceUrl, tokenStoreResource);
                }
                catch(Exception exp)
                {
                    throw new ArgumentException($"{exp}");
                }
            }
            else // error, incorrect usage of Auth_flag binding input  
                throw new ArgumentException("Incorrect usage of Auth_flag binding input: Choose \"msi\" or \"user\" ");
        }
        catch (Exception exp) // Overall catch statement 
        {
            throw new ArgumentException($"{exp}");
        }

    }

}

/// <summary>
/// Token json object definition. 
/// </summary>
public class Token

{
    public string Name { get; set; }

    public string DisplayName { get; set; }

    public string TokenUri { get; set; }

    public string LoginUri { get; set; }

    public TokenValue Value { get; set; }

    public TokenStatus Status { get; set; }

}

public class TokenValue

{
    public string AccessToken { get; set; }

    public int ExpiresIn { get; set; }
}


public class TokenStatus

{
    public string State { get; set; }

    public TokenStatusError Error { get; set; }
}

public class TokenStatusError

{
    public string Code { get; set; }

    public string Message { get; set; }
}