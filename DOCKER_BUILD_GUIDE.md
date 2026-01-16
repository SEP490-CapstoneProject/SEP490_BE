# Docker Build Guide

## ✅ Working Services

Only **2 services** are fully implemented and can be built:

1. **API Gateway** - Basic scaffolding
2. **Auth Service** - Fully functional

## 🔴 Why Other Services Fail

The remaining 9 services (UserProfile, Portfolio, Company, JobHiring, Connection, Community, Subscription, Advertisement, Moderation, Notification) will **fail to build** because:

- They have project structure but **no implementation**
- Missing Controllers, Services, DbContext, etc.
- This is **expected and normal**

## 🚀 How to Build & Run Working Services

### Option 1: Build Only Working Services

```powershell
# Build only the services that work
docker-compose build sqlserver apigateway auth-service

# Start them
docker-compose up -d sqlserver auth-service

# Check status
docker-compose ps

# View logs
docker-compose logs -f auth-service
```

### Option 2: Run Auth Service Locally (Recommended)

```powershell
# Start SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" `
  -p 1433:1433 --name sqlserver -d `
  mcr.microsoft.com/mssql/server:2022-latest

# Run Auth Service locally
cd d:\Capstone\src\Services\Auth\Auth.API
dotnet run

# Access Swagger
# http://localhost:5001/swagger
```

## 📝 To Fix Build Errors

You need to implement each service following the Auth Service pattern:

1. Create domain entities
2. Create DbContext
3. Create repositories
4. Create services
5. Create controllers
6. Configure Program.cs

See [`README.md`](file:///d:/Capstone/README.md) for detailed implementation guide.

## 🎯 Quick Test

Test Auth Service right now:

```powershell
# Start SQL Server
docker-compose up -d sqlserver

# Run Auth Service
cd src\Services\Auth\Auth.API
dotnet run

# In another terminal, test registration
curl -X POST http://localhost:5001/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{\"email\":\"test@example.com\",\"password\":\"Test123!\",\"role\":1}'
```

## ⚠️ Important

**Don't try to build all services with `docker-compose build`** until you've implemented them. It will fail and waste time.

**Instead:**
- Build only what works: `docker-compose build auth-service`
- Or run locally: `dotnet run` in Auth.API directory

---

**Status**: Auth Service is ready to use! The other services need implementation first. 🚀
