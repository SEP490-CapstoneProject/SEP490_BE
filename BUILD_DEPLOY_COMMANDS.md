# Build & Deploy Commands

Hướng dẫn build và deploy các services trong dự án.

## Prerequisites

```powershell
# Login to Azure
az login

# Login to Azure Container Registry
az acr login --name skillsnapregistry
```

## Build & Deploy Pattern

### 1. Generate Build Tag
```powershell
# Tạo tag theo timestamp
$tag = Get-Date -Format "yyyyMMddHHmmss"
Write-Output "Building with tag: $tag"

# (Optional) Lưu tag để dùng lại
Set-Content -Path "last-build-tag.txt" -Value $tag
```

### 2. Build Docker Image
```powershell
# Template
docker build -f src/Services/{ServiceName}/Dockerfile ` -t skillsnapregistry.azurecr.io/{service-name}:$tag .

# Examples:
docker build -f src/Services/Auth/Dockerfile -t skillsnapregistry.azurecr.io/auth-service:$tag .
docker build -f src/Services/UserProfile/Dockerfile -t skillsnapregistry.azurecr.io/userprofile-service:$tag .
docker build -f src/Services/Portfolio/Dockerfile -t skillsnapregistry.azurecr.io/portfolio-service:$tag .
docker build -f src/Services/Application/Dockerfile -t skillsnapregistry.azurecr.io/application-service:$tag .
docker build -f src/Services/Community/Dockerfile -t skillsnapregistry.azurecr.io/community-service:$tag .
docker build -f src/Services/Media/Dockerfile -t skillsnapregistry.azurecr.io/media-service:$tag .
docker build -f src/Services/Notification/Dockerfile -t skillsnapregistry.azurecr.io/notification-service:$tag .
docker build -f src/Services/Payment/Dockerfile -t skillsnapregistry.azurecr.io/payment-service:$tag .
docker build -f src/Services/Subscription/Dockerfile -t skillsnapregistry.azurecr.io/subscription-service:$tag .
docker build -f src/Gateway/Dockerfile -t skillsnapregistry.azurecr.io/gateway:$tag .
docker build -f src/Services/RealtimeService/Dockerfile -t skillsnapregistry.azurecr.io/realtime-service:$tag .
```

### 3. Push to Azure Container Registry
```powershell
# Template
docker push skillsnapregistry.azurecr.io/{service-name}:$tag

# Examples:
docker push skillsnapregistry.azurecr.io/auth-service:$tag
docker push skillsnapregistry.azurecr.io/userprofile-service:$tag
docker push skillsnapregistry.azurecr.io/portfolio-service:$tag
docker push skillsnapregistry.azurecr.io/application-service:$tag
docker push skillsnapregistry.azurecr.io/community-service:$tag
docker push skillsnapregistry.azurecr.io/media-service:$tag
docker push skillsnapregistry.azurecr.io/notification-service:$tag
docker push skillsnapregistry.azurecr.io/payment-service:$tag
docker push skillsnapregistry.azurecr.io/subscription-service:$tag
docker push skillsnapregistry.azurecr.io/gateway:$tag
docker push skillsnapregistry.azurecr.io/realtime-service:$tag
```

### 4. Deploy to Azure Container Apps
```powershell
# Template
az containerapp update `
  --name {app-name} `
  --resource-group CapStone `
  --image skillsnapregistry.azurecr.io/{service-name}:$tag

# Examples:
az containerapp update --name auth-service --resource-group CapStone --image skillsnapregistry.azurecr.io/auth-service:$tag
az containerapp update --name userprofile-service --resource-group CapStone --image skillsnapregistry.azurecr.io/userprofile-service:$tag
az containerapp update --name portfolio-service --resource-group CapStone --image skillsnapregistry.azurecr.io/portfolio-service:$tag
az containerapp update --name application-service --resource-group CapStone --image skillsnapregistry.azurecr.io/application-service:$tag
az containerapp update --name community-service --resource-group CapStone --image skillsnapregistry.azurecr.io/community-service:$tag
az containerapp update --name media-service --resource-group CapStone --image skillsnapregistry.azurecr.io/media-service:$tag
az containerapp update --name notification-service --resource-group CapStone --image skillsnapregistry.azurecr.io/notification-service:$tag
az containerapp update --name payment-service --resource-group CapStone --image skillsnapregistry.azurecr.io/payment-service:$tag
az containerapp update --name subscription-service --resource-group CapStone --image skillsnapregistry.azurecr.io/subscription-service:$tag
az containerapp update --name gateway --resource-group CapStone --image skillsnapregistry.azurecr.io/gateway:$tag
az containerapp update --name realtime-service --resource-group CapStone --image skillsnapregistry.azurecr.io/realtime-service:$tag
```

## Complete Examples

### Deploy Single Service (e.g., Auth Service)

```powershell
# 1. Login (nếu cần)
az acr login --name skillsnapregistry

# 2. Generate tag
$tag = Get-Date -Format "yyyyMMddHHmmss"
Write-Output "Building with tag: $tag"

# 3. Build
docker build -f src/Services/Auth/Dockerfile -t skillsnapregistry.azurecr.io/auth-service:$tag .

# 4. Push
docker push skillsnapregistry.azurecr.io/auth-service:$tag

# 5. Deploy
az containerapp update --name auth-service --resource-group CapStone --image skillsnapregistry.azurecr.io/auth-service:$tag
```

