public class InsufficientFundsException extends Exception {

    public InsufficientFundsException(String accountId, double requested, double available) {
        super(String.format(
            ATMConstants.ExceptionMessages.INSUFFICIENT_FUNDS_FORMAT,
            accountId, requested, available
        ));
    }
}