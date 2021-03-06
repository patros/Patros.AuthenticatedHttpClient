﻿using System.Net.Http;

namespace CoderPatros.AuthenticatedHttpClient
{
    // implementation shamelessly ripped off from the Azure sample
    // https://github.com/Azure-Samples/active-directory-dotnet-daemon/blob/master/TodoListDaemon/Program.cs
    public static class AzureAdAuthenticatedHttpClient
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
        public static HttpClient GetClient(AzureAdAuthenticatedHttpClientOptions options)
        {
            var msgHandler = new AzureAdAuthenticatedHttpMessageHandler(options);
            return new HttpClient(msgHandler);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
        public static HttpClient GetClient(AzureAdAuthenticatedHttpClientOptions options, HttpMessageHandler innerHandler)
        {
            var msgHandler = new AzureAdAuthenticatedHttpMessageHandler(options, innerHandler);
            return new HttpClient(msgHandler);
        }
    }
}