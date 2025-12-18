using CountryLookupByCode.Constants;
using CountryLookupByCode.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CountryLookupByCode.Services
{
    public class CountryService
    {
        private readonly HttpClient _httpClient;

        public CountryService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<Country?> GetCountryAsync(string countryCode)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(ApiUrls.RestCountriesBaseUrl+countryCode);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            var countries = JsonSerializer.Deserialize<List<Country>>(json);

            return countries != null && countries.Count>0 ? countries[0] : null;
        }

        public async Task<List<string>> GetNeighborNamesAsync(List<string> borderCodes)
        {
            var neighborNames = new List<string>();
            if (borderCodes == null || borderCodes.Count == 0) return neighborNames;

            foreach (var code in borderCodes)
            {
                var country = await GetCountryAsync(code);
                if (country != null)
                    neighborNames.Add(country.Name.Common);
            }
            return neighborNames;
        }


    }
}
