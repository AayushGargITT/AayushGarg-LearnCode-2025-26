using System.Linq.Expressions;

class Program
{
    public static async Task Main(string[] args)
    {
        var input = ReadInput();

        try
        {   
            var config = new ConfigurationManager();
            var httpClient = new HttpClient();

            IGeocodingService service = new GoogleGeocodingAdapter(
                httpClient,
                config.GetGoogleApiKey(),
                config.GetGoogleBaseUrl()
            );
            var locationService = new LocationService(adapter);
            var results = await locationService.GetLocationsAsync(input);

            PrintOutput(results);
        }
        catch (Exception ex)
        {
            PrintError(ex.Message);
        }

    }

    private static string ReadInput()
    {
        Console.WriteLine("Enter location:");
        var input = Console.ReadLine();

        return input;
    }

    private static void PrintOutput(List<Location> locations)
    {
        foreach (var loc in locations)
        {
            Console.WriteLine($"Address: {loc.Name}");
            Console.WriteLine($"Lat: {loc.Latitude}, Lng: {loc.Longitude}");
            Console.WriteLine("----------------------");
        }
    }

    private static void PrintError(string message)
    {
        Console.WriteLine($"Error: {message}");
    }


}