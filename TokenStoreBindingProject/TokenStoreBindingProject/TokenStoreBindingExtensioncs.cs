using Microsoft.Azure.WebJobs;
using System;

namespace MyFirstCustomBindingLibrary
{
    public static class TokenStoreBindingExtension
    {
        public static IWebJobsBuilder AddTokenStoreBinding(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<TokenStoreBinding_ExtensionProvider>();
            return builder;
        }
    }
}
