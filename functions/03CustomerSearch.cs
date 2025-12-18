public

    class CustomerSearch {
  // Search customer by Country
   public List<Customer> SearchByCountry(string country) {
    var query = from c in
                    db.customers
                where c.Country.Contains(country)
  orderby c.CustomerID ascending select c;

    return query.ToList();
  }

  // Search customer by companyname
       public List<Customer> SearchByCompanyName(string company) {
    var query = from c in
                    db.customers
                where c.CompanyName.Contains(company)
  orderby c.CustomerID ascending select c;

    return query.ToList();
  }

  // Search customer by contact person
 public List<Customer> SearchByContact(string contact) {
    var query = from c in
                    db.customers
                where c.Contact.Contains(contact)
  orderby c.CustomerID ascending select c;

    return query.ToList();
  }

}

public class CustomerCsvExporter
{
  public string Export(List<Customer> customer) {
    StringBuilder sb = new StringBuilder();

    foreach (var customer in customers) {
      sb.AppendFormat("{0},{1}, {2}, {3}", customer.CustomerID, customer.CompanyName,
                      customer.ContactName, customer.Country);
      sb.AppendLine();
    }

    return sb.ToString();
  }
}