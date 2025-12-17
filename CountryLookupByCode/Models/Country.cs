using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountryLookupByCode.Models
{
    public class Country
    {
        public Name Name { get; set; }
        public List<string> Borders { get; set; }
    }
}
