public class Paperboy {
    public void collectPayment(Customer customer, double paymentAmount) {
        boolean success = customer.requestPayment(paymentAmount);

        if (!success) {
            System.out.println("Customer has Insufficient Balance");
        }
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
    
    public boolean requestPayment(double amount) {
        return myWallet.withdraw(amount);
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

    public void withdraw(double amount){
        if (myWallet.hasEnoughMoney(amount)) {
            myWallet.subtractMoney(amount);
            return true;
        }
        return false;
    }
}