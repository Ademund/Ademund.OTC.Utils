using System.Net.Http;
using System.Threading.Tasks;

namespace Ademund.OTC.Utils
{
    public interface ISigner
    {
        string Key { get; init; }
        string Secret { get; init; }
        string Region { get; init; }
        string Service { get; init; }

        void Sign(HttpRequestMessage request, bool forceReSign = false);
        Task SignAsync(HttpRequestMessage request, bool forceReSign = false);
    }
}
