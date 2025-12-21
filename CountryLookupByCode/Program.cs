using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using CountryLookupByCode.Services;
using CountryLookupByCode.Constants;

namespace CountryLookupByCode
{
    public class Program
    {
        static async Task Main()
        {
            Console.Write(Messages.EnterCountryCode);
            string countryCode = Console.ReadLine().Trim().ToUpper();

            if (string.IsNullOrEmpty(countryCode))
            {
                Console.WriteLine(Messages.InvalidCountryCode);
                return;
            }

            CountryService service = new CountryService();
            var country =await service.GetCountryAsync(countryCode);

            if (country != null)
            {
                Console.WriteLine(Messages.CountryNamePrefix + country.Name.Common);
                var neighbors = await service.GetNeighborNamesAsync(country.Borders);
                if (neighbors.Count > 0)
                    Console.WriteLine(Messages.NeighbouringCountriesPrefix + string.Join(", ", neighbors));
                else
                    Console.WriteLine(Messages.NeighbouringNotFound);
            }
            else
                Console.WriteLine(Messages.CountryNotFound);
        }
    }
}
