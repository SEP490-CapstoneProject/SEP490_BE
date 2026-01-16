# 🚀 Quick Start Guide - Recruitment Platform

## Prerequisites Checklist

- [ ] .NET 8 SDK installed
- [ ] Docker Desktop installed and running
- [ ] At least 8GB RAM available
- [ ] 20GB free disk space

## Option 1: Run with Docker (Recommended)

### Step 1: Verify Docker is Running

```powershell
docker --version
docker-compose --version
```

### Step 2: Build and Start Services

```powershell
# Navigate to project root
cd d:\Capstone

# Build all Docker images (this will take 10-15 minutes first time)
docker-compose build

# Start all services
docker-compose up -d

# Check service status
docker-compose ps
```

### Step 3: Verify Services are Running

```powershell
# Check SQL Server
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -Q "SELECT name FROM sys.databases"

# View Auth Service logs
docker-compose logs -f auth-service

# Check all services
docker-compose ps
```

### Step 4: Test Auth Service

```powershell
# Register a new user
curl -X POST http://localhost:5001/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{\"email\":\"admin@test.com\",\"password\":\"Admin123!\",\"role\":3}'

# Login
curl -X POST http://localhost:5001/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{\"email\":\"admin@test.com\",\"password\":\"Admin123!\"}'
```

### Step 5: Access Swagger UI

Open in browser:
- Auth Service: http://localhost:5001/swagger
- UserProfile Service: http://localhost:5002/swagger (when implemented)

## Option 2: Run Locally (Development)

### Step 1: Start SQL Server in Docker

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" `
  -p 1433:1433 --name sqlserver `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### Step 2: Build Solution

```powershell
cd d:\Capstone
dotnet restore
dotnet build
```

### Step 3: Run Auth Service

```powershell
cd src\Services\Auth\Auth.API
dotnet run
```

The service will:
- Automatically create the database
- Run EF Core migrations
- Start listening on http://localhost:5001

### Step 4: Test with Swagger

Open: http://localhost:5001/swagger

## Troubleshooting

### Docker Issues

**Problem**: Docker containers won't start
```powershell
# Check Docker is running
docker info

# View logs
docker-compose logs

# Restart Docker Desktop
```

**Problem**: SQL Server health check failing
```powershell
# Check SQL Server logs
docker logs sqlserver

# Restart SQL Server
docker restart sqlserver
```

**Problem**: Port already in use
```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill process (replace PID)
taskkill /PID <PID> /F
```

### Build Issues

**Problem**: NuGet restore fails
```powershell
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

**Problem**: EF Core migration fails
```powershell
# Check connection string in appsettings.json
# Ensure SQL Server is running
# Try manual migration
cd src\Services\Auth\Auth.API
dotnet ef database update --project ..\Auth.Infrastructure
```

## Next Steps

1. **Complete remaining services** - Follow the Auth Service pattern
2. **Implement API Gateway** - Route all requests through gateway
3. **Add integration tests** - Test end-to-end workflows
4. **Deploy to production** - Use Azure, AWS, or your preferred cloud

## Useful Commands

```powershell
# Docker Commands
docker-compose up -d              # Start all services
docker-compose down               # Stop all services
docker-compose logs -f            # View all logs
docker-compose ps                 # List running services
docker-compose restart auth-service  # Restart specific service

# .NET Commands
dotnet build                      # Build solution
dotnet test                       # Run tests
dotnet clean                      # Clean build artifacts
dotnet ef migrations add <Name>   # Create migration
dotnet ef database update         # Apply migrations

# SQL Server Commands (in container)
docker exec -it sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd"
```

## Service URLs

| Service | URL | Swagger |
|---------|-----|---------|
| API Gateway | http://localhost:5000 | http://localhost:5000/swagger |
| Auth Service | http://localhost:5001 | http://localhost:5001/swagger |
| UserProfile | http://localhost:5002 | http://localhost:5002/swagger |
| Portfolio | http://localhost:5003 | http://localhost:5003/swagger |
| Company | http://localhost:5004 | http://localhost:5004/swagger |
| JobHiring | http://localhost:5005 | http://localhost:5005/swagger |
| Connection | http://localhost:5006 | http://localhost:5006/swagger |
| Community | http://localhost:5007 | http://localhost:5007/swagger |
| Subscription | http://localhost:5008 | http://localhost:5008/swagger |
| Advertisement | http://localhost:5009 | http://localhost:5009/swagger |
| Moderation | http://localhost:5010 | http://localhost:5010/swagger |
| Notification | http://localhost:5011 | http://localhost:5011/swagger |

## Database Connection

**Connection String:**
```
Server=localhost,1433;Database=AuthServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

**Connect with SQL Server Management Studio (SSMS):**
- Server: localhost,1433
- Authentication: SQL Server Authentication
- Login: sa
- Password: YourStrong@Passw0rd

## Support

For issues or questions:
1. Check the [README.md](README.md)
2. Review the implementation plan
3. Check Docker and service logs
4. Verify all prerequisites are installed

---

**Status**: Auth Service is fully functional and ready to test! 🎉
