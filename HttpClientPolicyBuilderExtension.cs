using System;
using Microsoft.Extensions.DependencyInjection;

namespace DataIntegration.HttpPolicies
{
    public static class HttpClientPolicyBuilderExtension
    {
        public static IHttpClientBuilder ForwardPolicyHandlers(this IHttpClientBuilder builder)
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            var httpPolicyHandler = new HttpClientPolicyHandler();
            builder.Services.AddPolicyRegistry(httpPolicyHandler.HttpPolicyRegistry);
            builder.AddPolicyHandlerFromRegistry(httpPolicyHandler.PolicySelector);
            return builder;
        }
    }
}