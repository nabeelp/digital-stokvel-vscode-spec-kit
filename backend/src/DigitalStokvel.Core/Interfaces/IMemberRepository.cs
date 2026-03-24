using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Repository interface for Member entity operations
/// </summary>
public interface IMemberRepository : IRepository<Member>
{
    /// <summary>
    /// Finds a member by their phone number
    /// </summary>
    Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a member by their bank customer ID
    /// </summary>
    Task<Member?> GetByBankCustomerIdAsync(string bankCustomerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a member by their ApplicationUser ID (AspNetUsers link)
    /// </summary>
    Task<Member?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active members with FICA verification
    /// </summary>
    Task<IEnumerable<Member>> GetVerifiedMembersAsync(CancellationToken cancellationToken = default);
}
