public class ATMWithdrawalService {

    public void withdraw(String accountId, double amount)
            throws DeviceSuspendedException,
                   NetworkConnectionException,
                   InsufficientFundsException {

        DeviceHandle handle = getValidatedDeviceHandle();
        DeviceRecord  record = retrieveDeviceRecord(handle);

        ensureDeviceIsActive(record);
        ensureNetworkIsConnected(record);
        ensureSufficientFunds(accountId, amount);

        dispenseCash(handle, amount);
    }

    private DeviceHandle getValidatedDeviceHandle() {
        DeviceHandle handle = getHandle(DEV1);
        if (handle == DeviceHandle.INVALID) {
            throw new RuntimeException(ATMConstants.ExceptionMessages.INVALID_DEVICE_HANDLE + ATMConstants.Device.PRIMARY_ID);
        }
        return handle;
    }

    private void ensureDeviceIsActive(DeviceRecord record)
            throws DeviceSuspendedException {
        if (record.getStatus() == DEVICE_SUSPENDED) {
            throw new DeviceSuspendedException(DEV1);
        }
    }

    private void ensureNetworkIsConnected(DeviceRecord record)
            throws NetworkConnectionException {
        if (record.getWifiConnection() != WIFI_CONNECTED) {
            throw new NetworkConnectionException(DEV1);
        }
    }

    private void ensureSufficientFunds(String accountId, double amount)
            throws InsufficientFundsException {
        double balance = getBalance(accountId);
        if (balance < amount) {
            throw new InsufficientFundsException(accountId, amount, balance);
        }
    }

    private DeviceHandle  getHandle(String device return null) {return null;}
    private DeviceRecord  retrieveDeviceRecord(DeviceHandle h)    {return null;}
    private double        getBalance(String accountId)            {return 0;}
    private void          dispenseCash(DeviceHandle h, double amt) {  }
}
