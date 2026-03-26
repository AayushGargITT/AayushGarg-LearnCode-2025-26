using System.Text.Json;

namespace GeoApp.Boundaries
{
    public interface IGeocodingService
    {
        Task<List<Location>> GetCoordinatesAsync(string locationName);
    }
}