using CountryLookupByCode.Services;
using CountryLookupByCode.Constants;
using CountryLookupByCode.Models;

namespace CountryLookupByCode
{
    public class App
    {
        private readonly CountryService _countryService;
        public App(CountryService countryService)
        {
            _countryService = countryService;
        }
        private string? GetCountryCodeFromUser()
    {
        Console.Write(Messages.EnterCountryCode);
        string? countryCode = Console.ReadLine()?.Trim().ToUpper();

        if (string.IsNullOrEmpty(countryCode))
        {
            Console.WriteLine(Messages.InvalidCountryCode);
            return null;
        }

        return countryCode;
    }

    private void DisplayCountryInfo(Country? country,List<string> neighbours)
    {
        
        Console.WriteLine(Messages.CountryNamePrefix + country.Name.Common);

        if (neighbours.Any())
            Console.WriteLine(Messages.NeighbouringCountriesPrefix + string.Join(", ", neighbours));
        else
            Console.WriteLine(Messages.NeighbouringNotFound);
    }
        public async Task RunAsync()
        {
            string countryCode = GetCountryCodeFromUser();
            if (countryCode == null) return;

            var country =await _countryService.GetCountryAsync(countryCode);
            if (country == null)
            {
                Console.WriteLine(Messages.CountryNotFound);
                return;
            }
            var countryBorders=await _countryService.GetNeighborNamesAsync(country.Borders);

            DisplayCountryInfo(country,countryBorders);
        }
    }
}
