using System.Text.Json;
using System.Models;
using System.Security.Cryptography.X509Certificates;

namespace GeoApp.Boundaries
{

    public class GoogleGeocodingAdapter : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public GoogleGeocodingAdapter(HttpClient httpClient, string apiKey, string baseUrl)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _baseUrl = baseUrl;
       }

       public async Task<List<Location>> GetCoordinatesAsync(string locationName)
        {
            var requestUrl = $"{_baseUrl}?address={encodedAddress}&key={_apiKey}";
            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to call Geocoding API");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var results = new List<Location>();
            foreach (var element in doc.RootElement.GetProperty("results").EnumerateArray())
            {
                results.Add(new Location
                {
                    Name = element.GetProperty("formatted_address").GetString(),
                    Latitude = element.GetProperty("geometry").GetProperty("location").GetProperty("lat").GetDouble(),
                    Longitude = element.GetProperty("geometry").GetProperty("location").GetProperty("lng").GetDouble()
                });
            }
            return results;


        }

    }
}