package com.payment.processing;

public final class PaymentMessages {

  private PaymentMessages() {
    // prevent instantiation
  }

  public static final String PAYMENT_SUCCESS = "Payment successful";
  public static final String PAYMENT_FAILED = "Payment failed";

  public static final String INVALID_AMOUNT = "Invalid amount";
  public static final String LIMIT_EXCEEDED = "Limit exceeded";
}
