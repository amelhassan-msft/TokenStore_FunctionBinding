using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config; // must include for IExtensuibConfigProvider 
//using Microsoft.WindowsAzure.Mobile.Service.Config; // to use, install nuget package Microsoft.WindowsAzure.Mobile.Service.ResourceBroker 

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
        string tokenStoreResource = "https://tokenstore.azure.net";
        // update the below with your resource URL
        string tokenResourceUrl = $"https://{arg.TokenStore_Name}.westcentralus.tokenstore.azure.net/services/{arg.TokenStore_Service}/tokens/{arg.TokenStore_TokenName}"; // Add variable location? i.e. westcentralus 

        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        // Get a token to access Token Store

        string tokenStoreApiToken = await azureServiceTokenProvider.GetAccessTokenAsync(tokenStoreResource);

        // Get Dropbox token from Token Vault
        var request = new HttpRequestMessage(HttpMethod.Post, $"{tokenResourceUrl}/accesstoken");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenStoreApiToken);
        HttpClient client = new HttpClient();
        var response = await client.SendAsync(request);
        var outputToken = await response.Content.ReadAsStringAsync();

        var output = new TokenBindingOutput
        {
            TokenStore_Name_Out = arg.TokenStore_Name,
            TokenStore_Service_Out = arg.TokenStore_Service,
            TokenStore_TokenName_Out = arg.TokenStore_TokenName,
            Obj_ID_Out = arg.Obj_ID,
            Tenant_ID_Out = arg.Tenant_ID,
            outputToken = outputToken
        };

        return output;
    }
}