using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Jobs;

/// <summary>
/// Background job that runs on the 1st of each month at 00:01 UTC
/// Capitalizes accrued interest into group balance
/// </summary>
public class InterestCapitalizationJob
{
    private readonly ILogger<InterestCapitalizationJob> _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly IInterestService _interestService;

    public InterestCapitalizationJob(
        ILogger<InterestCapitalizationJob> logger,
        IGroupRepository groupRepository,
        IInterestService interestService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _interestService = interestService ?? throw new ArgumentNullException(nameof(interestService));
    }

    /// <summary>
    /// Execute monthly interest capitalization for all active groups
    /// Adds AccruedInterest to Balance and resets AccruedInterest to 0
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Monthly interest capitalization job started at {Timestamp}",
            DateTime.UtcNow);

        try
        {
            // Get all active groups - in production, implement GetAllActiveGroupsAsync method
            _logger.LogWarning(
                "[STUB] InterestCapitalizationJob - would iterate through all active groups. Implement GetAllActiveGroupsAsync repository method.");

            int successCount = 0;
            int failureCount = 0;

            // Stub implementation - would iterate through actual groups
            var activeGroups = new List<Guid>(); // Would come from repository

            foreach (var groupId in activeGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var (success, newBalance, errorMessage) = await _interestService.CapitalizeMonthlyAsync(
                        groupId,
                        cancellationToken);

                    if (success)
                    {
                        successCount++;
                        _logger.LogInformation(
                            "Interest capitalized for group {GroupId}. New balance: R{NewBalance}",
                            groupId,
                            newBalance);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "Failed to capitalize interest for group {GroupId}: {Error}",
                            groupId,
                            errorMessage);
                    }

                    // Rate limiting between groups
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(
                        ex,
                        "Exception while capitalizing interest for group {GroupId}",
                        groupId);
                }
            }

            _logger.LogInformation(
                "Monthly interest capitalization completed. Success: {Success}, Failed: {Failed}",
                successCount,
                failureCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monthly interest capitalization job failed");
            throw;
        }
    }
}
