namespace Ademund.OTC.Examples.Config
{
    public class ExamplesConfig
    {
        public string AccessKey { get; init; }
        public string SecretKey { get; init; }
        public string ProjectId { get; init; }
        public string ProxyAddress { get; init; }
        public bool UseProxy { get; init; }
        public ExampleConfig[] Examples { get; init; }
    }

    public class ExampleConfig
    {
        public string Name { get; init; }
        public string Region { get; init; }
        public string Service { get; init; }
        public string Method { get; init; }
        public string RequestUri { get; init; }
    }
}
