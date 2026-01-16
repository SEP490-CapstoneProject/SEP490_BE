# 🚀 Service Build & Run Guide - Docker

## Complete guide to build and run microservices in Docker without errors

This guide shows you how to build and run any microservice in Docker, based on lessons learned from the Auth Service setup.

---

## 📋 Prerequisites

1. **Docker Desktop** running
2. **SQL Server** container running:
   ```powershell
   docker-compose up -d sqlserver
   ```
3. **.NET 8 SDK** installed locally

---

## 🔧 Step-by-Step: Build Any Service

### Method 1: Runtime-Only Build (Recommended - Avoids Network Issues)

This method builds the app locally first, then creates a Docker image with just the runtime.

#### 1. Create Migration (if needed)

```powershell
cd d:\Capstone\src\Services\[ServiceName]\[ServiceName].API

# Create migration
dotnet ef migrations add InitialCreate --project ..\[ServiceName].Infrastructure

# Example for UserProfile:
# cd d:\Capstone\src\Services\UserProfile\UserProfile.API
# dotnet ef migrations add InitialCreate --project ..\UserProfile.Infrastructure
```

#### 2. Publish the Application

```powershell
cd d:\Capstone\src\Services\[ServiceName]\[ServiceName].API
dotnet publish -c Release -o .\publish

# Example for UserProfile:
# cd d:\Capstone\src\Services\UserProfile\UserProfile.API
# dotnet publish -c Release -o .\publish
```

#### 3. Create Runtime Dockerfile

Create `Dockerfile.runtime` in the service folder:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080

# Copy pre-built files
COPY src/Services/[ServiceName]/[ServiceName].API/publish .

ENTRYPOINT ["dotnet", "[ServiceName].API.dll"]
```

**Example for UserProfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080

COPY src/Services/UserProfile/UserProfile.API/publish .

ENTRYPOINT ["dotnet", "UserProfile.API.dll"]
```

#### 4. Build Docker Image

```powershell
cd d:\Capstone

# Build image
docker build -t capstone-[servicename]-service -f src/Services/[ServiceName]/Dockerfile.runtime .

# Example for UserProfile:
# docker build -t capstone-userprofile-service -f src/Services/UserProfile/Dockerfile.runtime .
```

#### 5. Run Container

```powershell
docker run -d \
  --name [servicename]-service \
  --network capstone_recruitment-network \
  -p [PORT]:8080 \
  -e "ASPNETCORE_ENVIRONMENT=Development" \
  -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=[ServiceName]ServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;" \
  capstone-[servicename]-service

# Example for UserProfile (port 5002):
# docker run -d --name userprofile-service --network capstone_recruitment-network -p 5002:8080 -e "ASPNETCORE_ENVIRONMENT=Development" -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=UserProfileServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;" capstone-userprofile-service
```

---

### Method 2: Full Docker Build (If Network is Stable)

Use this if you can successfully download .NET SDK images.

#### 1. Ensure Base Images are Downloaded

```powershell
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/dotnet/sdk:8.0
```

#### 2. Use Standard Dockerfile

The standard Dockerfile (already created for all services):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/[ServiceName]/[ServiceName].API/[ServiceName].API.csproj", "Services/[ServiceName]/[ServiceName].API/"]
# ... copy other project files

RUN dotnet restore "Services/[ServiceName]/[ServiceName].API/[ServiceName].API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/[ServiceName]/ Services/[ServiceName]/

WORKDIR "/src/Services/[ServiceName]/[ServiceName].API"
RUN dotnet build "[ServiceName].API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "[ServiceName].API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "[ServiceName].API.dll"]
```

#### 3. Build with docker-compose

```powershell
docker-compose build [servicename]-service

# Example:
# docker-compose build userprofile-service
```

#### 4. Run with docker-compose

```powershell
docker-compose up -d [servicename]-service

# Example:
# docker-compose up -d userprofile-service
```

---

## 🔍 Verify Service is Running

### Check Container Status

```powershell
docker ps
```

You should see your service running.

### Check Logs

```powershell
docker logs [servicename]-service

# Look for:
# - "Now listening on: http://[::]:8080"
# - "Application started"
# - No error messages
```

### Verify Database Migration

```powershell
docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "USE [ServiceName]ServiceDb; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME;"

# Example for UserProfile:
# docker exec -it sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd" -C -Q "USE UserProfileServiceDb; SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME;"
```

### Test API

```powershell
# Access Swagger UI
Start-Process "http://localhost:[PORT]/swagger"

