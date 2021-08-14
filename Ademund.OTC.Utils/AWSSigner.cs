﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Ademund.OTC.Utils
{
    public class AWSSigner : BaseSigner
    {
        private const string Algorithm = "AWS";

        protected AWSSigner()
        {
        }

        protected AWSSigner(string key, string secret, string region = null, string service = null) : base(key, secret, region, service)
        {
        }

        public override void Sign(HttpRequestMessage request, bool forceReSign = false)
        {
            if (request.Headers.Contains(HeaderAuthorization)
                && (request.Headers.Get(HeaderAuthorization)?.StartsWith(Algorithm) == true)
                && !forceReSign)
            {
                return;
            }

            SetAuthorizationHeader(request);
        }

        public override Task SignAsync(HttpRequestMessage request, bool forceReSign = false)
        {
            if (request.Headers.Contains(HeaderAuthorization)
                && (request.Headers.Get(HeaderAuthorization)?.StartsWith(Algorithm) == true)
                && !forceReSign)
            {
                return Task.CompletedTask;
            }

            SetAuthorizationHeader(request);
            return Task.CompletedTask;
        }

        private void SetAuthorizationHeader(HttpRequestMessage request)
        {
            string amzHeaders = "";
            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers.Where(x => x.Key.StartsWith("x-amz-")).OrderBy(x => x.Key).ToList())
            {
                amzHeaders += $"{header.Key}:{header.Value.First()}\n";
            }

            const string contentMD5 = "";
            DateTime date = DateTime.UtcNow;
            string dateString = date.ToString("R");
            string message = $"{request.Method}\n{contentMD5}\n{request.Content.Headers.ContentType}\n{dateString}\n{amzHeaders}{request.RequestUri.AbsoluteUri}";
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            byte[] keyBytes = Encoding.ASCII.GetBytes(Secret);
            var hash = new HMACSHA1(keyBytes);
            byte[] messageHash = hash.ComputeHash(messageBytes);
            string messageHashString = Encoding.ASCII.GetString(messageHash);
            string messageBase64String = Convert.ToBase64String(messageHash);

            request.Headers.Set("Date", dateString);
            request.Headers.TryAddWithoutValidation(HeaderAuthorization, $"{Algorithm} {Key}:{messageBase64String}");
        }
    }
}