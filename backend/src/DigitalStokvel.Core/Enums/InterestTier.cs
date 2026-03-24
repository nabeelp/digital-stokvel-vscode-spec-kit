namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Interest rate tiers based on group balance
/// </summary>
public enum InterestTier
{
    /// <summary>
    /// Tier 1: R0 - R10,000 balance = 3.5% annual interest
    /// </summary>
    Tier1_3_5Pct = 1,
    
    /// <summary>
    /// Tier 2: R10,000 - R50,000 balance = 4.5% annual interest
    /// </summary>
    Tier2_4_5Pct = 2,
    
    /// <summary>
    /// Tier 3: R50,000+ balance = 5.5% annual interest
    /// </summary>
    Tier3_5_5Pct = 3
}

/// <summary>
/// Extension methods for InterestTier enum
/// </summary>
public static class InterestTierExtensions
{
    /// <summary>
    /// Get the annual interest rate for a tier (as decimal, e.g., 0.035 for 3.5%)
    /// </summary>
    public static decimal GetAnnualRate(this InterestTier tier)
    {
        return tier switch
        {
            InterestTier.Tier1_3_5Pct => 0.035m,
            InterestTier.Tier2_4_5Pct => 0.045m,
            InterestTier.Tier3_5_5Pct => 0.055m,
            _ => 0.035m
        };
    }

    /// <summary>
    /// Get the display name for a tier
    /// </summary>
    public static string GetDisplayName(this InterestTier tier)
    {
        return tier switch
        {
            InterestTier.Tier1_3_5Pct => "Tier 1 (3.5%)",
            InterestTier.Tier2_4_5Pct => "Tier 2 (4.5%)",
            InterestTier.Tier3_5_5Pct => "Tier 3 (5.5%)",
            _ => "Unknown Tier"
        };
    }

    /// <summary>
    /// Get the balance range description for a tier
    /// </summary>
    public static string GetBalanceRange(this InterestTier tier)
    {
        return tier switch
        {
            InterestTier.Tier1_3_5Pct => "R0 - R10,000",
            InterestTier.Tier2_4_5Pct => "R10,000 - R50,000",
            InterestTier.Tier3_5_5Pct => "R50,000+",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Determine interest tier based on balance amount
    /// </summary>
    public static InterestTier DetermineTierFromBalance(decimal balance)
    {
        if (balance < 10000m)
            return InterestTier.Tier1_3_5Pct;
        else if (balance < 50000m)
            return InterestTier.Tier2_4_5Pct;
        else
            return InterestTier.Tier3_5_5Pct;
    }
}
