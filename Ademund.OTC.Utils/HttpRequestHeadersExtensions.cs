using System.Linq;
using System.Net.Http.Headers;

namespace Ademund.OTC.Utils
{
    public static class HttpRequestHeadersExtensions
    {
        public static void Set(this HttpRequestHeaders headers, string name, string value)
        {
            if (headers.Contains(name)) headers.Remove(name);
            headers.Add(name, value);
        }

        public static string Get(this HttpRequestHeaders headers, string name)
        {
            return headers.Contains(name) ? headers.GetValues(name).FirstOrDefault() : null;
        }

        public static void Set(this HttpContentHeaders headers, string name, string value)
        {
            if (headers.Contains(name)) headers.Remove(name);
            headers.Add(name, value);
        }

        public static string Get(this HttpContentHeaders headers, string name)
        {
            return headers.Contains(name) ? headers.GetValues(name).FirstOrDefault() : null;
        }
    }
}
