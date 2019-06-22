using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using MyFirstCustomBindingLibrary;
using System;

[assembly: WebJobsStartup(typeof(TokenStoreBindingStartUp))] // must be included otherwise, binding will not be resolved  
namespace MyFirstCustomBindingLibrary
{
    class TokenStoreBindingStartUp : IWebJobsStartup // startup class
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddTokenStoreBinding();
        }

    }

}