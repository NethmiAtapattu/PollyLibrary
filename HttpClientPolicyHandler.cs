using System;
using System.Linq;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;

namespace DataIntegration.HttpPolicies
{
    internal class HttpClientPolicyHandler
    {
        public HttpClientPolicyHandler()
        {
            HttpPolicyRegistry = new PolicyRegistry
            {
                {"NoOpPolicy", NoOpPolicy},
                {"WaitAndRetryPolicy", WaitAndRetryPolicy},
                {"CircuitBreakerPolicy", GetCircuitBreakerPolicy()},
                {"CircuitBreakerWithWaitAndRetryPolicy", CircuitBreakerWithWaitAndRetryPolicy}
            };
        }

        public PolicyRegistry HttpPolicyRegistry { get; }

        private IAsyncPolicy<HttpResponseMessage> NoOpPolicy => Policy.NoOpAsync()
            .AsAsyncPolicy<HttpResponseMessage>();

        private IAsyncPolicy<HttpResponseMessage> WaitAndRetryPolicy => Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

       
        private IAsyncPolicy<HttpResponseMessage> CircuitBreakerWithWaitAndRetryPolicy =>
            Policy.WrapAsync(WaitAndRetryPolicy, GetCircuitBreakerPolicy());

        private IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30), OnBreak, OnReset, OnHalfOpen);
        }



        public IAsyncPolicy<HttpResponseMessage> PolicySelector(IReadOnlyPolicyRegistry<string> policyRegistry, HttpRequestMessage httpRequestMessage)
        {
            try
            {
                if (httpRequestMessage.Method == HttpMethod.Get)
                {
                    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("CircuitBreakerWithWaitAndRetryPolicy");
                }
                else if (httpRequestMessage.Method == HttpMethod.Post && httpRequestMessage.Method == HttpMethod.Put)
                {
                    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("CircuitBreakerWithWaitAndRetryPolicy");
                }
                else
                {
                    return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("NoOpPolicy");
                }
            }
            catch (ArgumentNullException)
            {
                return policyRegistry.Get<IAsyncPolicy<HttpResponseMessage>>("NoOpPolicy");
            }

        }

        private static void OnReset()
        {
            Console.WriteLine("RESET");
            Console.WriteLine();
        }

        private static void OnHalfOpen()
        {
            Console.WriteLine("HALFOPEN");
            Console.WriteLine();
        }

        private static void OnBreak(DelegateResult<HttpResponseMessage> arg1, TimeSpan arg2)
        {
            Console.WriteLine("BREAK");
            Console.WriteLine();
        }
    }
}