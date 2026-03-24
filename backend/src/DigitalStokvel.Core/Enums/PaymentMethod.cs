namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Payment method used for a contribution
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// One-tap payment using bank integration (instant confirmation)
    /// </summary>
    OneTap = 0,
    
    /// <summary>
    /// Debit order (automated monthly deduction)
    /// </summary>
    DebitOrder = 1,
    
    /// <summary>
    /// USSD payment from feature phone (operator-based)
    /// </summary>
    USSD = 2
}
