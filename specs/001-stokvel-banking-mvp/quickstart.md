# Quickstart Guide: Digital Stokvel Banking MVP

**Purpose**: Get the Digital Stokvel Banking MVP running locally for development  
**Date**: 2026-03-24  
**Estimated Setup Time**: 30-45 minutes

---

## Prerequisites

### Required Software

**Backend (.NET/C#)**:
- .NET SDK 10.0 or later ([Download](https://dotnet.microsoft.com/download))
- PostgreSQL 16+ ([Download](https://www.postgresql.org/download/))
- Redis 7+ ([Download](https://redis.io/download/))
- Visual Studio 2022 or VS Code with C# extension

**Android**:
- Android Studio Hedgehog (2023.1.1) or later
- JDK 17
- Android SDK API Level 26+ (Android 8.0)
- Kotlin plugin

**iOS**:
- macOS 13+ (Ventura or later)
- Xcode 15+
- CocoaPods 1.12+

**Web (React + TypeScript)**:
- Node.js 20+ LTS ([Download](https://nodejs.org/))
- npm 10+ or yarn 1.22+

**Tools**:
- Git
- Docker (optional, for containerized PostgreSQL/Redis)
- Azure CLI (for cloud deployments)
- Postman or similar API testing tool

---

## Quick Setup (Docker Compose)

For fastest setup, use Docker Compose to spin up all backend services:

```bash
# Clone repository
git clone https://github.com/yourbank/digital-stokvel.git
cd digital-stokvel

# Start PostgreSQL, Redis, and backend API
docker-compose up -d

# Verify services are running
docker-compose ps

# View logs
docker-compose logs -f
```

**Services Started**:
- PostgreSQL: `localhost:5432` (user: `stokvel_dev`, password: `dev_password`)
- Redis: `localhost:6379`
- Backend API: `http://localhost:5000`

**Skip to**: [Run Mobile Apps](#run-mobile-apps-android--ios) or [Run Web Dashboard](#run-web-dashboard)

---

## Manual Setup

### 1. Clone Repository

```bash
git clone https://github.com/yourbank/digital-stokvel.git
cd digital-stokvel
```

---

### 2. Backend Setup (C# / ASP.NET Core)

#### 2.1 Install .NET SDK

```bash
# Verify .NET SDK installation
dotnet --version
# Expected: 10.0.x
```

#### 2.2 Setup PostgreSQL

**Option A - Docker**:
```bash
docker run --name stokvel-postgres \
  -e POSTGRES_USER=stokvel_dev \
  -e POSTGRES_PASSWORD=dev_password \
  -e POSTGRES_DB=stokvel_db \
  -p 5432:5432 \
  -d postgres:16
```

**Option B - Local Installation**:
1. Install PostgreSQL 16
2. Create database and user:
```sql
CREATE DATABASE stokvel_db;
CREATE USER stokvel_dev WITH PASSWORD 'dev_password';
GRANT ALL PRIVILEGES ON DATABASE stokvel_db TO stokvel_dev;
```

#### 2.3 Setup Redis

**Option A - Docker**:
```bash
docker run --name stokvel-redis \
  -p 6379:6379 \
  -d redis:7
```

**Option B - Local Installation**:
```bash
# Windows (using Chocolatey)
choco install redis

# macOS
brew install redis
brew services start redis

# Linux
sudo apt-get install redis-server
sudo systemctl start redis
```

#### 2.4 Configure Backend

Create `backend/src/DigitalStokvel.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=stokvel_db;Username=stokvel_dev;Password=dev_password",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-for-development-only",
    "Issuer": "https://localhost:5000",
    "Audience": "https://localhost:5000",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DigitalStokvel": "Debug"
    }
  }
}
```

#### 2.5 Run Database Migrations

```bash
cd backend/src/DigitalStokvel.API

# Apply migrations
dotnet ef database update

# Verify migration
dotnet ef migrations list
```

#### 2.6 Seed Development Data

```bash
# Run seeding script
dotnet run --seed-data

# This creates:
# - 3 test members (Ntombizodwa, Thandi, Sipho)
# - 2 test groups (Ntombizodwa Stokvel, Ubuntu Savings)
# - Sample contributions and transactions
```

#### 2.7 Run Backend API

```bash
cd backend/src/DigitalStokvel.API

# Build project
dotnet build

# Run API
dotnet run

# API should be running on https://localhost:5001
```

**Test API**:
```bash
# Health check
curl https://localhost:5001/health

# Expected response: {"status":"Healthy"}
```

---

### 3. Run Mobile Apps (Android & iOS)

#### 3.1 Android Setup

```bash
cd android

# Sync Gradle dependencies
./gradlew build

# Run on emulator
./gradlew installDebug

# Or open in Android Studio
# File > Open > Select 'android' directory
# Run > Run 'app'
```

**Configure API Endpoint**:

Edit `android/app/src/main/res/values/config.xml`:
```xml
<resources>
    <string name="api_base_url">http://10.0.2.2:5000</string>
    <!-- 10.0.2.2 is the host machine from Android emulator -->
</resources>
```

**Test Users** (for login):
- Phone: `+27821234567` | PIN: `1234` (Ntombizodwa - Chairperson)
- Phone: `+27821234568` | PIN: `1234` (Thandi - Treasurer)
- Phone: `+27821234569` | PIN: `1234` (Sipho - Member)

#### 3.2 iOS Setup

```bash
cd ios

# Install CocoaPods dependencies
pod install

# Open workspace in Xcode
open DigitalStokvel.xcworkspace
```

**Configure API Endpoint**:

Edit `ios/DigitalStokvel/Config.plist`:
```xml
<dict>
    <key>APIBaseURL</key>
    <string>http://localhost:5000</string>
</dict>
```

**Run in Simulator**:
- Select target device (e.g., iPhone 15 Pro)
- Press Cmd+R to build and run

---

### 4. Run Web Dashboard

#### 4.1 Install Dependencies

```bash
cd web

# Using npm
npm install

# Or using yarn
yarn install
```

#### 4.2 Configure Environment

Create `web/.env.development`:
```env
REACT_APP_API_BASE_URL=http://localhost:5000
REACT_APP_ENVIRONMENT=development
REACT_APP_VERSION=1.0.0
```

#### 4.3 Run Development Server

```bash
# Using npm
npm start

# Or using yarn
yarn start

# Web dashboard should open at http://localhost:3000
```

**Test Login**:
- Bank Customer ID: `BC001234`
- PIN: `1234`
- Role: Chairperson

---

## Testing

### Backend Unit Tests

```bash
cd backend/tests/DigitalStokvel.Tests.Unit

# Run all unit tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReporter=html
```

### Backend Integration Tests

```bash
cd backend/tests/DigitalStokvel.Tests.Integration

# Start Testcontainers (PostgreSQL + Redis in Docker)
dotnet test

# Integration tests automatically spin up database/Redis in Docker
```

### Android Unit Tests

```bash
cd android

# Run unit tests
./gradlew test

# Run instrumentation tests (requires emulator/device)
./gradlew connectedAndroidTest
```

### iOS Unit Tests

```bash
# In Xcode
# Select test scheme
# Press Cmd+U to run tests
```

### Web Unit Tests

```bash
cd web

# Run Jest tests
npm test

# Run with coverage
npm test -- --coverage
```

---

## Development Workflow

### 1. Create Feature Branch

```bash
git checkout -b feature/001-stokvel-banking-mvp
```

### 2. Make Changes

Edit code in your preferred IDE:
- **Backend**: Visual Studio 2022 or VS Code
- **Android**: Android Studio
- **iOS**: Xcode
- **Web**: VS Code

### 3. Run Tests

```bash
# Backend
cd backend && dotnet test

# Android
cd android && ./gradlew test

# Web
cd web && npm test
```

### 4. Commit and Push

```bash
git add .
git commit -m "feat: implement group creation API endpoint"
git push origin feature/001-stokvel-banking-mvp
```

### 5. Create Pull Request

Open PR on GitHub targeting `main` branch. CI pipeline will run:
- Unit tests
- Integration tests
- Linting
- Security scans

---

## Common Issues & Troubleshooting

### Issue: PostgreSQL Connection Failed

**Error**: `Npgsql.NpgsqlException: Connection refused`

**Solution**:
1. Verify PostgreSQL is running: `docker ps` or `pg_isready`
2. Check connection string in `appsettings.Development.json`
3. Ensure firewall allows port 5432

---

### Issue: Redis Connection Failed

**Error**: `StackExchange.Redis.RedisConnectionException: No connection is available`

**Solution**:
1. Verify Redis is running: `docker ps` or `redis-cli ping`
2. Check Redis connection string in `appsettings.Development.json`
3. Ensure firewall allows port 6379

---

### Issue: Android Emulator Cannot Reach API

**Error**: `Network error: Connection timeout`

**Solution**:
1. Use `10.0.2.2` (not `localhost`) for Android emulator
2. Ensure API is running and accessible: `curl http://localhost:5000/health`
3. Check emulator network settings: Settings > Network & Internet

---

### Issue: iOS Simulator Cannot Reach API

**Error**: `Network error: Cannot connect to host`

**Solution**:
1. Use `localhost` for iOS simulator (not `10.0.2.2`)
2. Ensure API is running with HTTPS: `https://localhost:5001`
3. Disable App Transport Security for local dev (edit `Info.plist`):
```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
</dict>
```

---

### Issue: EF Core Migrations Not Applied

**Error**: `Npgsql.PostgresException: relation "groups" does not exist`

**Solution**:
```bash
cd backend/src/DigitalStokvel.API

# Check pending migrations
dotnet ef migrations list

# Apply migrations
dotnet ef database update

# If migrations are corrupted, reset database
dotnet ef database drop --force
dotnet ef database update
```

---

### Issue: Web App CORS Error

**Error**: `Access to fetch at 'http://localhost:5000' from origin 'http://localhost:3000' has been blocked by CORS policy`

**Solution**:
Add CORS policy in `backend/src/DigitalStokvel.API/Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ...

app.UseCors("AllowWebDev");
```

---

## Next Steps

### Phase 2: Implementation Tasks

Once local development environment is set up, proceed to:

1. **Read**: [`tasks.md`](tasks.md) (generated by `/speckit.tasks` command)
2. **Implement**: User stories in priority order (P0 → P1 → P2)
3. **Test**: Write unit/integration tests for each task
4. **Review**: Submit PR for code review

---

## Useful Commands Reference

### Backend (.NET)

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run API
dotnet run --project backend/src/DigitalStokvel.API

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Generate EF Core DbContext scaffold from existing DB
dotnet ef dbcontext scaffold "Host=localhost;Database=stokvel_db;Username=stokvel_dev;Password=dev_password" Npgsql.EntityFrameworkCore.PostgreSQL
```

---

### Android

```bash
# Build debug APK
./gradlew assembleDebug

# Install on device
./gradlew installDebug

# Run unit tests
./gradlew test

# Run instrumentation tests
./gradlew connectedAndroidTest

# Clean build
./gradlew clean
```

---

### iOS

```bash
# Install pods
pod install

# Update pods
pod update

# Clean build
Product > Clean Build Folder (Cmd+Shift+K)

# Run tests
Product > Test (Cmd+U)
```

---

### Web (React)

```bash
# Install dependencies
npm install

# Start dev server
npm start

# Build for production
npm run build

# Run tests
npm test

# Run linter
npm run lint

# Format code
npm run format
```

---

### Docker

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# Rebuild containers
docker-compose up --build

# Remove volumes (reset database)
docker-compose down -v
```

---

## Additional Resources

- [REST API Contract](contracts/rest-api.md)
- [USSD Flow Documentation](contracts/ussd-flow.md)
- [Data Model](data-model.md)
- [Technical Research](research.md)
- [Implementation Plan](plan.md)

---

## Support

**Development Questions**: Slack channel `#digital-stokvel-dev`  
**Bug Reports**: GitHub Issues  
**Architecture Decisions**: See [ADR documentation](docs/architecture/decisions/)

---

**Happy Coding! 🚀**
