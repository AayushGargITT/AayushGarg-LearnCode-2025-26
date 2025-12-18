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
                where c.Country.Contains(company)
  orderby c.CustomerID ascending select c;

    return query.ToList();
  }

  // Search customer by contact person
 public List<Customer> SearchByContact(string contact) {
    var query = from c in
                    db.customers
                where c.Country.Contains(contact)
  orderby c.CustomerID ascending select c;

    return query.ToList();
  }

  public string ExportToCSV(List<Customer> data) {
    StringBuilder sb = new StringBuilder();

    foreach (var item in data) {
      sb.AppendFormat("{0},{1}, {2}, {3}", item.CustomerID, item.CompanyName,
                      item.ContactName, item.Country);
      sb.AppendLine();
    }

    return sb.ToString();
  }
}