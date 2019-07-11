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

[Extension("TokenStoreTest")]
public class TokenStoreBinding_ExtensionProvider : IExtensionConfigProvider
{

    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreBindingAttribute>();
        rule.BindToInput<String>(BuildItemFromAttribute); // was TokenBindingOutput
    }

    private async Task<String> BuildItemFromAttribute(TokenStoreBindingAttribute arg, ValueBindingContext arg2)
    {

        // Get token ...
        string tokenStoreResource = "https://tokenstore.azure.net"; // Note: Changed to "https://{token-store-name}.tokenstore.azure.net (extract from the url???) 
        string tokenResourceUrl = arg.Token_url; // Note: The resource url no longer contains the location 

        var outputToken = "NULL";

        try
        {
            string IDToken = "NULL";
            // Extract tenant and object ID 
            var tenantID = "empty";
            var objectID = "empty";

            if (arg.Auth_flag.ToLower() == "user") // If Flag = "USER" or "user" 
            {
                // Get user's AAD ID token from the header 
                StringValues headerValues;
                if (arg.Req.Headers.TryGetValue("X-MS-TOKEN-AAD-ID-TOKEN", out headerValues))
                    IDToken = headerValues.FirstOrDefault();

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
                tokenResourceUrl = $"{arg.Token_url}/tokens/{objectID}"; // uriToken, FIX {tenantID}-{objectID} (too long) 
            }

            // Shared logic for If Flag = "msi" or "user"
            if (arg.Auth_flag.ToLower() == "msi" || arg.Auth_flag.ToLower() == "user")
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource); // Get a token to access Token Store

                // Get token from Token Store
                var request = new HttpRequestMessage(HttpMethod.Get, tokenResourceUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
                HttpClient client = new HttpClient();
                var response = await client.SendAsync(request);
                var serviceApiToken = await response.Content.ReadAsStringAsync();

                // Get access token for client to retrieve files 
                var tokenStoreToken = JsonConvert.DeserializeObject<Token>(serviceApiToken);
                outputToken = tokenStoreToken.Value.AccessToken;

            }
            else // error, incorrect flag usage 
                throw new ArgumentException("Incorrect usage of Auth_flag binding input: Choose \"msi\" or \"user\" ");
        }
        catch (Exception exp)
        {
            throw new ArgumentException($"{exp}");
        }

        // Error: ouputToken is still marked NULL 
        if (outputToken == "NULL" || string.IsNullOrEmpty(outputToken))
            throw new ArgumentException("Retrieved token is NULL - Ensure that this token exists and app permissions are enabled");

    /*var output = new TokenBindingOutput // use for more detailed type 
    {
        outputToken = outputToken
    };*/

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