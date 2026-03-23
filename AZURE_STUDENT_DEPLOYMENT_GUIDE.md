# 🎓 Azure Student Deployment Guide - SkillSnap Platform

## 📋 Table of Contents
1. [Azure Student Account Overview](#azure-student-account-overview)
2. [Cost Estimation & Optimization](#cost-estimation--optimization)
3. [Architecture Overview](#architecture-overview)
4. [Prerequisites](#prerequisites)
5. [Step-by-Step Deployment](#step-by-step-deployment)
6. [Post-Deployment Configuration](#post-deployment-configuration)
7. [Monitoring & Troubleshooting](#monitoring--troubleshooting)
8. [Cost Management](#cost-management)

---

## 🎓 Azure Student Account Overview

### What You Get:
- **$100 USD free credit** (valid for 12 months)
- **Free services** for 12 months:
  - Azure App Service (B1 tier)
  - Azure SQL Database (Basic tier)
  - Azure Storage
  - Azure Container Registry
- **Always free services**:
  - Azure Key Vault (limited transactions)
  - Azure Monitor (limited data)
  - Azure DevOps

### Important Limits:
- No credit card required (but limited to free/discounted services)
- Maximum 10 cores for VMs
- Some premium services are not available

---

## 💰 Cost Estimation & Optimization

### Recommended Architecture for Azure Student ($100 credit):

| Service | Tier | Monthly Cost | Notes |
|---------|------|--------------|-------|
| **Azure SQL Database** | Basic (5 DTU) | ~$5 | All services share one DB |
| **Azure Container Apps** | Consumption | ~$0-20 | Pay per use, autoscale to 0 |
| **Azure Container Registry** | Basic | ~$5 | Store Docker images |
| **Azure Key Vault** | Standard | ~$3-5 | Store secrets |
| **Azure Redis Cache** | Basic C0 (250MB) | ~$16 | Session & caching |
| **RabbitMQ on Container App** | Consumption | ~$5-10 | Message queue |
| **TOTAL** | | **~$34-61/month** | Leaves $40+ for other needs |

### ✅ **Cost Optimization Tips:**
1. **Use Container Apps instead of App Service** - Auto-scale to zero when idle
2. **Single SQL Database** - Use schemas to separate services (saves ~$25/month)
3. **Consumption tier** - Only pay for actual usage
4. **B1 Free tier** - Use the 750 hours/month free App Service if needed
5. **Azure Student Resources** - Some services are completely free for students

---

## 🏗️ Architecture Overview

### Deployment Strategy: Azure Container Apps

```
Internet
    ↓
Azure API Management (Optional - $50/month, skip for student)
    ↓
Azure Container Apps Environment
    ├── API Gateway (YARP)
    ├── Auth Service
    ├── UserProfile Service
    ├── Portfolio Service
    ├── Company Service
    ├── Community Service
    ├── Subscription Service
    ├── Payment Service
    ├── Notification Service
    ├── Media Service
    └── Application Service
    ↓
Azure SQL Database (Single instance, multiple schemas)
Azure Redis Cache
RabbitMQ Container App
Azure Blob Storage (Cloudinary fallback)
Azure Key Vault (Secrets)
```

---

## 📦 Prerequisites

### 1. Install Required Tools

```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI

# Verify installation
az --version

# Login to Azure
az login

# Set your subscription (Azure Student)
az account list --output table
az account set --subscription "Azure for Students"
```

### 2. Install Docker Desktop
```powershell
# Already installed based on your setup
docker --version
docker compose --version
```

### 3. Prepare Environment
```powershell
# Ensure .env file exists with real credentials
cp .env.example .env
# Edit .env with actual values
```

---

## 🚀 Step-by-Step Deployment

### Phase 1: Create Azure Resources

#### Step 1.1: Set Variables
```powershell
# Set your configuration
$RESOURCE_GROUP = "skillsnap-student-rg"
$LOCATION = "southeastasia"  # Closest to Vietnam, or use "eastasia"
$SQL_SERVER = "skillsnap-sql-server"
$SQL_DB = "skillsnap-db"
$SQL_ADMIN = "sqladmin"
$SQL_PASSWORD = "YourStrong@Passw0rd123"  # Change this!
$ACR_NAME = "skillsnapregistry"  # Must be globally unique, lowercase only
$KEYVAULT_NAME = "skillsnap-kv"  # Must be globally unique
$CONTAINER_ENV = "skillsnap-env"
$REDIS_NAME = "skillsnap-redis"

# Set default resource group
az configure --defaults group=$RESOURCE_GROUP location=$LOCATION
```

#### Step 1.2: Create Resource Group
```powershell
az group create `
  --name $RESOURCE_GROUP `
  --location $LOCATION

Write-Host "✅ Resource group created" -ForegroundColor Green
```

#### Step 1.3: Create Azure SQL Database
```powershell
# Create SQL Server
az sql server create `
  --name $SQL_SERVER `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --admin-user $SQL_ADMIN `
  --admin-password $SQL_PASSWORD

Write-Host "✅ SQL Server created" -ForegroundColor Green

# Configure firewall to allow Azure services
az sql server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# Allow your IP for management (replace with your IP)
$MY_IP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az sql server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name AllowMyIP `
  --start-ip-address $MY_IP `
  --end-ip-address $MY_IP

Write-Host "✅ Firewall rules configured" -ForegroundColor Green

# Create database (Basic tier - cheapest)
az sql db create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER `
  --name $SQL_DB `
  --service-objective Basic `
  --backup-storage-redundancy Local

Write-Host "✅ SQL Database created" -ForegroundColor Green
Write-Host "Connection string: Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" -ForegroundColor Cyan
```

#### Step 1.4: Create Azure Container Registry
```powershell
az acr create `
  --name $ACR_NAME `
  --resource-group $RESOURCE_GROUP `
  --sku Basic `
  --admin-enabled true

Write-Host "✅ Container Registry created" -ForegroundColor Green

# Get ACR credentials
$ACR_USERNAME = az acr credential show --name $ACR_NAME --query username -o tsv
$ACR_PASSWORD = az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv

Write-Host "ACR Username: $ACR_USERNAME" -ForegroundColor Cyan
Write-Host "ACR Password: $ACR_PASSWORD" -ForegroundColor Cyan
```

#### Step 1.5: Create Azure Redis Cache
```powershell
az redis create `
  --name $REDIS_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku Basic `
  --vm-size C0

Write-Host "✅ Redis Cache created (this takes 5-10 minutes)" -ForegroundColor Green

# Get Redis connection string (after creation completes)
$REDIS_KEY = az redis list-keys --name $REDIS_NAME --resource-group $RESOURCE_GROUP --query primaryKey -o tsv
$REDIS_CONNECTION = "$REDIS_NAME.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"

Write-Host "Redis connection string: $REDIS_CONNECTION" -ForegroundColor Cyan
```

#### Step 1.6: Create Azure Key Vault
```powershell
az keyvault create `
  --name $KEYVAULT_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --enable-rbac-authorization false

Write-Host "✅ Key Vault created" -ForegroundColor Green

# Add secrets
az keyvault secret set --vault-name $KEYVAULT_NAME --name "SqlConnectionString" --value "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set --vault-name $KEYVAULT_NAME --name "JwtSecret" --value "your-256-bit-secret-key-here-must-be-at-least-32-characters-long-change-this"

az keyvault secret set --vault-name $KEYVAULT_NAME --name "RedisConnection" --value $REDIS_CONNECTION

# Payment provider secrets (replace with your actual values)
az keyvault secret set --vault-name $KEYVAULT_NAME --name "VNPay--TmnCode" --value "YOUR_VNPAY_TMN_CODE"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "VNPay--HashSecret" --value "YOUR_VNPAY_HASH_SECRET"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--PartnerCode" --value "YOUR_MOMO_PARTNER_CODE"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--AccessKey" --value "YOUR_MOMO_ACCESS_KEY"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--SecretKey" --value "YOUR_MOMO_SECRET_KEY"

# Cloudinary secrets
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--CloudName" --value "YOUR_CLOUD_NAME"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--ApiKey" --value "YOUR_API_KEY"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--ApiSecret" --value "YOUR_API_SECRET"

Write-Host "✅ Secrets added to Key Vault" -ForegroundColor Green
```

#### Step 1.7: Create Container Apps Environment
```powershell
# Install Container Apps extension
az extension add --name containerapp --upgrade

# Create Container Apps Environment
az containerapp env create `
  --name $CONTAINER_ENV `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION

Write-Host "✅ Container Apps Environment created" -ForegroundColor Green
```

---

### Phase 2: Build & Push Docker Images

#### Step 2.1: Login to ACR
```powershell
az acr login --name $ACR_NAME
```

#### Step 2.2: Build and Push All Services
```powershell
# Change to project root
cd D:\Capstone

# List of services to build
$services = @(
    "Auth",
    "UserProfile", 
    "Portfolio",
    "Company",
    "Community",
    "Subscription",
    "Payment",
    "Notification",
    "Media",
    "Application"
)

foreach ($service in $services) {
    Write-Host "`n🔨 Building $service Service..." -ForegroundColor Yellow
    
    # Build image
    docker build -t "$ACR_NAME.azurecr.io/$($service.ToLower())-service:latest" `
        -f "src/Services/$service/Dockerfile" .
    
    # Push to ACR
    docker push "$ACR_NAME.azurecr.io/$($service.ToLower())-service:latest"
    
    Write-Host "✅ $service Service pushed to ACR" -ForegroundColor Green
}

Write-Host "`n✅ All services built and pushed!" -ForegroundColor Green
```

---

### Phase 3: Deploy Services to Container Apps

#### Step 3.1: Create RabbitMQ Service
```powershell
az containerapp create `
  --name rabbitmq `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINER_ENV `
  --image rabbitmq:3-management `
  --target-port 5672 `
  --ingress internal `
  --min-replicas 1 `
  --max-replicas 1 `
  --cpu 0.5 `
  --memory 1.0Gi `
  --env-vars `
    RABBITMQ_DEFAULT_USER=guest `
    RABBITMQ_DEFAULT_PASS=guest

Write-Host "✅ RabbitMQ deployed" -ForegroundColor Green
```

#### Step 3.2: Deploy Backend Services

Create a deployment script for all services:

```powershell
# Function to deploy a service
function Deploy-Service {
    param(
        [string]$ServiceName,
        [int]$Port = 8080,
        [bool]$HasKeyVault = $true
    )
    
    $imageName = "$ACR_NAME.azurecr.io/$($ServiceName.ToLower())-service:latest"
    
    Write-Host "`n🚀 Deploying $ServiceName Service..." -ForegroundColor Yellow
    
    $envVars = @(
        "ASPNETCORE_ENVIRONMENT=Production",
        "ConnectionStrings__DefaultConnection=secretref:sqlconnectionstring",
        "Jwt__Secret=secretref:jwtsecret",
        "Jwt__Issuer=skillsnap-api",
        "Jwt__Audience=skillsnap-client",
        "RabbitMQ__Host=rabbitmq",
        "RabbitMQ__Port=5672",
        "RabbitMQ__Username=guest",
        "RabbitMQ__Password=guest",
        "Redis__Connection=secretref:redisconnection"
    )
    
    # Create container app
    az containerapp create `
        --name "$($ServiceName.ToLower())-service" `
        --resource-group $RESOURCE_GROUP `
        --environment $CONTAINER_ENV `
        --image $imageName `
        --registry-server "$ACR_NAME.azurecr.io" `
        --registry-username $ACR_USERNAME `
        --registry-password $ACR_PASSWORD `
        --target-port $Port `
        --ingress internal `
        --min-replicas 0 `
        --max-replicas 3 `
        --cpu 0.5 `
        --memory 1.0Gi `
        --env-vars ($envVars -join " ")
    
    # Add Key Vault secrets if needed
    if ($HasKeyVault) {
        $principalId = az containerapp show `
            --name "$($ServiceName.ToLower())-service" `
            --resource-group $RESOURCE_GROUP `
            --query identity.principalId -o tsv
        
        # Grant Key Vault access
        az keyvault set-policy `
            --name $KEYVAULT_NAME `
            --object-id $principalId `
            --secret-permissions get list
    }
    
    Write-Host "✅ $ServiceName Service deployed" -ForegroundColor Green
}

# Deploy all services
Deploy-Service -ServiceName "Auth"
Deploy-Service -ServiceName "UserProfile"
Deploy-Service -ServiceName "Portfolio"
Deploy-Service -ServiceName "Company"
Deploy-Service -ServiceName "Community"
Deploy-Service -ServiceName "Subscription"
Deploy-Service -ServiceName "Payment"
Deploy-Service -ServiceName "Notification"
Deploy-Service -ServiceName "Media"
Deploy-Service -ServiceName "Application"
```

#### Step 3.3: Deploy API Gateway (YARP)
```powershell
# Build and push gateway
docker build -t "$ACR_NAME.azurecr.io/api-gateway:latest" `
    -f src/ApiGateway/Dockerfile .

docker push "$ACR_NAME.azurecr.io/api-gateway:latest"

# Deploy gateway with external ingress
az containerapp create `
  --name api-gateway `
  --resource-group $RESOURCE_GROUP `
  --environment $CONTAINER_ENV `
  --image "$ACR_NAME.azurecr.io/api-gateway:latest" `
  --registry-server "$ACR_NAME.azurecr.io" `
  --registry-username $ACR_USERNAME `
  --registry-password $ACR_PASSWORD `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 `
  --max-replicas 5 `
  --cpu 0.5 `
  --memory 1.0Gi `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production

# Get gateway URL
$GATEWAY_URL = az containerapp show `
  --name api-gateway `
  --resource-group $RESOURCE_GROUP `
  --query properties.configuration.ingress.fqdn -o tsv

Write-Host "`n✅ API Gateway deployed!" -ForegroundColor Green
Write-Host "🌐 Gateway URL: https://$GATEWAY_URL" -ForegroundColor Cyan
```

---

## 🔧 Post-Deployment Configuration

### Step 4.1: Update VNPay & MoMo Webhook URLs

Update your payment provider dashboards with new webhook URLs:

**VNPay:**
- Return URL: `https://your-gateway-url.azurecontainerapps.io/api/payments/return/vnpay`
- IPN URL: `https://your-gateway-url.azurecontainerapps.io/api/payments/webhook/vnpay`

**MoMo:**
- Return URL: `https://your-gateway-url.azurecontainerapps.io/api/payments/return/momo`
- IPN URL: `https://your-gateway-url.azurecontainerapps.io/api/payments/webhook/momo`

### Step 4.2: Test Deployment
```powershell
# Get gateway URL
$GATEWAY_URL = az containerapp show `
  --name api-gateway `
  --resource-group $RESOURCE_GROUP `
  --query properties.configuration.ingress.fqdn -o tsv

# Test health endpoint
Invoke-RestMethod -Uri "https://$GATEWAY_URL/health" -Method Get

# Test auth endpoint
Invoke-RestMethod -Uri "https://$GATEWAY_URL/api/auth/health" -Method Get
```

---

## 📊 Monitoring & Troubleshooting

### View Logs
```powershell
# View logs for a service
az containerapp logs show `
  --name payment-service `
  --resource-group $RESOURCE_GROUP `
  --follow

# View recent logs
az containerapp logs show `
  --name payment-service `
  --resource-group $RESOURCE_GROUP `
  --tail 100
```

### Check Service Status
```powershell
# List all container apps
az containerapp list `
  --resource-group $RESOURCE_GROUP `
  --output table

# Get service details
az containerapp show `
  --name payment-service `
  --resource-group $RESOURCE_GROUP
```

### Scale Services Manually
```powershell
# Scale up during high traffic
az containerapp update `
  --name payment-service `
  --resource-group $RESOURCE_GROUP `
  --min-replicas 2 `
  --max-replicas 5

# Scale down to save costs
az containerapp update `
  --name payment-service `
  --resource-group $RESOURCE_GROUP `
  --min-replicas 0 `
  --max-replicas 1
```

---

## 💰 Cost Management

### Monitor Spending
```powershell
# View current costs
az consumption usage list `
  --start-date 2026-03-01 `
  --end-date 2026-03-31 `
  --output table

# Set budget alert (optional)
az consumption budget create `
  --budget-name "StudentBudget" `
  --amount 50 `
  --time-grain Monthly `
  --start-date 2026-03-01 `
  --end-date 2027-03-01
```

### Cost Optimization Checklist
- [ ] Scale services to 0 when not in use (`min-replicas 0`)
- [ ] Use Basic tier for SQL Database (not Standard/Premium)
- [ ] Use Basic tier for Redis (not Standard)
- [ ] Delete unused resources immediately
- [ ] Stop services during development/testing hours
- [ ] Use Log Analytics sparingly (limited free tier)
- [ ] Monitor daily spending in Azure Portal

### Stop All Services (Save Costs)
```powershell
# Stop all container apps (scale to 0)
$services = @(
    "auth-service",
    "userprofile-service",
    "portfolio-service",
    "company-service",
    "community-service",
    "subscription-service",
    "payment-service",
    "notification-service",
    "media-service",
    "application-service",
    "rabbitmq"
)

foreach ($service in $services) {
    az containerapp update `
        --name $service `
        --resource-group $RESOURCE_GROUP `
        --min-replicas 0 `
        --max-replicas 0
    Write-Host "⏸️  Stopped $service" -ForegroundColor Yellow
}

Write-Host "`n✅ All services stopped to save costs" -ForegroundColor Green
```

### Restart All Services
```powershell
foreach ($service in $services) {
    az containerapp update `
        --name $service `
        --resource-group $RESOURCE_GROUP `
        --min-replicas 1 `
        --max-replicas 3
    Write-Host "▶️  Started $service" -ForegroundColor Green
}
```

---

## 🗑️ Cleanup (Delete Everything)

**WARNING: This deletes ALL resources and data!**

```powershell
# Delete entire resource group
az group delete `
  --name $RESOURCE_GROUP `
  --yes `
  --no-wait

Write-Host "🗑️  Resource group deletion started (takes 5-10 minutes)" -ForegroundColor Red
```

---

## 📚 Additional Resources

- **Azure Student Portal**: https://azure.microsoft.com/en-us/free/students/
- **Azure Container Apps Docs**: https://learn.microsoft.com/en-us/azure/container-apps/
- **Azure SQL Database Docs**: https://learn.microsoft.com/en-us/azure/azure-sql/
- **Cost Management**: https://portal.azure.com/#view/Microsoft_Azure_CostManagement/

---

## 🆘 Common Issues

### Issue: "Quota exceeded"
**Solution**: Azure Student limits cores. Use smaller instances (0.5 CPU, 1GB RAM).

### Issue: "Container app fails to start"
**Solution**: Check logs with `az containerapp logs show --name <service> --follow`

### Issue: "Can't connect to SQL"
**Solution**: Check firewall rules, ensure connection string is correct in Key Vault.

### Issue: "High costs"
**Solution**: Scale to 0 when not testing, use Basic tiers, delete unused resources.

### Issue: "Payment webhooks not working"
**Solution**: Update VNPay/MoMo dashboard with new Azure URLs.

---

## ✅ Checklist

Before deploying:
- [ ] Azure Student account activated
- [ ] Azure CLI installed and logged in
- [ ] Docker images build successfully locally
- [ ] .env file has real credentials
- [ ] VNPay/MoMo accounts configured

After deploying:
- [ ] All services show "Running" status
- [ ] Gateway URL is accessible
- [ ] Health endpoints return 200 OK
- [ ] Webhook URLs updated in payment provider dashboards
- [ ] Budget alerts configured
- [ ] Daily cost monitoring enabled

---

**🎉 Congratulations!** Your SkillSnap platform is now running on Azure!

**Gateway URL**: `https://<your-gateway>.azurecontainerapps.io`

**Estimated Monthly Cost**: $34-61 (well within $100 student credit)
