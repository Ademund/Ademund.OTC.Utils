using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace Ademund.OTC.Utils.Tests
{
    public class SignerTests
    {
        [Theory]
        [ClassData(typeof(SignerTestData))]
        public void Specifying_RegionAndService_ShouldGiveConsistentResults(string ak, string sk, string region, string service, string sdkDate, string projectId, string expected)
        {
            var signer = new Signer(ak, sk, region, service);
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri("http://example.com"));
            request.Headers.Set("X-Sdk-Date", sdkDate);
            request.Headers.Set("X-Project-Id", projectId);

            signer.Sign(request);

            var auth = request.Headers.Get("Authorization");
            Assert.Equal(expected, auth);
        }
    }

    public class SignerTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "ak", "sk", null, null, "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101///sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=ec385090522c25504a5b574c7195ac56bf37081d90f74fc093a2676f906cb057" };
            yield return new object[] { "ak", "sk", null, "", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101///sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=ec385090522c25504a5b574c7195ac56bf37081d90f74fc093a2676f906cb057" };
            yield return new object[] { "ak", "sk", "", null, "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101///sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=ec385090522c25504a5b574c7195ac56bf37081d90f74fc093a2676f906cb057" };
            yield return new object[] { "ak", "sk", "", "", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101///sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=ec385090522c25504a5b574c7195ac56bf37081d90f74fc093a2676f906cb057" };
            yield return new object[] { "ak", "sk", null, "service", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101//service/sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=1feb87f00aa9a1ff67e17b7166f9e3f599ae8e134e38817bdf3135efa5317535" };
            yield return new object[] { "ak", "sk", "", "service", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101//service/sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=1feb87f00aa9a1ff67e17b7166f9e3f599ae8e134e38817bdf3135efa5317535" };
            yield return new object[] { "ak", "sk", "region", null, "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101/region//sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=0fc9c2559c07ac70d086fa9bef16f34273b7886b3d1b57f5b5af8e88769f2c8e" };
            yield return new object[] { "ak", "sk", "region", "", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101/region//sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=0fc9c2559c07ac70d086fa9bef16f34273b7886b3d1b57f5b5af8e88769f2c8e" };
            yield return new object[] { "ak", "sk", "region", "service", "19700101T000001Z", "project", "SDK-HMAC-SHA256 Credential=ak/19700101/region/service/sdk_request, SignedHeaders=host;x-project-id;x-sdk-date, Signature=8bdb0e44341d71482390afe6536fa5f3ecfa6da7956ee95d1f8a60b7ab871240" };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
