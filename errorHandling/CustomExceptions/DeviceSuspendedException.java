public class DeviceSuspendedException extends Exception {

    public DeviceSuspendedException(String deviceId) {
        super(ATMConstants.ExceptionMessages.DEVICE_SUSPENDED + deviceId);
    }
}