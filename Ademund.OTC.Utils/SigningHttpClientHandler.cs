﻿using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ademund.OTC.Utils
{
    public class SigningHttpClientHandler : HttpClientHandler
    {
        private readonly Signer _signer;

        public SigningHttpClientHandler(Signer signer)
        {
            _signer = signer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _signer.Sign(request);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
