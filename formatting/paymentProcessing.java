package com.payment.processing;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.HashMap;
import java.util.Map;

public class PaymentProcessor {
    
    private static final BigDecimal MIN_AMOUNT = new BigDecimal("0.01");
    private static final BigDecimal MAX_AMOUNT = new BigDecimal("5000");
    private static final int MAX_RETRIES = 2;
    
    private Logger logger;
    private NotificationService notifier;
    private Map<String, PaymentRecord> history;
    
    public PaymentProcessor(Logger logger, NotificationService notifier) {
        this.logger = logger;
        this.notifier = notifier;
        this.history = new HashMap<>();
    }
    
    public PaymentResult process(PaymentRequest request) {
        validate(request);
        
        int attempt = 0;
        while (attempt < MAX_RETRIES) {
            try {
                execute(request);
                record(request);
                notifySuccess(request);
                
                return new PaymentResult(true, PaymentMessages.PAYMENT_SUCCESS, generateId());
                
            } catch (PaymentException e) {
                attempt++;
                logger.log("Retry attempt: " + attempt);
            }
        }
        
        return new PaymentResult(false, PaymentMessages.PAYMENT_FAILED, null);
    }
    
    private void validate(PaymentRequest request) {
        if (request.customerId() == null || request.customerId().isBlank()) {
            throw new IllegalArgumentException(CustomerMessages.CUSTOMER_ID_REQUIRED);
        }
        
        if (request.amount() == null || 
            request.amount().compareTo(MIN_AMOUNT) < 0) {
            throw new IllegalArgumentException(PaymentMessages.INVALID_AMOUNT);
        }
    }
    
    private void execute(PaymentRequest request) {
        logger.log("Executing payment of " + request.amount());
        
        if (request.amount().compareTo(MAX_AMOUNT) > 0) {
            throw new PaymentException(PaymentMessages.LIMIT_EXCEED);
        }
    }
    
    private void record(PaymentRequest request) {
        PaymentRecord record = new PaymentRecord(
            request.customerId(),
            request.amount(),
            LocalDateTime.now()
        );
        
        history.put(generateId(), record);
    }
    
    private void notifySuccess(PaymentRequest request) {
        String message = "Payment of " + request.amount() + " processed";
        notifier.send(request.customerId(), message);
    }
    
    private String generateId() {
        return "TXN-" + System.currentTimeMillis();
    }
}