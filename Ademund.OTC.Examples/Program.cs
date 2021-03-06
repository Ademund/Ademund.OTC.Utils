using Ademund.OTC.Examples.Config;
using Ademund.OTC.Utils;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Ademund.OTC.Examples
{
    class Program
    {
        // dotnet user-secrets init
        // dotnet user-secrets set "Examples:AccessKey" "ak"
        // dotnet user-secrets set "Examples:SecretKey" "sk"
        // dotnet user-secrets set "Examples:ProjectId" "pid"

        static async Task Main(string[] args)
        {
            string env = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            builder.AddJsonFile($"appsettings.{env}.json", optional: true);

            IConfigurationRoot configuration = builder.Build();
            var config = configuration.GetSection("Examples").Get<ExamplesConfig>();

            Console.WriteLine("Config Params: ");
            Console.WriteLine($" - AccessKey: {config.AccessKey}");
            Console.WriteLine($" - ProjectId: {config.ProjectId}");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("Choose Example Request: ");
                int choice = 0;
                foreach (var x in config.Examples)
                {
                    Console.WriteLine($" {choice++} - {x.Name}");
                }
                Console.WriteLine(" x - Exit");
                Console.Write("Enter Example Number: ");

                if (!int.TryParse(Console.ReadLine(), out choice))
                    break;

                var example = config.Examples[choice];
                Console.WriteLine($" - Region: {example.Region}");
                Console.WriteLine($" - Service: {example.Service}");
                Console.WriteLine($" - RequestUri: {example.RequestUri}");
                Console.WriteLine();

                var requestUri = new Uri(example.RequestUri.Replace("/{project_id}/", $"/{config.ProjectId}/"));
                ISigner signer = example.Service == "obs" ?
                    new AWSSigner(config.AccessKey, config.SecretKey, example.Region, "s3") :
                    new Signer(config.AccessKey, config.SecretKey, example.Region, example.Service);

                var signingClientHandler = new SigningHttpClientHandler(signer) { Proxy = new WebProxy(config.ProxyAddress), UseProxy = config.UseProxy };
                var httpClient = new HttpClient(signingClientHandler) {
                    BaseAddress = requestUri
                };

                var requestMessage = new HttpRequestMessage(new HttpMethod(example.Method), requestUri);
                requestMessage.Headers.Add("X-Project-Id", config.ProjectId);

                var response = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
                string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                //string jsonString = JToken.Parse(responseString).ToString(Formatting.Indented);

                Console.WriteLine("Request Headers: ");
                foreach (var header in requestMessage.Headers)
                {
                    Console.WriteLine($" - {header.Key}: {string.Join(",", header.Value)}");
                }
                Console.WriteLine();

                Console.WriteLine("Response: ");
                Console.WriteLine(responseString);
                Console.WriteLine();

                Console.WriteLine("Press any key to continue");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }
}
