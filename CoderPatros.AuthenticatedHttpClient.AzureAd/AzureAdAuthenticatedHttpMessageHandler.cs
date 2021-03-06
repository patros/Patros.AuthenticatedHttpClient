using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CoderPatros.AuthenticatedHttpClient
{
    // implementation shamelessly ripped off from the Azure sample
    // https://github.com/Azure-Samples/active-directory-dotnet-daemon/blob/master/TodoListDaemon/Program.cs
    public class AzureAdAuthenticatedHttpMessageHandler : DelegatingHandler
    {
        private readonly string _resourceId;
        private readonly AuthenticationContext _authContext;
        private readonly ClientCredential _clientCredential;

        public AzureAdAuthenticatedHttpMessageHandler(AzureAdAuthenticatedHttpClientOptions options)
        {
            Contract.Requires(options != null);

            _resourceId = options.ResourceId;

            var authority = String.Format(CultureInfo.InvariantCulture, options.AadInstance, options.Tenant);
            _authContext = new AuthenticationContext(authority);

            _clientCredential = new ClientCredential(options.ClientId, options.AppKey);
        }

        public AzureAdAuthenticatedHttpMessageHandler(
            AzureAdAuthenticatedHttpClientOptions options, 
            HttpMessageHandler innerHandler) : this(options)
        {
            InnerHandler = innerHandler;
        }

        internal virtual async Task<string> AcquireAccessTokenAsync()
        {
            var result = await _authContext.AcquireTokenAsync(_resourceId, _clientCredential).ConfigureAwait(false);
            return result?.AccessToken;
        }

        private async Task<string> AcquireTokenWithRetriesAsync(CancellationToken cancellationToken)
        {
            //
            // Get an access token from Azure AD using client credentials.
            // If the attempt to get a token fails because the server is unavailable, retry twice after 3 seconds each.
            //
            string token = null;
            int retryCount = 0;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    token = await AcquireAccessTokenAsync().ConfigureAwait(false);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable" && !cancellationToken.IsCancellationRequested)
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    // Console.WriteLine(
                    //     String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                    //     DateTime.Now.ToString(),
                    //     ex.ToString(),
                    //     retry.ToString()));
                }
            }
            while ((retry == true) && (retryCount < 3) && !cancellationToken.IsCancellationRequested);

            return token;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Contract.Requires(request != null);

            var accessToken = await AcquireTokenWithRetriesAsync(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
            
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}