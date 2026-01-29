public class Paperboy {
    public void collectPayment(Customer customer, double paymentAmount) {
        customer.requestPayment(paymentAmount);
    }
}

public class Customer {
    private String firstName;
    private String lastName;
    private Wallet myWallet;
    
    public String getFirstName() { 
        return firstName; 
    }
    
    public String getLastName() { 
        return lastName; 
    }
    
    public void requestPayment(double amount) {
        if (myWallet.hasEnoughMoney(amount)) {
            myWallet.subtractMoney(amount);
        } else {
           //Come Back Later
        }
    }
}

public class Wallet {
    private float value;

    public float getTotalMoney() {
        return value; 
    }
    public void setTotalMoney(float newValue) {
        value = newValue;
    }
    public boolean hasEnoughMoney(float amount) {
        return value >= amount;
    }
    
    public void subtractMoney(float debit) {
        value -= debit;
    }
}