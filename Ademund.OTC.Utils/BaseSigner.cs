using System.Net.Http;
using System.Threading.Tasks;

namespace Ademund.OTC.Utils
{
    public abstract class BaseSigner : ISigner
    {
        protected const string HeaderAuthorization = "Authorization";

        public string Key { get; init; }
        public string Secret { get; init; }
        public string Region { get; init; } = string.Empty;
        public string Service { get; init; } = string.Empty;

        protected BaseSigner() { }
        protected BaseSigner(string key, string secret, string region = null, string service = null)
        {
            Key = key;
            Secret = secret;
            Region = region ?? string.Empty;
            Service = service ?? string.Empty;
        }

        public abstract void Sign(HttpRequestMessage request, bool forceReSign = false);
        public abstract Task SignAsync(HttpRequestMessage request, bool forceReSign = false);
    }
}
