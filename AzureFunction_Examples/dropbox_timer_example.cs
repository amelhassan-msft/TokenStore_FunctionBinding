using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Dropbox.Api;
using System.Linq;

public static class TestTokenStoreBinding_timer
{
    [FunctionName("TestTokenStoreBinding_timer")]
    public static async void Run([TimerTrigger("*/1 * * * * * ")]TimerInfo myTimer, ILogger log,
        [TokenStoreBinding(TokenStore_Name = "sample-token-store", TokenStore_Service = "dropbox", TokenStore_TokenName = "sampletoken",
        TokenStore_Location = "westcentralus", Obj_ID = "Sample obj ID", Tenant_ID = "Sample tenant ID")] TokenBindingOutput tokenbindingoutput) // update binding inputs 
    {
        // timer triggered every second (note: may be slowed down since this is an async method)
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (!string.IsNullOrEmpty(tokenbindingoutput.outputToken))
        {
            using (var dbx = new DropboxClient(tokenbindingoutput.outputToken))
            {
                var list = await dbx.Files.ListFolderAsync(string.Empty);
                

                // show folders then files
                foreach (var item in list.Entries.Where(i => i.IsFolder))
                {
                    log.LogInformation($"Directory: {item.Name}");
                }

                foreach (var item in list.Entries.Where(i => i.IsFile))
                {
                    log.LogInformation($"File: {item.Name}");
                }
            }
        }
    }
}
