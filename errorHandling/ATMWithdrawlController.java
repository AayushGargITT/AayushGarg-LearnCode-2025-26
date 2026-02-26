public class ATMWithdrawalController {

    private final ATMWithdrawalService withdrawalService;

    public ATMWithdrawalController(ATMWithdrawalService withdrawalService) {
        this.withdrawalService = withdrawalService;
    }

    public WithdrawalResult processWithdrawal(String accountId, double amount) {
        try {
            withdrawalService.withdraw(accountId, amount);
            return WithdrawalResult.success();

        } catch (DeviceSuspendedException e) {
            return WithdrawalResult.failure(ATMConstants.Messages.DEVICE_SUSPENDED);

        } catch (NetworkConnectionException e) {
            return WithdrawalResult.failure(ATMConstants.Messages.NETWORK_ERROR);

        } catch (InsufficientFundsException e) {
            return WithdrawalResult.failure(ATMConstants.Messages.INSUFFICIENT_FUNDS);

        } catch (RuntimeException e) {
            return WithdrawalResult.failure(ATMConstants.Messages.UNEXPECTED_ERROR);
        }
    }
}
