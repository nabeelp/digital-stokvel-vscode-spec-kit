using Microsoft.AspNetCore.Identity;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Application user extending ASP.NET Core Identity with stokvel-specific properties
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? PreferredLanguage { get; set; } // EN, ZU, ST, XH, AF
    public bool FicaVerified { get; set; }
    public DateTime? FicaVerificationDate { get; set; }
    public string? BankCustomerId { get; set; } // Link to banking system customer ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
