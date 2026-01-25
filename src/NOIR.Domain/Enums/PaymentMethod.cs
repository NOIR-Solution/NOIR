namespace NOIR.Domain.Enums;

/// <summary>
/// Payment method types supported by the system.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// E-wallet payment (MoMo, ZaloPay, etc.).
    /// </summary>
    EWallet = 0,

    /// <summary>
    /// QR code payment (VNPay QR, MoMo QR, etc.).
    /// </summary>
    QRCode = 1,

    /// <summary>
    /// Bank transfer via ATM or Internet Banking.
    /// </summary>
    BankTransfer = 2,

    /// <summary>
    /// Credit card payment.
    /// </summary>
    CreditCard = 3,

    /// <summary>
    /// Debit card payment.
    /// </summary>
    DebitCard = 4,

    /// <summary>
    /// Installment payment.
    /// </summary>
    Installment = 5,

    /// <summary>
    /// Cash on Delivery.
    /// </summary>
    COD = 6,

    /// <summary>
    /// Buy Now Pay Later.
    /// </summary>
    BuyNowPayLater = 7
}
