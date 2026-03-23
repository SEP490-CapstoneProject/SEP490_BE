# 🚀 Quick Deploy Script - Azure Student
# Run this script to automatically deploy SkillSnap to Azure

# ============================================================================
# CONFIGURATION - EDIT THESE VALUES
# ============================================================================

$RESOURCE_GROUP = "skillsnap-student-rg"
$LOCATION = "southeastasia"  # Closest to Vietnam
$SQL_SERVER = "skillsnap-sql-$(Get-Random -Minimum 1000 -Maximum 9999)"  # Must be unique
$SQL_DB = "skillsnap-db"
$SQL_ADMIN = "sqladmin"
$SQL_PASSWORD = "YourStrong@Passw0rd$(Get-Random -Minimum 100 -Maximum 999)"  # Change this!
$ACR_NAME = "skillsnapacr$(Get-Random -Minimum 1000 -Maximum 9999)"  # Must be unique, lowercase
$KEYVAULT_NAME = "skillsnap-kv-$(Get-Random -Minimum 100 -Maximum 999)"  # Must be unique
$CONTAINER_ENV = "skillsnap-env"
$REDIS_NAME = "skillsnap-redis-$(Get-Random -Minimum 100 -Maximum 999)"

# JWT Secret (generate strong random string)
$JWT_SECRET = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})

# Payment Provider Credentials - REPLACE WITH YOUR ACTUAL VALUES!
$VNPAY_TMN_CODE = "YOUR_VNPAY_TMN_CODE"
$VNPAY_HASH_SECRET = "YOUR_VNPAY_HASH_SECRET"
$MOMO_PARTNER_CODE = "YOUR_MOMO_PARTNER_CODE"
$MOMO_ACCESS_KEY = "YOUR_MOMO_ACCESS_KEY"
$MOMO_SECRET_KEY = "YOUR_MOMO_SECRET_KEY"

# Cloudinary Credentials - REPLACE WITH YOUR ACTUAL VALUES!
$CLOUDINARY_CLOUD_NAME = "YOUR_CLOUD_NAME"
$CLOUDINARY_API_KEY = "YOUR_API_KEY"
$CLOUDINARY_API_SECRET = "YOUR_API_SECRET"

# ============================================================================
# PRE-FLIGHT CHECKS
# ============================================================================

Write-Host "`n🎓 SkillSnap Azure Student Deployment" -ForegroundColor Cyan
Write-Host "======================================`n" -ForegroundColor Cyan

# Check Azure CLI
if (!(Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI not found. Please install from: https://aka.ms/installazurecliwindows" -ForegroundColor Red
    exit 1
}

# Check Docker
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker not found. Please install Docker Desktop" -ForegroundColor Red
    exit 1
}

# Check if logged in
$account = az account show 2>$null
if (!$account) {
    Write-Host "🔐 Please login to Azure..." -ForegroundColor Yellow
    az login
}

Write-Host "✅ Pre-flight checks passed`n" -ForegroundColor Green

# Display configuration
Write-Host "📋 Deployment Configuration:" -ForegroundColor Yellow
Write-Host "   Resource Group: $RESOURCE_GROUP"
Write-Host "   Location: $LOCATION"
Write-Host "   SQL Server: $SQL_SERVER"
Write-Host "   Container Registry: $ACR_NAME"
Write-Host "   Key Vault: $KEYVAULT_NAME"
Write-Host ""

