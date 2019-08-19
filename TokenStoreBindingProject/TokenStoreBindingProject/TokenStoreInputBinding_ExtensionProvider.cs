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
