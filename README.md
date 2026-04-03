# Recruitment Platform - Microservices Architecture

A comprehensive recruitment and professional networking platform built with **ASP.NET Core (.NET 8)** using microservice architecture, SQL Server, and Docker.

## 🏗️ Architecture Overview

This project implements a microservices architecture with:
- **10 Microservices** (each with its own database)
- **1 API Gateway** (YARP - routing and authentication)
- **SQL Server** (single instance, separate databases)
- **RabbitMQ** (event-driven communication)
- **Redis** (caching)
- **Docker & Docker Compose** (containerization and orchestration)
- **JWT Authentication** (secure token-based auth)
- **Clean Architecture** (Domain, Application, Infrastructure, API layers)

## 📋 Services

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| API Gateway | 5000 | - | YARP reverse proxy, routes requests |
| Auth Service | 5001 | AuthServiceDb | Authentication & user management |
| UserProfile Service | 5002 | UserProfileServiceDb | Employee & Company profiles |
| Portfolio Service | 5003 | PortfolioServiceDb | CV/Portfolio management |
| Company Service | 5004 | CompanyServiceDb | Company profiles & posts |
| Connection Service | 5006 | ConnectionServiceDb | Matching & messaging |
| Community Service | 5007 | CommunityServiceDb | Social posts & interactions |
| Subscription Service | 5008 | SubscriptionServiceDb | Plans & billing |
| Notification Service | 5011 | NotificationServiceDb | Real-time notifications (SignalR) |
| Media Service | 5012 | - | Cloudinary upload |
| Application Service | 5013 | ApplicationServiceDb | Job applications |

## 🚀 Quick Start

### Prerequisites

- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Docker Desktop** ([Download](https://www.docker.com/products/docker-desktop))
- **SQL Server** (included in Docker Compose)

### Running with Docker

```bash
# Build all services
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Running Locally (Development)

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run Auth Service
cd src/Services/Auth/Auth.API
dotnet run

# Run other services in separate terminals
cd src/Services/UserProfile/UserProfile.API
dotnet run
```

## 📦 Project Structure

```
RecruitmentPlatform/
├── src/
│   ├── ApiGateway/                    # YARP API Gateway
│   ├── Services/
│   │   ├── Auth/                      # ✅ Authentication
│   │   ├── UserProfile/               # ✅ Employee & Company profiles
│   │   ├── Portfolio/                 # ✅ CV/Portfolio management
│   │   ├── Company/                   # ✅ Company service
│   │   ├── Connection/                # ✅ Matching & chat
│   │   ├── Community/                 # ✅ Social posts
│   │   ├── Subscription/              # ✅ Plans & billing
│   │   ├── Notification/              # ✅ Real-time notifications
│   │   ├── Media/                     # ✅ Cloudinary upload
│   │   └── Application/               # ✅ Job applications
│   └── Shared/
│       ├── RecruitmentPlatform.Common/
│       └── RecruitmentPlatform.Contracts/
├── docker-compose.yml
└── RecruitmentPlatform.sln
```

## ✅ Completed Components

### 1. **Shared Libraries**
- ✅ `RecruitmentPlatform.Common` - Base entities, API response, JWT settings
- ✅ `RecruitmentPlatform.Contracts` - All enums and Auth DTOs

### 2. **Auth Service** (FULLY IMPLEMENTED)
- ✅ User & RefreshToken entities
- ✅ AuthDbContext with EF Core configurations
- ✅ Repository pattern implementation
- ✅ JWT token generation & validation
- ✅ BCrypt password hashing
- ✅ Auth controller with all endpoints:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `POST /api/auth/revoke`
  - `PUT /api/auth/change-password`
  - `PUT /api/auth/lock-user/{id}`
  - `GET /api/auth/users`
- ✅ Program.cs with auto-migration
- ✅ Dockerfile
- ✅ appsettings.json

### 3. **Docker Infrastructure**
- ✅ docker-compose.yml with all 12 services
- ✅ SQL Server container with health checks
- ✅ Network configuration
- ✅ Volume persistence

## 🔧 Implementation Guide

To complete the remaining services, follow the **Auth Service pattern**:

### Step 1: Create Domain Entities

```csharp
// Example: Portfolio.Domain/Entities/Portfolio.cs
using RecruitmentPlatform.Common;

namespace Portfolio.Domain.Entities;

public class Portfolio : BaseEntity
{
    public int EmployeeId { get; set; }
    public string TemplateType { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    // ... other properties
}
```

### Step 2: Create DbContext

```csharp
// Example: Portfolio.Infrastructure/Data/PortfolioDbContext.cs
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }
    
    public DbSet<Portfolio> Portfolios { get; set; }
    // ... other DbSets
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entities
    }
}
```

### Step 3: Create Repository Interface & Implementation

Follow the pattern in `Auth.Application/Interfaces/IAuthRepository.cs` and `Auth.Infrastructure/Repositories/AuthRepository.cs`

### Step 4: Create Service Layer

Follow the pattern in `Auth.Application/Services/AuthService.cs`

### Step 5: Create API Controller

Follow the pattern in `Auth.API/Controllers/AuthController.cs`

### Step 6: Configure Program.cs

Copy from `Auth.API/Program.cs` and update:
- DbContext registration
- Service registrations
- Connection string name

### Step 7: Create Dockerfile

Copy from `src/Services/Auth/Dockerfile` and update service names

### Step 8: Add NuGet Packages

```bash
# For each service, add these packages:
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

## 🗄️ Database Schemas

### Auth Service
```sql
User (Id, Email, PasswordHash, Role, Status, CreatedAt)
RefreshToken (Id, UserId, Token, ExpiredAt, Revoked)
```

### UserProfile Service
```sql
Employee (Id, UserId, FullName, Phone, AvatarUrl, Description, Status)
```

### Portfolio Service
```sql
Portfolio (Id, EmployeeId, TemplateType, FullName, Email, Phone, Description, Status)
Skill (Id, Name)
PortfolioSkill (Id, PortfolioId, SkillId)
Education (Id, PortfolioId, Institution, Degree, StartDate, EndDate)
Experience (Id, PortfolioId, Company, Position, StartDate, EndDate, Description)
Project (Id, PortfolioId, Name, Description, StartDate, EndDate)
Award (Id, PortfolioId, Title, Issuer, Date)
Activity (Id, PortfolioId, Name, Description, Date)
Hobby (Id, PortfolioId, Name, Description)
Reference (Id, PortfolioId, Name, Position, Company, Phone, Email)
```

### Company Service
```sql
Company (Id, UserId, CompanyName, Address, Description, AvatarUrl)
CompanyPost (Id, CompanyId, Position, Salary, Address, Description, Media, Status)
```

### Connection Service
```sql
Connection (Id, UserIdFrom, UserIdTo, JobPostId, Status, CreatedAt, MatchedAt)
MessageRoom (Id, ConnectionId, CreatedAt)
Message (Id, MessageRoomId, SenderUserId, Content, CreatedAt)
```

### Community Service
```sql
CommunityPost (Id, UserId, Content, Media, CreatedAt, Status)
Comment (Id, PostId, UserId, Content, CreatedAt)
Favorite (Id, PostId, UserId, CreatedAt)
```

### Subscription Service
```sql
Plan (Id, Name, Price, DurationInDays)
Subscription (Id, UserId, PlanId, StartDate, EndDate, Status)
Payment (Id, UserId, Amount, PaymentMethod, PaymentStatus, CreatedAt)
```

### Notification Service
```sql
Notification (Id, UserId, Title, Content, Type, ObjectId, ActorId, IsRead, CreatedAt)
```

### Application Service
```sql
Application (Id, EmployeeId, CompanyId, CompanyPostId, PortfolioId, RoomId, Status, AppliedAt)
-- Status: WAITING(0), REVIEWING(1), ACCEPTED(2), REJECTED(3)
```

## 🔐 Authentication Flow

1. **Register**: `POST /api/auth/register`
   ```json
   {
     "email": "user@example.com",
     "password": "Password123!",
     "role": 1
   }
   ```

2. **Login**: `POST /api/auth/login`
   ```json
   {
     "email": "user@example.com",
     "password": "Password123!"
   }
   ```
   
   Response:
   ```json
   {
     "success": true,
     "data": {
       "accessToken": "eyJhbGciOiJIUzI1NiIs...",
       "refreshToken": "base64string...",
       "user": { "id": 1, "email": "user@example.com", "role": 1 }
     }
   }
   ```

3. **Use Token**: Add to request headers
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
   ```

## 🧪 Testing

### Test Auth Service

```bash
# Register a new user
curl -X POST http://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","role":1}'

# Login
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Access protected endpoint
curl -X GET http://localhost:5001/api/auth/users \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### Swagger UI

Each service has Swagger UI available at:
- Auth Service: http://localhost:5001/swagger
- UserProfile Service: http://localhost:5002/swagger
- etc.

## 📝 Configuration

### JWT Settings (appsettings.json)

```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyForJWTTokenGenerationMustBeLongEnough123456",
    "Issuer": "RecruitmentPlatform",
    "Audience": "RecruitmentPlatformUsers",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Database Connection

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

## 🚨 Important Notes

### Chat Functionality Constraint
- ❌ No direct user-to-user messaging
- ✅ Chat only between job seeker and recruiter
- ✅ Chat enabled only after mutual matching (application acceptance)

### Database Isolation
- Each service has its own database
- No cross-database joins
- Data aggregation happens at the API/Gateway level

### Security
- JWT tokens expire after 60 minutes
- Refresh tokens valid for 7 days
- Passwords hashed with BCrypt
- Role-based authorization (USER, RECRUITER, ADMIN, MODERATOR)

## 🛠️ Development Workflow

1. **Implement remaining services** following the Auth Service pattern
2. **Create EF Core migrations** for each service:
   ```bash
   cd src/Services/[ServiceName]/[ServiceName].API
   dotnet ef migrations add InitialCreate --project ../[ServiceName].Infrastructure
   ```
3. **Test each service** individually before Docker deployment
4. **Build Docker images** and test with docker-compose
5. **Implement API Gateway** routing (YARP or Ocelot)

## 📚 Technologies Used

- **ASP.NET Core 8.0** - Web API framework
- **Entity Framework Core 8.0** - ORM
- **SQL Server 2022** - Database
- **Docker** - Containerization
- **YARP** - API Gateway / Reverse Proxy
- **RabbitMQ** - Message broker
- **Redis** - Caching
- **SignalR** - Real-time communication
- **JWT** - Authentication
- **BCrypt** - Password hashing
- **Cloudinary** - Media storage
- **Polly** - Resilience & retry policies
- **Swagger/OpenAPI** - API documentation
- **Clean Architecture** - Code organization

## 📄 License

This project is for educational/capstone purposes.

---

**Status**: 🟢 10 Services Implemented | 🟢 API Gateway (YARP) Complete
