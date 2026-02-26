public class NetworkConnectionException extends Exception {

    public NetworkConnectionException(String deviceId) {
        super(ATMConstants.ExceptionMessages.NETWORK_UNAVAILABLE + deviceId);
    }
}