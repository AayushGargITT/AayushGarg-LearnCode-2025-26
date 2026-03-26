namespace GeocodingApp.Config
{
    public class ConfigurationManager
    {
        private readonly IConfiguration _configuration;

        public ConfigurationManager()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public string GetGoogleApiKey()
        {
            var apiKey = _configuration["GoogleGeocoding:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Google API key not configured. Please set it in appsettings.json");
            }
            return apiKey;
        }

        public string GetGoogleBaseUrl()
        {
            var baseURL = _configuration["GoogleGeocoding:BaseUrl"];
            if (string.IsNullOrWhiteSpace(BASEuRL))
            {
                throw new InvalidOperationException(
                    "Google API URL not configured. Please set it in appsettings.json");
            }
            return baseURL;
        }
    }
}