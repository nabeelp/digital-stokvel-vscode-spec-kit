namespace DigitalStokvel.Core.ValueObjects;

/// <summary>
/// Value object representing money with currency and amount.
/// Ensures consistent decimal precision for financial calculations.
/// </summary>
public record Money
{
    /// <summary>
    /// Currency code (ISO 4217). Defaults to ZAR for South African Rand.
    /// </summary>
    public string Currency { get; init; }

    /// <summary>
    /// Amount with 4 decimal places precision (supports up to R999,999,999,999,999.9999)
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Creates a new Money instance
    /// </summary>
    /// <param name="amount">Amount in decimal format</param>
    /// <param name="currency">Currency code (defaults to ZAR)</param>
    public Money(decimal amount, string currency = "ZAR")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required", nameof(currency));

        Amount = Math.Round(amount, 4, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Adds two Money values (must be same currency)
    /// </summary>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Subtracts two Money values (must be same currency)
    /// </summary>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {left.Currency} and {right.Currency}");

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>
    /// Multiplies money by a factor
    /// </summary>
    public static Money operator *(Money money, decimal factor)
    {
        return new Money(money.Amount * factor, money.Currency);
    }

    /// <summary>
    /// Divides money by a factor
    /// </summary>
    public static Money operator /(Money money, decimal factor)
    {
        if (factor == 0)
            throw new DivideByZeroException("Cannot divide money by zero");

        return new Money(money.Amount / factor, money.Currency);
    }

    /// <summary>
    /// Compares two Money values
    /// </summary>
    public static bool operator >(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare different currencies: {left.Currency} and {right.Currency}");

        return left.Amount > right.Amount;
    }

    /// <summary>
    /// Compares two Money values
    /// </summary>
    public static bool operator <(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot compare different currencies: {left.Currency} and {right.Currency}");

        return left.Amount < right.Amount;
    }

    public override string ToString() => $"{Currency} {Amount:N4}";

    /// <summary>
    /// Creates a zero money instance
    /// </summary>
    public static Money Zero(string currency = "ZAR") => new Money(0, currency);

    /// <summary>
    /// Checks if amount is zero
    /// </summary>
    public bool IsZero => Amount == 0;
}
