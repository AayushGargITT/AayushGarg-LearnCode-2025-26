public class WithdrawalResult {

    private final boolean successful;
    private final String  message;

    private WithdrawalResult(boolean successful, String message) {
        this.successful = successful;
        this.message    = message;
    }

    public static WithdrawalResult success() {
        return new WithdrawalResult(true, ATMConstants.Messages.WITHDRAWAL_SUCCESS);
    }

    public static WithdrawalResult failure(String reason) {
        return new WithdrawalResult(false, reason);
    }

    public boolean isSuccessful() { return successful; }
    public String  getMessage()   { return message;    }
}