using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ademund.OTC.Utils
{
    public class SigningHttpClientHandler : HttpClientHandler
    {
        private readonly ISigner _signer;

        public SigningHttpClientHandler(ISigner signer)
        {
            _signer = signer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _signer.SignAsync(request).ConfigureAwait(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
