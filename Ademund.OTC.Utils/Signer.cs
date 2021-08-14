using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Ademund.OTC.Utils
{
    public class Signer : BaseSigner
    {
        private const string BasicDateFormat = "yyyyMMddTHHmmssZ";
        private const string ShortDateFormat = "yyyyMMdd";
        private const string Algorithm = "SDK-HMAC-SHA256";
        private const string RequestType = "sdk_request";
        private const string HeaderXDate = "X-Sdk-Date";
        private const string HeaderHost = "Host";
        private const string HeaderContentSha256 = "X-Sdk-Content-Sha256";
        private const string EmptyContentHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        private readonly HashSet<string> _unsignedHeaders = new() { "content-type" };

        public Signer() { }
        public Signer(string key, string secret, string region = null, string service = null) : base(key, secret, region, service) { }

        public override void Sign(HttpRequestMessage request, bool forceReSign = false)
        {
            // if the request has already been signed
            // do not attempt to sign it again unless forceReSign is set to true
            if (request.Headers.Contains(HeaderAuthorization)
                && (request.Headers.Get(HeaderAuthorization)?.StartsWith(Algorithm) == true)
                && !forceReSign)
            {
                return;
            }

            var (basicDate, shortDate) = ProcessHeaderXDate(request);
            ProcessHeaderHost(request);
            string canonicalRequest = ConstructCanonicalRequest(request);
            ProcessCanonicalRequest(request, canonicalRequest, basicDate, shortDate);
        }

        public override async Task SignAsync(HttpRequestMessage request, bool forceReSign = false)
        {
            // if the request has already been signed
            // do not attempt to sign it again unless forceReSign is set to true
            if (request.Headers.Contains(HeaderAuthorization)
                && (request.Headers.Get(HeaderAuthorization)?.StartsWith(Algorithm) == true)
                && !forceReSign)
            {
                return;
            }

            var (basicDate, shortDate) = ProcessHeaderXDate(request);
            ProcessHeaderHost(request);
            string canonicalRequest = await ConstructCanonicalRequestAsync(request).ConfigureAwait(false);
            ProcessCanonicalRequest(request, canonicalRequest, basicDate, shortDate);
        }

        private (string basicDate, string shortDate) ProcessHeaderXDate(HttpRequestMessage request)
        {
            if (!DateTime.TryParseExact(request.Headers.Get(HeaderXDate), BasicDateFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out DateTime t))
            {
                t = DateTime.Now;
                request.Headers.Set(HeaderXDate, t.ToUniversalTime().ToString(BasicDateFormat));
            }
            string basicDate = t.ToUniversalTime().ToString(BasicDateFormat);
            string shortDate = t.ToUniversalTime().ToString(ShortDateFormat);
            return (basicDate, shortDate);
        }

        private void ProcessHeaderHost(HttpRequestMessage request)
        {
            request.Headers.Set(HeaderHost, request.Headers.Get(HeaderHost) ?? request.RequestUri.Host);
        }

        private void ProcessCanonicalRequest(HttpRequestMessage request, string canonicalRequest, string basicDate, string shortDate)
        {
            List<string> signedHeaders = ProcessSignedHeaders(request);
            string stringToSign = StringToSign(canonicalRequest, basicDate, shortDate);
            string signature = SignStringToSign(stringToSign, GetSigningKey(shortDate));
            string authValue = ProcessAuthHeader(signature, signedHeaders, shortDate);
            request.Headers.TryAddWithoutValidation(HeaderAuthorization, authValue);
        }

        /// <summary>
        /// Build a CanonicalRequest from a regular request string
        /// CanonicalRequest consists of several parts:
        ///   Part 1. HTTPRequestMethod
        ///   Part 2. CanonicalURI
        ///   Part 3. CanonicalQueryString
        ///   Part 4. CanonicalHeaders
        ///   Part 5 SignedHeaders
        ///   Part 6 HexEncode(Hash(RequestPayload))
        /// </summary>
        private string ConstructCanonicalRequest(HttpRequestMessage request)
        {
            return $"{ProcessRequestParameters(request)}\n" +
                   $"{ProcessRequestPayload(request)}";
        }

        private async Task<string> ConstructCanonicalRequestAsync(HttpRequestMessage request)
        {
            return $"{ProcessRequestParameters(request)}\n" +
                   $"{await ProcessRequestPayloadAsync(request).ConfigureAwait(false)}";
        }

        private string ProcessRequestParameters(HttpRequestMessage request)
        {
            return $"{ProcessRequestMethod(request)}\n" +
                   $"{ProcessCanonicalUri(request)}\n" +
                   $"{ProcessCanonicalQueryString(request)}\n" +
                   $"{ProcessCanonicalHeaders(request)}\n" +
                   $"{string.Join(";", ProcessSignedHeaders(request))}";
        }

        private string ProcessRequestMethod(HttpRequestMessage request)
        {
            return request.Method.Method;
        }

        private string ProcessCanonicalUri(HttpRequestMessage request)
        {
            string uri = request.RequestUri.GetComponents(UriComponents.Path | UriComponents.KeepDelimiter, UriFormat.Unescaped);
            uri = string.Join("/", uri.Split('/').Select(HttpEncoder.UrlEncode).ToList());
            uri = uri.EndsWith("/") ? uri : uri + "/";
            return uri;
        }

        private string ProcessCanonicalQueryString(HttpRequestMessage request)
        {
            System.Collections.Specialized.NameValueCollection queryParams = HttpUtility.ParseQueryString(request.RequestUri.Query);
            var keys = queryParams.AllKeys.ToList();
            keys.Sort(string.CompareOrdinal);

            var queryStrings = new List<string>();
            foreach (string key in keys)
            {
                string k = HttpEncoder.UrlEncode(key);
                var values = queryParams[key].Split('=').ToList();
                values.Sort(string.CompareOrdinal);
                queryStrings.AddRange(values.Select(value => k + "=" + HttpEncoder.UrlEncode(value)));
            }

            return string.Join("&", queryStrings);
        }

        private string ProcessCanonicalHeaders(HttpRequestMessage request)
        {
            var headers = new List<string>();
            foreach (string key in ProcessSignedHeaders(request))
            {
                var values = new List<string>(request.Headers.GetValues(key));
                values.Sort(string.CompareOrdinal);
                foreach (string value in values)
                {
                    headers.Add(key + ":" + value.Trim());
                    request.Headers.Set(key, Encoding.GetEncoding("iso-8859-1").GetString(Encoding.UTF8.GetBytes(value)));
                }
            }

            return string.Join("\n", headers) + "\n";
        }

        private List<string> ProcessSignedHeaders(HttpRequestMessage request)
        {
            IEnumerable<string> keys = request.Headers.Select(x => x.Key);
            var signedHeaders = (from key in keys
                                 let keyLower = key.ToLower()
                                 where !_unsignedHeaders.Contains(keyLower)
                                 select key.ToLower()).ToList();

            signedHeaders.Sort(string.CompareOrdinal);
            return signedHeaders;
        }

        private string ProcessRequestPayload(HttpRequestMessage request)
        {
            string hexEncodePayload;
            if (request.Headers.Get(HeaderContentSha256) != null)
            {
                hexEncodePayload = request.Headers.Get(HeaderContentSha256);
            }
            else
            {
                if ((request.Method != HttpMethod.Post) && (request.Method != HttpMethod.Put))
                {
                    hexEncodePayload = EmptyContentHash;
                }
                else
                {
                    byte[] data = request.Content?.ReadAsByteArrayAsync()
                        .GetAwaiter()
                        .GetResult();
                    hexEncodePayload = HexEncodeSha256Hash(data);
                }
            }

            return hexEncodePayload;
        }

        private async Task<string> ProcessRequestPayloadAsync(HttpRequestMessage request)
        {
            string hexEncodePayload;
            if (request.Headers.Get(HeaderContentSha256) != null)
            {
                hexEncodePayload = request.Headers.Get(HeaderContentSha256);
            }
            else
            {
                if ((request.Method != HttpMethod.Post) && (request.Method != HttpMethod.Put))
                {
                    hexEncodePayload = EmptyContentHash;
                }
                else
                {
                    byte[] data = await (request.Content?.ReadAsByteArrayAsync()).ConfigureAwait(false);
                    hexEncodePayload = HexEncodeSha256Hash(data);
                }
            }

            return hexEncodePayload;
        }

        private string ProcessAuthHeader(string signature, List<string> signedHeaders, string shortDate)
        {
            return $"{Algorithm} Credential={Key}/{shortDate}/{Region}/{Service}/{RequestType}, SignedHeaders={string.Join(";", signedHeaders)}, Signature={signature}";
        }

        private static string HexEncodeSha256Hash(byte[] body)
        {
            SHA256 sha256 = new SHA256Managed();
            byte[] bytes = sha256.ComputeHash(body);
            sha256.Clear();
            return ToHexString(bytes);
        }

        private static string ToHexString(byte[] value)
        {
            int num = value.Length * 2;
            char[] array = new char[num];
            int num2 = 0;
            for (int i = 0; i < num; i += 2)
            {
                byte b = value[num2++];
                array[i] = GetHexValue(b / 16);
                array[i + 1] = GetHexValue(b % 16);
            }

            return new string(array, 0, num);
        }

        private static char GetHexValue(int i)
        {
            if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'a');
        }

        private string StringToSign(string canonicalRequest, string basicDate, string shortDate)
        {
            SHA256 sha256 = new SHA256Managed();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));
            sha256.Clear();
            return $"{Algorithm}\n" +
                   $"{basicDate}\n" +
                   $"{shortDate}/{Region}/{Service}/{RequestType}\n" +
                   $"{ToHexString(bytes)}";
        }

        private byte[] GetSigningKey(string shortDate)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes($"SDK{Secret}");
            byte[] kDate = HMacSha256(kSecret, shortDate);
            byte[] kRegion = HMacSha256(kDate, Region);
            byte[] kService = HMacSha256(kRegion, Service);

            return HMacSha256(kService, RequestType);
        }

        private string SignStringToSign(string stringToSign, byte[] signingKey)
        {
            byte[] hm = HMacSha256(signingKey, stringToSign);
            return ToHexString(hm);
        }

        private byte[] HMacSha256(byte[] keyByte, string message)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hMacSha256 = new HMACSHA256(keyByte))
            {
                return hMacSha256.ComputeHash(messageBytes);
            }
        }
    }
}