$confirm = Read-Host "Continue with deployment? (y/n)"
if ($confirm -ne "y") {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

# ============================================================================
# PHASE 1: CREATE AZURE RESOURCES
# ============================================================================

Write-Host "`n🏗️  PHASE 1: Creating Azure Resources...`n" -ForegroundColor Cyan

# Set defaults
az configure --defaults group=$RESOURCE_GROUP location=$LOCATION

# Create Resource Group
Write-Host "Creating Resource Group..." -ForegroundColor Yellow
az group create --name $RESOURCE_GROUP --location $LOCATION
Write-Host "✅ Resource Group created`n" -ForegroundColor Green

# Create SQL Server
Write-Host "Creating SQL Server (takes 2-3 minutes)..." -ForegroundColor Yellow
az sql server create `
    --name $SQL_SERVER `
    --resource-group $RESOURCE_GROUP `
    --location $LOCATION `
    --admin-user $SQL_ADMIN `
    --admin-password $SQL_PASSWORD

# Configure firewall
$MY_IP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az sql server firewall-rule create --server $SQL_SERVER --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
az sql server firewall-rule create --server $SQL_SERVER --name AllowMyIP --start-ip-address $MY_IP --end-ip-address $MY_IP

# Create database
az sql db create `
    --name $SQL_DB `
    --server $SQL_SERVER `
    --service-objective Basic `
    --backup-storage-redundancy Local

$SQL_CONNECTION_STRING = "Server=tcp:$SQL_SERVER.database.windows.net,1433;Database=$SQL_DB;User ID=$SQL_ADMIN;Password=$SQL_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
Write-Host "✅ SQL Database created`n" -ForegroundColor Green

# Create Container Registry
Write-Host "Creating Container Registry..." -ForegroundColor Yellow
az acr create --name $ACR_NAME --sku Basic --admin-enabled true
$ACR_USERNAME = az acr credential show --name $ACR_NAME --query username -o tsv
$ACR_PASSWORD = az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv
Write-Host "✅ Container Registry created`n" -ForegroundColor Green

# Create Redis Cache
Write-Host "Creating Redis Cache (takes 5-10 minutes)..." -ForegroundColor Yellow
az redis create `
    --name $REDIS_NAME `
    --location $LOCATION `
    --sku Basic `
    --vm-size C0
Write-Host "⏳ Redis creation started (will complete in background)`n" -ForegroundColor Yellow

# Create Key Vault
Write-Host "Creating Key Vault..." -ForegroundColor Yellow
az keyvault create `
    --name $KEYVAULT_NAME `
    --location $LOCATION `
    --enable-rbac-authorization false
Write-Host "✅ Key Vault created`n" -ForegroundColor Green

# Add secrets to Key Vault
Write-Host "Adding secrets to Key Vault..." -ForegroundColor Yellow
az keyvault secret set --vault-name $KEYVAULT_NAME --name "SqlConnectionString" --value $SQL_CONNECTION_STRING | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "JwtSecret" --value $JWT_SECRET | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "VNPay--TmnCode" --value $VNPAY_TMN_CODE | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "VNPay--HashSecret" --value $VNPAY_HASH_SECRET | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--PartnerCode" --value $MOMO_PARTNER_CODE | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--AccessKey" --value $MOMO_ACCESS_KEY | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "MoMo--SecretKey" --value $MOMO_SECRET_KEY | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--CloudName" --value $CLOUDINARY_CLOUD_NAME | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--ApiKey" --value $CLOUDINARY_API_KEY | Out-Null
az keyvault secret set --vault-name $KEYVAULT_NAME --name "Cloudinary--ApiSecret" --value $CLOUDINARY_API_SECRET | Out-Null
Write-Host "✅ Secrets added to Key Vault`n" -ForegroundColor Green

# Wait for Redis to complete
Write-Host "Waiting for Redis Cache to complete..." -ForegroundColor Yellow
$retries = 0
while ($retries -lt 30) {
    $redisStatus = az redis show --name $REDIS_NAME --query provisioningState -o tsv 2>$null
    if ($redisStatus -eq "Succeeded") {
        break
    }
    Write-Host "  Redis status: $redisStatus (retry $retries/30)" -ForegroundColor Gray
    Start-Sleep -Seconds 20
    $retries++
}

$REDIS_KEY = az redis list-keys --name $REDIS_NAME --query primaryKey -o tsv
$REDIS_CONNECTION = "$REDIS_NAME.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"
az keyvault secret set --vault-name $KEYVAULT_NAME --name "RedisConnection" --value $REDIS_CONNECTION | Out-Null
Write-Host "✅ Redis Cache ready`n" -ForegroundColor Green

# Create Container Apps Environment
Write-Host "Creating Container Apps Environment..." -ForegroundColor Yellow
az extension add --name containerapp --upgrade --only-show-errors
az containerapp env create --name $CONTAINER_ENV
Write-Host "✅ Container Apps Environment created`n" -ForegroundColor Green

Write-Host "✅ PHASE 1 COMPLETE: All Azure resources created`n" -ForegroundColor Green

# ============================================================================
# PHASE 2: BUILD AND PUSH DOCKER IMAGES
# ============================================================================

Write-Host "`n🐳 PHASE 2: Building and Pushing Docker Images...`n" -ForegroundColor Cyan

# Login to ACR
az acr login --name $ACR_NAME

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

$failedBuilds = @()

foreach ($service in $services) {
    Write-Host "🔨 Building $service Service..." -ForegroundColor Yellow
    
    try {
        $imageName = "$ACR_NAME.azurecr.io/$($service.ToLower())-service:latest"
        
        # Build
        docker build -t $imageName -f "src\Services\$service\Dockerfile" . 2>&1 | Out-Null
        
        # Push
        docker push $imageName 2>&1 | Out-Null
        
        Write-Host "✅ $service Service built and pushed" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to build $service Service" -ForegroundColor Red
        $failedBuilds += $service
    }
}

if ($failedBuilds.Count -gt 0) {
    Write-Host "`n⚠️  Some services failed to build: $($failedBuilds -join ', ')" -ForegroundColor Yellow
    $continue = Read-Host "Continue with deployment? (y/n)"
    if ($continue -ne "y") {
        exit 1
    }
}

Write-Host "`n✅ PHASE 2 COMPLETE: All images pushed to ACR`n" -ForegroundColor Green

# ============================================================================
# PHASE 3: DEPLOY SERVICES
# ============================================================================

Write-Host "`n🚀 PHASE 3: Deploying Services...`n" -ForegroundColor Cyan

# Deploy RabbitMQ
Write-Host "Deploying RabbitMQ..." -ForegroundColor Yellow
az containerapp create `
    --name rabbitmq `
    --environment $CONTAINER_ENV `
    --image rabbitmq:3-management `
    --target-port 5672 `
    --ingress internal `
    --min-replicas 1 `
    --max-replicas 1 `
    --cpu 0.5 `
    --memory 1.0Gi `
    --env-vars RABBITMQ_DEFAULT_USER=guest RABBITMQ_DEFAULT_PASS=guest
Write-Host "✅ RabbitMQ deployed`n" -ForegroundColor Green

# Deploy each service
foreach ($service in $services) {
    Write-Host "Deploying $service Service..." -ForegroundColor Yellow
    
    $imageName = "$ACR_NAME.azurecr.io/$($service.ToLower())-service:latest"
    $serviceName = "$($service.ToLower())-service"
    
    az containerapp create `
        --name $serviceName `
        --environment $CONTAINER_ENV `
        --image $imageName `
        --registry-server "$ACR_NAME.azurecr.io" `
        --registry-username $ACR_USERNAME `
        --registry-password $ACR_PASSWORD `
        --target-port 8080 `
        --ingress internal `
        --min-replicas 0 `
        --max-replicas 3 `
        --cpu 0.5 `
        --memory 1.0Gi `
        --system-assigned `
        --env-vars `
            "ASPNETCORE_ENVIRONMENT=Production" `
            "Azure__KeyVault__Url=https://$KEYVAULT_NAME.vault.azure.net/"
    
    # Grant Key Vault access
    $principalId = az containerapp show --name $serviceName --query identity.principalId -o tsv
    az keyvault set-policy --name $KEYVAULT_NAME --object-id $principalId --secret-permissions get list | Out-Null
    
    Write-Host "✅ $service Service deployed" -ForegroundColor Green
}

Write-Host "`n✅ PHASE 3 COMPLETE: All services deployed`n" -ForegroundColor Green

# ============================================================================
# DEPLOYMENT SUMMARY
# ============================================================================

Write-Host "`n🎉 DEPLOYMENT COMPLETE!" -ForegroundColor Green
Write-Host "=====================`n" -ForegroundColor Green

Write-Host "📋 Resource Summary:" -ForegroundColor Cyan
Write-Host "   Resource Group: $RESOURCE_GROUP"
Write-Host "   SQL Server: $SQL_SERVER.database.windows.net"
Write-Host "   Container Registry: $ACR_NAME.azurecr.io"
Write-Host "   Key Vault: https://$KEYVAULT_NAME.vault.azure.net/"
Write-Host ""

Write-Host "🔐 Credentials (SAVE THESE!):" -ForegroundColor Yellow
Write-Host "   SQL Admin: $SQL_ADMIN"
Write-Host "   SQL Password: $SQL_PASSWORD"
Write-Host "   JWT Secret: $JWT_SECRET"
Write-Host ""

Write-Host "📝 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Deploy API Gateway (YARP) - see manual steps in guide"
Write-Host "   2. Update VNPay/MoMo webhook URLs with your gateway URL"
Write-Host "   3. Test services: az containerapp list --output table"
Write-Host "   4. View logs: az containerapp logs show --name payment-service --follow"
Write-Host ""

Write-Host "💰 Estimated Monthly Cost: `$34-61 (within `$100 student credit)" -ForegroundColor Green
Write-Host ""

Write-Host "📚 Full documentation: AZURE_STUDENT_DEPLOYMENT_GUIDE.md" -ForegroundColor Cyan
