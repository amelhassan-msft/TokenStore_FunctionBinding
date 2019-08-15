using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using MyFirstCustomBindingLibrary;
using System;

[assembly: WebJobsStartup(typeof(TokenStoreInputBindingStartUp))] // must be included otherwise, binding will not be resolved  
namespace MyFirstCustomBindingLibrary
{
    class TokenStoreInputBindingStartUp : IWebJobsStartup // startup class
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddTokenStoreBinding();
        }

    }

}