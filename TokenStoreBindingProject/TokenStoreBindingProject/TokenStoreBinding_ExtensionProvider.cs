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
using System.Linq;
using Microsoft.Extensions.Primitives;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text;

[Extension("TokenStoreTest")]
public class TokenStoreBinding_ExtensionProvider : IExtensionConfigProvider
{
    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreBindingAttribute>();
        rule.BindToInput<String>(BuildItemFromAttribute); // <{output type of binding}>
    }

    // ******************************************** Token Store Helper Functions ********************************************************
    public async Task<String> get_or_create_token(string tokenResourceUrl, string tokenStoreResource)
    {
        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource); // Get a token to access Token Store

        // Get token from Token Store
        var request = new HttpRequestMessage(HttpMethod.Get, tokenResourceUrl);
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
            create_token(tokenResourceUrl, tokenStoreApiToken, tokenStoreResource);

        return null; // output token could not be accessed 
    }
    public async void create_token(string tokenResourceUrl, string tokenStoreApiToken, string tokenStoreResource)
    {
        HttpClient client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Put, tokenResourceUrl);
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
            throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}. MSI does not have \"create\" permission for the following token store: {tokenStoreResource}. Further details: {responseStr}");
        
        //tokenStoreToken = JsonConvert.DeserializeObject<Token>(responseStr);
        //outputToken = tokenStoreToken.Value.AccessToken;
        // would it be possible to redirect user to something like this: https://ameltokenstore.tokenstore.azure.net/services/dropbox/tokens/a86c35dd-68a5-4668-9fae-2e7743c4feef/login
    }

    // ***************************************************************************************************************************************************
    // ***************************** Functions to deal with headers and extract token name based on ID Provider ******************************************
    public string get_aad_tokenpath(JwtPayload header_payload, TokenStoreBindingAttribute arg) // Token name based on tenant and object IDs 
    {
        string tenantID = null;
        string objectID = null;

        foreach (Claim claim in header_payload.Claims)
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
            return $"{arg.Token_url}/tokens/{objectID}"; // uriToken, TODO: Naming convention should be {tenantID}-{objectID} (currently too long)
    }

    public string get_facebook_tokenpath(JwtPayload header_payload, TokenStoreBindingAttribute arg)
    {
        // Token name based on user name 
        return "NULL";
    }

    public string get_google_tokenpath(JwtPayload header_payload, TokenStoreBindingAttribute arg) // Token name based on sub (i.e. the google user id) 
    {
        string user_id = null;

        foreach (Claim claim in header_payload.Claims)
        {
            if (claim.Type == "sub")
                user_id = claim.Value;
        }
        return $"{arg.Token_url}/tokens/{user_id}";
    }

    // ********************************************************************************************************************

    // ******************************************* Main Function ***********************************************************
    private async Task<String> BuildItemFromAttribute(TokenStoreBindingAttribute arg, ValueBindingContext arg2)
    {
        // Extract resource url from provided url path to token 
        Uri tokenURI = new Uri(arg.Token_url);
        string path = tokenURI.Authority;
        string tokenStoreResource = $"https://{tokenURI.Authority}"; // Format: "https://{token-store-name}.tokenstore.azure.net"
        string tokenResourceUrl = arg.Token_url; // for user scienario only specify path up to service 

        try
        {
            if (arg.Auth_flag.ToLower() == "user") // If Flag = "USER" or "user" 
            {
                string IDToken = arg.EasyAuthAccessToken; // access or ID token retrieved from header
                JwtSecurityTokenHandler JwtHandler = new JwtSecurityTokenHandler();
                if (!JwtHandler.CanReadToken(IDToken)) 
                    throw new ArgumentException("ID token cannot be read as JWT");

                var securityToken = JwtHandler.ReadJwtToken(IDToken);
                var payload = securityToken.Payload; // extract payload data 

                // Build path to token 
                switch (arg.Identity_provider) // TODO: Enums should not be case sensitive
                {
                    case ID_Providers.aad:
                        tokenResourceUrl = get_aad_tokenpath(payload, arg);
                        break;
                    case ID_Providers.facebook:
                        tokenResourceUrl = get_facebook_tokenpath(payload, arg);
                        break;
                    case ID_Providers.google:
                        tokenResourceUrl = get_google_tokenpath(payload, arg);
                        break;
                    default:
                        throw new InvalidOperationException("Incorrect usage of Identity_provider parameter. Input must be of type ID_providers enum which currently supports aad, facebook, and google");
                }
            }

            // Shared logic for If Auth_Flag = "msi" or "user"
            if (arg.Auth_flag.ToLower() == "msi" || arg.Auth_flag.ToLower() == "user")
            {
                return await get_or_create_token(tokenResourceUrl, tokenStoreResource);
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

// *********************** Token json object definition ****************************************
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