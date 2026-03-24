# Scheduled Jobs Configuration

## Overview
The Digital Stokvel Banking API uses Hangfire for reliable background job scheduling. All jobs are configured with PostgreSQL storage for persistence and high availability.

## Architecture

**Technology**: Hangfire 1.8+ with PostgreSQL storage
**Database**: Shared connection with main application database
**Worker Count**: 5 concurrent workers
**Dashboard**: Available at `/hangfire` in development mode

## Scheduled Jobs

### 1. Daily Interest Accrual Job
**Job ID**: `daily-interest-accrual`
**Class**: `DigitalStokvel.Infrastructure.Jobs.DailyInterestAccrualJob`
**Schedule**: Daily at 00:01 UTC
**Cron Expression**: `1 0 * * *`

**Purpose**: Calculates and accrues daily compounding interest for all active group wallets.

**Process**:
1. Query all active groups with Balance > 0
2. For each group:
   - Determine interest tier based on balance (Tier 1: 3.5%, Tier 2: 4.5%, Tier 3: 5.5%)
   - Calculate daily interest: `A = P(1 + r/365)^1`
   - Add to `AccruedInterest` field (not yet capitalized to Balance)
   - Create InterestCalculation record with date, principal, rate, accrued amount
3. Log execution summary: groups processed, total interest accrued

**Tier Thresholds**:
- Tier 1 (3.5%): R0 - R9,999.99
- Tier 2 (4.5%): R10,000 - R49,999.99
- Tier 3 (5.5%): R50,000+

**Failure Handling**: Retries up to 3 times with exponential backoff. Failures logged to Application Insights.

### 2. Interest Capitalization Job
**Job ID**: `monthly-interest-capitalization`
**Class**: `DigitalStokvel.Infrastructure.Jobs.InterestCapitalizationJob`
**Schedule**: Monthly on 1st at 00:01 UTC
**Cron Expression**: `1 0 1 * *`

**Purpose**: Capitalizes accrued interest into the principal balance on the first of each month.

**Process**:
1. Query all active groups with `AccruedInterest` > 0
2. For each group:
   - Add `AccruedInterest` to `Balance`
   - Reset `AccruedInterest` to 0
   - Create audit log entry: "Interest capitalized: R{amount}"
   - Update `LastUpdated` timestamp
3. Send notification to Chairperson: "Your group earned R{amount} interest this month"
4. Log execution summary: groups processed, total capitalized

**Example**:
- Starting Balance: R10,000
- Accrued Interest: R37.50 (from daily calculations)
- After Capitalization: Balance = R10,037.50, AccruedInterest = R0.00

**Failure Handling**: Critical job. If fails, alerts sent to operations team. Retries up to 5 times.

### 3. Payment Reminder Job
**Job ID**: `daily-payment-reminders`
**Class**: `DigitalStokvel.Infrastructure.Jobs.PaymentReminderJob`
**Schedule**: Daily at 09:00 UTC (11:00 SAST)
**Cron Expression**: `0 9 * * *`

**Purpose**: Sends payment reminders to members 3 days and 1 day before their contribution is due.

**Process**:
1. Calculate reminder dates:
   - 3-day reminder: `Today + 3 days`
   - 1-day reminder: `Today + 1 day`
2. For each active group:
   - Calculate next payment due date based on `contributionFrequency`
   - If next payment date matches reminder target (3 days or 1 day out):
     - Send SMS to all active members via Azure Communication Services
     - Send push notification via Azure Notification Hubs
     - Log reminder sent
3. Supports 5 languages: English, Zulu, Sotho, Xhosa, Afrikaans
4. Log execution summary: groups checked, reminders sent

**Reminder Templates** (English):
- 3-day: "Hi {Name}, your {GroupName} contribution of R{Amount} is due in 3 days. Pay now to avoid penalties."
- 1-day: "Hi {Name}, reminder: {GroupName} contribution of R{Amount} is due tomorrow. Pay today to stay current."

**Failure Handling**: Non-critical. Failed sends logged but do not block other reminders. Retries up to 2 times.

## Dashboard

**URL**: `http://localhost:5000/hangfire` (Development only)
**Authentication**: Open in development, requires authentication in production
**Features**:
- View job execution history
- Monitor failed jobs
- Manually trigger jobs
- View queue statistics

