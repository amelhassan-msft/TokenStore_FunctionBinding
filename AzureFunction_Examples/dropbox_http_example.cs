// Example of an Azure Function that uses a TokenStore binding to list a user's dropbox files 
// Nuget Dependencies: Dropbox.Api and Microsoft.NET.Sdk.Functions
// Assembly Dependencies: DLL file for TokenStore binding 
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Dropbox.Api;
using System.Collections.Generic;
using System.Linq;

public static class TestTokenStoreBinding_http
{
    [FunctionName("TestTokenStoreBinding_http")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        ILogger log, [TokenStoreBinding(TokenStore_Name = "sample-token-store", TokenStore_Service = "dropbox",
        TokenStore_TokenName = "sampletoken", TokenStore_Location = "westcentralus", 
        Obj_ID = "Sample obj ID", Tenant_ID = "Sample tenant ID")] TokenBindingOutput tokenbindingoutput) // set input binding parameters
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        var filesList = new List<string>();

        if (!string.IsNullOrEmpty(tokenbindingoutput.outputToken)) // If token is avaliable, list dropbox directories/files
        {
            using (var dbx = new DropboxClient(tokenbindingoutput.outputToken))
            {
                var list = await dbx.Files.ListFolderAsync(string.Empty);

                // show folders then files
                foreach (var item in list.Entries.Where(i => i.IsFolder))
                {
                    filesList.Add($"{item.Name}/");
                }

                foreach (var item in list.Entries.Where(i => i.IsFile))
                {
                    filesList.Add($"{item.Name} \n");
                }
            }
        }
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var file in filesList)
        {
            sb.Append(file);
        }
        return (ActionResult)new OkObjectResult($"Files: \n {sb.ToString()}");
    }
}
