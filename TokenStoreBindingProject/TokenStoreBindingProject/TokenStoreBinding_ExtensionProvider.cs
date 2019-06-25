using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config; // must include for IExtensionConfigProvider 
using Newtonsoft.Json;

[Extension("TokenStoreTest")]
public class TokenStoreBinding_ExtensionProvider : IExtensionConfigProvider
{

    public void Initialize(ExtensionConfigContext context)
    {
        var rule = context.AddBindingRule<TokenStoreBindingAttribute>();
        rule.BindToInput<TokenBindingOutput>(BuildItemFromAttribute);
    }

    private async Task<TokenBindingOutput> BuildItemFromAttribute(TokenStoreBindingAttribute arg, ValueBindingContext arg2)
    {

        // Get token ...
        string tokenStoreResource = "https://tokenstore.azure.net"; // Note: Will change soon 
        string tokenResourceUrl = arg.Token_url;

        var outputToken = "NULL"; 
        
        // If Flag = "MSI" or "msi" 
        if (arg.Auth_flag.ToLower() == "msi")
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
        else if (arg.Auth_flag.ToLower() == "user") // If Flag = "USER" or "user" 
        {
            // Code for getting token based on user credentials 
        }
        else // error, incorrect flag usage 
            throw new ArgumentException("Incorrect usage of Auth_flag binding input: Choose \"msi\" or \"user\" ");

        // Error: ouputToken is still marked NULL 
        if (outputToken == "NULL" || string.IsNullOrEmpty(outputToken))
            throw new ArgumentException("Retrieved token is NULL - Ensure that this token exists and app permissions are enabled");

        var output = new TokenBindingOutput
        {
            outputToken = outputToken
        };

        return output;
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