**Production**: Dashboard disabled in production. Use Application Insights or Azure Monitor for job monitoring.

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=digitalstokvel;Username=postgres;Password=your_password"
  },
  "Hangfire": {
    "WorkerCount": 5,
    "ServerName": "DigitalStokvel-Primary"
  }
}
```

### Environment Variables
- `CONNECTIONSTRINGS__DEFAULTCONNECTION`: PostgreSQL connection string (required)
- `ASPNETCORE_ENVIRONMENT`: Set to `Development` to enable dashboard

## Monitoring

### Key Metrics
- **Job Success Rate**: Should be > 99.9%
- **Average Execution Time**:
  - Daily Interest Accrual: < 30 seconds for 1000 groups
  - Interest Capitalization: < 15 seconds for 1000 groups
  - Payment Reminders: < 2 minutes for 10,000 members

### Alerts
- **Failed Jobs**: Alert if any job fails 3+ times
- **Long-Running Jobs**: Alert if job takes > 5 minutes
- **Missed Schedules**: Alert if job doesn't run within 10 minutes of scheduled time

### Application Insights Queries
```kql
// Failed Hangfire jobs in last 24 hours
traces
| where timestamp > ago(24h)
| where message contains "Hangfire" and message contains "failed"
| summarize count() by severityLevel, operation_Name
```

## Database Schema

Hangfire creates the following tables in PostgreSQL:
- `hangfire.job` - Job definitions
- `hangfire.jobparameter` - Job parameters
- `hangfire.jobqueue` - Job queue
- `hangfire.state` - Job state history
- `hangfire.server` - Active servers
- `hangfire.set` - Sets (e.g., recurring jobs)
- `hangfire.hash` - Hash tables
- `hangfire.counter` - Counters
- `hangfire.list` - Lists
- `hangfire.aggregatedcounter` - Aggregated counters

**Schema**: All tables are created in the `hangfire` schema to keep them separate from application tables.

## Deployment

### Azure App Service
```powershell
# Ensure App Service has WEBSITE_RUN_FROM_PACKAGE = 1
# Hangfire will auto-create schema on first run
az webapp config appsettings set --name digitalstokvel-api --resource-group rg-digitalstokvel --settings WEBSITE_RUN_FROM_PACKAGE=1
```

### Azure Container Apps
```bash
# Hangfire requires persistent storage for PostgreSQL
# Ensure connection string is configured as secret
az containerapp secret set --name digitalstokvel-api --resource-group rg-digitalstokvel --secrets connectionstring="Host=..."
```

### Scaling Considerations
- **Multiple instances**: Hangfire handles distributed locks automatically
- **Worker count**: Adjust based on CPU cores (default: 5)
- **Job queue**: Supports multiple queues for prioritization

## Troubleshooting

### Jobs Not Running
1. Check Hangfire dashboard: `http://localhost:5000/hangfire`
2. Verify connection string is correct
3. Check Application Insights for errors
4. Ensure clock is synchronized (UTC)

### Performance Issues
1. Increase `WorkerCount` in Program.cs
2. Optimize job queries (add database indexes)
3. Split large jobs into smaller batches
4. Consider separate job server (dedicated worker instance)

### Failed Job Retries
- Hangfire automatically retries failed jobs with exponential backoff
- Max retries: 3 for non-critical, 5 for critical
- Retry delays: 0s, 15s, 60s (exponential)

## Testing

### Manual Trigger (Development)
```csharp
// In a controller or test
BackgroundJob.Enqueue<DailyInterestAccrualJob>(job => job.ExecuteAsync(CancellationToken.None));
```

### Unit Testing
```csharp
// Test job logic without scheduler
[Fact]
public async Task DailyInterestAccrualJob_AcruesInterest_ForActiveGroups()
{
    // Arrange
    var job = new DailyInterestAccrualJob(_interestService, _groupRepository, _logger);
    
    // Act
    await job.ExecuteAsync(CancellationToken.None);
    
    // Assert
    // Verify interest was accrued
}
```

## Security

- **Dashboard**: Requires authentication in production
- **Database**: Use read/write permissions, not database owner
- **Secrets**: Store connection strings in Azure Key Vault
- **Audit**: All job executions logged to Application Insights

## Maintenance

### Cleanup Old Job History
Hangfire automatically cleans up job history older than 7 days. Customize retention:
```csharp
GlobalConfiguration.Configuration
    .UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions
    {
        JobExpirationCheckInterval = TimeSpan.FromHours(1),
        CountersAggregateInterval = TimeSpan.FromMinutes(5),
        PrepareSchemaIfNecessary = true,
        QueuePollInterval = TimeSpan.FromSeconds(15)
    });
```

### Backup Strategy
- Hangfire tables are backed up with main database
- Job history is recoverable from backups
- Recurring job schedules are stored in `hangfire.set` table

## References

- [Hangfire Documentation](https://docs.hangfire.io/)
- [Hangfire.PostgreSql](https://github.com/frankhommers/Hangfire.PostgreSql)
- [Cron Expression Generator](https://crontab.guru/)

---

**Last Updated**: 2025-05-15
**Version**: 1.0.0 (MVP)
