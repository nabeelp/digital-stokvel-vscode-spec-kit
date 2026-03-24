using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Hangfire;
using Hangfire.PostgreSql;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Infrastructure.Repositories;
using DigitalStokvel.Infrastructure.Messaging;
using DigitalStokvel.Infrastructure.Notifications;
using DigitalStokvel.Infrastructure.Payments;
using DigitalStokvel.Infrastructure.Jobs;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Services;
using DigitalStokvel.API.Middleware;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.ApplicationInsights(
        Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING") ?? "",
        TelemetryConverter.Traces)
    .CreateLogger();

try
{
    Log.Information("Starting Digital Stokvel Banking API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // User settings
        options.User.RequireUniqueEmail = false;
        options.User.AllowedUserNameCharacters = "0123456789+";
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Add JWT authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

    // Add distributed cache (Redis)
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "DigitalStokvel:";
    });

    // Add session management
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Add HTTP context accessor for audit logging
    builder.Services.AddHttpContextAccessor();

    // Register repositories
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IIdempotencyLogRepository, IdempotencyLogRepository>();
    builder.Services.AddScoped<IMemberRepository, MemberRepository>();
    builder.Services.AddScoped<IGroupRepository, GroupRepository>();
    builder.Services.AddScoped<IContributionRepository, ContributionRepository>();

    // Register payment gateway
    builder.Services.AddSingleton<IPaymentGateway, PaymentGatewayService>(sp =>
        new PaymentGatewayService(
            sp.GetRequiredService<ILogger<PaymentGatewayService>>(),
            builder.Configuration["PaymentGateway:ApiEndpoint"],
            builder.Configuration["PaymentGateway:ApiKey"]));

    // Register services
    builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
    builder.Services.AddSingleton<JwtTokenService>();
    builder.Services.AddScoped<AuthenticationService>();
    builder.Services.AddScoped<GroupService>();
    builder.Services.AddScoped<ContributionService>();
    builder.Services.AddScoped<ReceiptService>();
    builder.Services.AddScoped<IInterestService, InterestService>();
    builder.Services.AddSingleton<IServiceBusClient, ServiceBusClient>(sp =>
        new ServiceBusClient(
            builder.Configuration.GetConnectionString("ServiceBus") ?? "",
            sp.GetRequiredService<ILogger<ServiceBusClient>>()));
    builder.Services.AddSingleton<ISmsNotificationService, SmsNotificationService>(sp =>
        new SmsNotificationService(
            sp.GetRequiredService<ILogger<SmsNotificationService>>(),
            builder.Configuration.GetConnectionString("AzureCommunicationServices"),
            builder.Configuration["AzureCommunicationServices:SenderPhoneNumber"]));
    builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();

    // Register background jobs
    builder.Services.AddScoped<DigitalStokvel.Infrastructure.Jobs.PaymentReminderJob>();
    builder.Services.AddScoped<DigitalStokvel.Infrastructure.Jobs.DailyInterestAccrualJob>();
    builder.Services.AddScoped<DigitalStokvel.Infrastructure.Jobs.InterestCapitalizationJob>();

    // Add Hangfire for scheduled jobs
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = 5; // Number of concurrent job workers
        options.ServerName = $"DigitalStokvel-{Environment.MachineName}";
    });

    // Add rate limiting
    builder.Services.AddRateLimiter(RateLimitingConfiguration.ConfigureRateLimiting);

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000", // React web app
                    "http://localhost:5173", // Vite dev server
                    "capacitor://localhost", // Capacitor mobile
                    "ionic://localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Add controllers
    builder.Services.AddControllers();

    // Add API versioning
        
        // Hangfire dashboard in development only
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });
    }

    // Initialize Hangfire recurring jobs
    ConfigureRecurringJobs();uilder.Services.AddEndpointsApiExplorer();

    // Add OpenAPI
    builder.Services.AddOpenApi();

    // Add Application Insights
    builder.Services.AddApplicationInsightsTelemetry();

    var app = builder.Build();

    // Configure HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseRateLimiter();

    // Add custom error handling middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();

    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
        .WithName("HealthCheck")
        .WithTags("Health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}

// Configure recurring Hangfire jobs
static void ConfigureRecurringJobs()
{
    // Daily Interest Accrual Job - runs daily at 00:01 UTC
    RecurringJob.AddOrUpdate<DailyInterestAccrualJob>(
        "daily-interest-accrual",
        job => job.ExecuteAsync(CancellationToken.None),
        "1 0 * * *", // Cron: At 00:01 UTC every day
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    // Interest Capitalization Job - runs monthly on 1st at 00:01 UTC
    RecurringJob.AddOrUpdate<InterestCapitalizationJob>(
        "monthly-interest-capitalization",
        job => job.ExecuteAsync(CancellationToken.None),
        "1 0 1 * *", // Cron: At 00:01 UTC on day 1 of every month
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    // Payment Reminder Job - runs daily at 09:00 UTC (11:00 SAST)
    RecurringJob.AddOrUpdate<PaymentReminderJob>(
        "daily-payment-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 9 * * *", // Cron: At 09:00 UTC every day
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Utc
        });

    Log.Information("Hangfire recurring jobs scheduled: daily-interest-accrual, monthly-interest-capitalization, daily-payment-reminders");
}

// Hangfire authorization filter for development dashboard
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Allow access in development only
        return true;
    }
}
