using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;

namespace DigitalStokvel.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Member entity
/// </summary>
public class MemberRepository : Repository<Member>, IMemberRepository
{
    public MemberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Member>()
            .FirstOrDefaultAsync(m => m.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<Member?> GetByBankCustomerIdAsync(string bankCustomerId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Member>()
            .FirstOrDefaultAsync(m => m.BankCustomerId == bankCustomerId, cancellationToken);
    }

    public async Task<Member?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Member>()
            .FirstOrDefaultAsync(m => m.ApplicationUserId == applicationUserId, cancellationToken);
    }

    public async Task<IEnumerable<Member>> GetVerifiedMembersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Member>()
            .Where(m => m.FicaVerified && m.IsActive)
            .ToListAsync(cancellationToken);
    }
}