# Example for UserProfile (port 5002):
# Start-Process "http://localhost:5002/swagger"
```

---

## 📊 Service Port Mapping

| Service | Port | Database |
|---------|------|----------|
| Auth | 5001 | AuthServiceDb |
| UserProfile | 5002 | UserProfileServiceDb |
| Portfolio | 5003 | PortfolioServiceDb |
| Company | 5004 | CompanyServiceDb |
| JobHiring | 5005 | JobHiringServiceDb |
| Connection | 5006 | ConnectionServiceDb |
| Community | 5007 | CommunityServiceDb |
| Subscription | 5008 | SubscriptionServiceDb |
| Advertisement | 5009 | AdvertisementServiceDb |
| Moderation | 5010 | ModerationServiceDb |
| Notification | 5011 | NotificationServiceDb |

---

## 🐛 Common Issues & Solutions

### Issue 1: "EOF" or Network Errors When Pulling Images

**Error:**
```
Error response from daemon: failed to copy: httpReadSeeker: failed open: EOF
```

**Solution:** Use **Method 1 (Runtime-Only Build)** instead of Method 2.

---

### Issue 2: Migration Not Applied

**Error:**
```
Invalid object name 'TableName'
```

**Solution:**
1. Check if migration files exist in `[ServiceName].Infrastructure/Migrations/`
2. Verify `Program.cs` has auto-migration code:
   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var db = scope.ServiceProvider.GetRequiredService<[ServiceName]DbContext>();
       db.Database.Migrate();
   }
   ```
3. Rebuild the Docker image to include migration files

---

### Issue 3: Container Exits Immediately

**Check logs:**
```powershell
docker logs [servicename]-service
```

**Common causes:**
- SQL Server not ready (wait 30 seconds after starting SQL Server)
- Wrong connection string
- Missing environment variables

**Solution:**
```powershell
# Restart with proper wait time
docker-compose up -d sqlserver
Start-Sleep -Seconds 30
docker run -d ... # your docker run command
```

---

### Issue 4: Port Already in Use

**Error:**
```
Bind for 0.0.0.0:5002 failed: port is already allocated
```

**Solution:**
```powershell
# Find and stop the container using that port
docker ps
docker stop [container-name]
docker rm [container-name]
```

---

## 🔄 Quick Reference Commands

### Rebuild Service After Code Changes

```powershell
# 1. Publish
cd src\Services\[ServiceName]\[ServiceName].API
dotnet publish -c Release -o .\publish

# 2. Rebuild image
cd d:\Capstone
docker build -t capstone-[servicename]-service -f src/Services/[ServiceName]/Dockerfile.runtime .

# 3. Restart container
docker rm -f [servicename]-service
docker run -d --name [servicename]-service --network capstone_recruitment-network -p [PORT]:8080 -e "ASPNETCORE_ENVIRONMENT=Development" -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=[ServiceName]ServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;" capstone-[servicename]-service
```

### View All Running Services

```powershell
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### Stop All Services

```powershell
docker-compose down
```

### Start All Services

```powershell
docker-compose up -d
```

---

## ✅ Complete Example: UserProfile Service

Here's a complete example for the UserProfile service:

```powershell
# 1. Create migration
cd d:\Capstone\src\Services\UserProfile\UserProfile.API
dotnet ef migrations add InitialCreate --project ..\UserProfile.Infrastructure

# 2. Publish
dotnet publish -c Release -o .\publish

# 3. Create Dockerfile.runtime
# (Create file: src/Services/UserProfile/Dockerfile.runtime with content shown above)

# 4. Build Docker image
cd d:\Capstone
docker build -t capstone-userprofile-service -f src/Services/UserProfile/Dockerfile.runtime .

# 5. Run container
docker run -d --name userprofile-service --network capstone_recruitment-network -p 5002:8080 -e "ASPNETCORE_ENVIRONMENT=Development" -e "ConnectionStrings__DefaultConnection=Server=sqlserver;Database=UserProfileServiceDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;" capstone-userprofile-service

# 6. Verify
docker logs userprofile-service
Start-Process "http://localhost:5002/swagger"
```

---

## 🎯 Best Practices

1. **Always use Method 1 (Runtime-Only)** for development to avoid network issues
2. **Wait 30 seconds** after starting SQL Server before starting services
3. **Check logs** immediately after starting a container
4. **Verify migrations** by checking database tables
5. **Use consistent naming** for containers and images
6. **Document port assignments** to avoid conflicts

---

## 📝 Checklist for Each New Service

- [ ] Create migration files
- [ ] Publish application locally
- [ ] Create `Dockerfile.runtime`
- [ ] Build Docker image
- [ ] Start SQL Server (if not running)
- [ ] Run container with correct environment variables
- [ ] Check logs for errors
- [ ] Verify database tables created
- [ ] Test API via Swagger

---

**Status:** Follow this guide for all 10 remaining services to avoid the issues we encountered with Auth Service! 🚀
