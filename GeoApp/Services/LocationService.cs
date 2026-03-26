using GeoApp.Boundaries;
using GeoApp.Models;
using GeoApp.Utils;

namespace GeocodeApp.Core.Services
{
    public class LocationService
    {
        private readonly IGeocodingService _geocodingService;

        public LocationService(IGeocodingService geocodingService)
        {
            _geocodingService = geocodingService;
        }

        public async Task<List<Location>> GetLocationAsync(string address)
        {
            InputValidator.Validate(address);

            var results = await _geocodingService.GetCoordinatesAsync(address);

            if (results == null || results.Count == 0)
            {
                Console.WriteLine("No locations found");
                return;
            }                

            return results;
        }
    }
}