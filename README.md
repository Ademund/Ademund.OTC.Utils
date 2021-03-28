# Ademund.OTC.Utils
C# DotNet Utils for integrating with Open Telekom Cloud

![GitHub Workflow Status](https://img.shields.io/github/workflow/status/Ademund/Ademund.OTC.Utils/.NET)

**This library is very much alhpa at the moment!**

## Message Signing

The OTC API documentation states:

> API requests sent by third-party applications to public cloud services must be authenticated using signatures.

The goal of this library is to simplify the authentication process for anybody trying to use the APIs from dotnet.

Signing requires an AccessKey / SecretyKey pair and your OTC project id.

* [Obtaining Required Information](https://docs.otc.t-systems.com/api/apiug/apig-en-api-180328009.html)

### Simple signing of a request:

```cs
    string accessKey = "ak";
    string secretKey = "sk";
    string projectId = "pid";
    var requestUri = new Uri($"https://vpc.eu-de.otc.t-systems.com/v1/{projectId}/security-groups"));
    
    var signer = new Signer(accessKey, secretKey);
    var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
    requestMessage.Headers.Add("X-Project-Id", projectId);
    await signer.SignAsync(requestMessage);
    
    var httpClient = new HttpClient() {
        BaseAddress = requestUri
    };
    var response = await httpClient.SendAsync(requestMessage);
```

### Using a signing http client handler:

```cs
    string accessKey = "ak";
    string secretKey = "sk";
    string projectId = "pid";
    var requestUri = new Uri($"https://vpc.eu-de.otc.t-systems.com/v1/{projectId}/security-groups"));
    
    var signer = new Signer(accessKey, secretKey);
    var signingClientHandler = new SigningHttpClientHandler(signer);
   
    var httpClient = new HttpClient(signingClientHandler) {
        BaseAddress = requestUri
    };

    var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
    requestMessage.Headers.Add("X-Project-Id", projectId);
    var response = await httpClient.SendAsync(requestMessage);
```
