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

[Extension("TokenStoreTest")]
public class TokenStoreBinding_ExtensionProvider : IExtensionConfigProvider
{

    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreBindingAttribute>();
        rule.BindToInput<String>(BuildItemFromAttribute); // <{output type of binding}>
    }

    private async Task<String> BuildItemFromAttribute(TokenStoreBindingAttribute arg, ValueBindingContext arg2)
    {
        // Extract resource url from provided url path to token 
        Uri tokenURI = new Uri(arg.Token_url);
        string path = tokenURI.Authority;
        string tokenStoreResource = $"https://{tokenURI.Authority}"; // Format: "https://{token-store-name}.tokenstore.azure.net"
        string tokenResourceUrl = arg.Token_url; 

        string outputToken = null;

        try
        {
            string IDToken = null;
            // Extract tenant and object ID 
            string tenantID = null;
            string objectID = null;

            if (arg.Auth_flag.ToLower() == "user") // If Flag = "USER" or "user" 
            {
                IDToken = arg.EasyAuthAccessToken; // this is an AAD ID header token 
                JwtSecurityTokenHandler JwtHandler = new JwtSecurityTokenHandler();
                if (!JwtHandler.CanReadToken(IDToken)) 
                    throw new ArgumentException("ID token cannot be read as JWT");

                var securityToken = JwtHandler.ReadJwtToken(IDToken);
                var payload = securityToken.Payload; // extract payload data 

                foreach (Claim claim in payload.Claims)
                {
                    if (claim.Type == "tid")
                        tenantID = claim.Value;
                    if (claim.Type == "oid")
                        objectID = claim.Value;
                }

                if (tenantID == null)
                    throw new ArgumentException("TenantID cannot be read");
                else if (objectID == null)
                    throw new ArgumentException("ObjectID cannot be read");
                else
                    tokenResourceUrl = $"{arg.Token_url}/tokens/{objectID}"; // uriToken, TODO: Naming convention should be {tenantID}-{objectID} (currently too long)
            }

            // Shared logic for If Auth_Flag = "msi" or "user"
            if (arg.Auth_flag.ToLower() == "msi" || arg.Auth_flag.ToLower() == "user")
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource); // Get a token to access Token Store

                // Get token from Token Store
                var request = new HttpRequestMessage(HttpMethod.Get, tokenResourceUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.SendAsync(request);

                Token tokenStoreToken;
                if (response.IsSuccessStatusCode)
                {
                    var serviceApiToken = await response.Content.ReadAsStringAsync();

                    // Get access token for client to retrieve files 
                    tokenStoreToken = JsonConvert.DeserializeObject<Token>(serviceApiToken);

                    try
                    {
                        outputToken = tokenStoreToken.Value.AccessToken;
                    }
                        
                    catch
                    {
                        throw new ArgumentException($"Token Store toke status message: {tokenStoreToken.Status.Error.Message}");
                    }
                }
                else
                    throw new ArgumentException($"Http response message status code: {response.StatusCode.ToString()}"); 
                }
            else // error, incorrect flag usage 
                throw new ArgumentException("Incorrect usage of Auth_flag binding input: Choose \"msi\" or \"user\" ");
        }
        catch (Exception exp)
        {
            throw new ArgumentException($"{exp}");
        }

        // Error: ouputToken is still marked NULL 
        if (outputToken == null)
            throw new ArgumentException("Retrieved token is NULL - Ensure that this token exists and is logged in");

        return outputToken;
    }
}

// Token json object definition 
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