### Deploy Multiple Services

```powershell
# Login
az acr login --name skillsnapregistry

# Generate tag
$tag = Get-Date -Format "yyyyMMddHHmmss"
Write-Output "Deploying services with tag: $tag"

# Build all services
docker build -f src/Services/Auth/Dockerfile -t skillsnapregistry.azurecr.io/auth-service:$tag .
docker build -f src/Services/UserProfile/Dockerfile -t skillsnapregistry.azurecr.io/userprofile-service:$tag .
docker build -f src/Services/Portfolio/Dockerfile -t skillsnapregistry.azurecr.io/portfolio-service:$tag .

# Push all images
docker push skillsnapregistry.azurecr.io/auth-service:$tag
docker push skillsnapregistry.azurecr.io/userprofile-service:$tag
docker push skillsnapregistry.azurecr.io/portfolio-service:$tag

# Deploy all services
az containerapp update --name auth-service --resource-group CapStone --image skillsnapregistry.azurecr.io/auth-service:$tag
az containerapp update --name userprofile-service --resource-group CapStone --image skillsnapregistry.azurecr.io/userprofile-service:$tag
az containerapp update --name portfolio-service --resource-group CapStone --image skillsnapregistry.azurecr.io/portfolio-service:$tag
```

## Service Names Mapping

| Service Name | Container App Name | Dockerfile Path |
|--------------|-------------------|-----------------|
| Auth Service | auth-service | src/Services/Auth/Dockerfile |
| UserProfile Service | userprofile-service | src/Services/UserProfile/Dockerfile |
| Portfolio Service | portfolio-service | src/Services/Portfolio/Dockerfile |
| Application Service | application-service | src/Services/Application/Dockerfile |
| Community Service | community-service | src/Services/Community/Dockerfile |
| Media Service | media-service | src/Services/Media/Dockerfile |
| Notification Service | notification-service | src/Services/Notification/Dockerfile |
| Payment Service | payment-service | src/Services/Payment/Dockerfile |
| Subscription Service | subscription-service | src/Services/Subscription/Dockerfile |
| Realtime Service | realtime-service | src/Services/RealtimeService/Dockerfile |
| Gateway | gateway | src/Gateway/Dockerfile |

## Troubleshooting

### ACR Authentication Error
```powershell
# Error: authentication required
# Solution: Re-login to ACR
az acr login --name skillsnapregistry
```

### Build Cache Issues
```powershell
# Build without cache
docker build --no-cache -f src/Services/{ServiceName}/Dockerfile -t skillsnapregistry.azurecr.io/{service-name}:$tag .
```

### Check Service Logs
```powershell
# View logs
az containerapp logs show --name {service-name} --resource-group CapStone --tail 100 --follow false --format text

# Example:
az containerapp logs show --name portfolio-service --resource-group CapStone --tail 100 --follow false --format text
```

### Check Service Status
```powershell
# Get service info
az containerapp show --name {service-name} --resource-group CapStone --query "properties.{Status:runningStatus,LatestRevision:latestRevisionName,FQDN:'configuration.ingress.fqdn'}"

# Example:
az containerapp show --name portfolio-service --resource-group CapStone --query "properties.{Status:runningStatus,LatestRevision:latestRevisionName,FQDN:'configuration.ingress.fqdn'}"
```

## Quick Deploy Script Template

```powershell
# deploy-service.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ServiceName,
    
    [Parameter(Mandatory=$true)]
    [string]$ContainerAppName,
    
    [Parameter(Mandatory=$true)]
    [string]$DockerfilePath
)

# Generate tag
$tag = Get-Date -Format "yyyyMMddHHmmss"
Write-Output "Deploying $ServiceName with tag: $tag"

# Build
Write-Output "Building..."
docker build -f $DockerfilePath -t skillsnapregistry.azurecr.io/${ContainerAppName}:$tag .

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

# Push
Write-Output "Pushing to registry..."
docker push skillsnapregistry.azurecr.io/${ContainerAppName}:$tag

if ($LASTEXITCODE -ne 0) {
    Write-Error "Push failed!"
    exit 1
}

# Deploy
Write-Output "Deploying to Azure..."
az containerapp update --name $ContainerAppName --resource-group CapStone --image skillsnapregistry.azurecr.io/${ContainerAppName}:$tag

if ($LASTEXITCODE -eq 0) {
    Write-Output "✅ $ServiceName deployed successfully with tag: $tag"
} else {
    Write-Error "❌ Deploy failed!"
    exit 1
}
```

### Usage
```powershell
# Deploy Portfolio Service
.\deploy-service.ps1 -ServiceName "Portfolio" -ContainerAppName "portfolio-service" -DockerfilePath "src/Services/Portfolio/Dockerfile"

# Deploy Auth Service
.\deploy-service.ps1 -ServiceName "Auth" -ContainerAppName "auth-service" -DockerfilePath "src/Services/Auth/Dockerfile"
```

## Service URLs

All services are deployed at:
```
https://{service-name}.grayforest-11aba44e.southeastasia.azurecontainerapps.io
```

Examples:
- Auth: https://auth-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io
- UserProfile: https://userprofile-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io
- Portfolio: https://portfolio-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io
- Gateway: https://gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io

Swagger UI: `{service-url}/swagger/index.html`
