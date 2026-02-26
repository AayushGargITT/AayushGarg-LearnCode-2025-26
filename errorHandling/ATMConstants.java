public final class ATMConstants {
    private ATMConstants() {}

    public static final class Device {
        public static final String PRIMARY_ID = "DEV1";

        private Device() {}
    }

    public static final class Messages {
        public static final String WITHDRAWAL_SUCCESS =
            "Withdrawal successful.";

        public static final String DEVICE_SUSPENDED =
            "This ATM is temporarily out of service. Please use another "
            + "machine.";

        public static final String NETWORK_ERROR =
            "Unable to connect to the network. Please try again shortly.";

        public static final String INSUFFICIENT_FUNDS =
            "Your account balance is insufficient for this withdrawal.";

        public static final String UNEXPECTED_ERROR =
            "An unexpected error occurred. Please contact support.";

        private Messages() {}
    }

    public static final class ExceptionMessages {
        public static final String INVALID_DEVICE_HANDLE =
            "Could not obtain a valid device handle for: ";

        public static final String DEVICE_SUSPENDED =
            "ATM device is currently suspended: ";

        public static final String NETWORK_UNAVAILABLE =
            "No network connection available for device: ";

        public static final String INSUFFICIENT_FUNDS_FORMAT =
            "Insufficient funds for account '%s'. Requested: %.2f, Available: "
            + "%.2f";

        private ExceptionMessages() {}
    }
}