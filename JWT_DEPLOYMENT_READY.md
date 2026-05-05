# JWT Services Deployment - Ready for Manual Deployment

## Status: ✅ BUILT & READY

### Build Status
- ✅ All 3 services compiled successfully
- ✅ Docker images built locally (tag: 20260429132840)
- ⏳ Images not yet in Azure Container Registry (requires manual push)

### Services Ready to Deploy
1. **notification-service:20260429132840**
2. **community-service:20260429132840**
3. **company-service:20260429132840**

## Why Manual Deployment is Needed

The Docker images exist only in **local Docker Desktop** and need to be pushed to **Azure Container Registry** before Azure Container Apps can deploy them.

## Complete Deployment Steps

### Step 1: Authenticate with Azure Container Registry

```powershell
# Login to Azure
az login

# Login to Container Registry
az acr login --name skillsnapregistry

# Verify by listing repositories
az acr repository list --name skillsnapregistry --output table
```

### Step 2: Push Docker Images to Registry

```powershell
$tag = "20260429132840"
$registry = "skillsnapregistry.azurecr.io"

Write-Host "Pushing Docker images to Azure Container Registry..."
Write-Host ""

# Push Notification Service
Write-Host "1. Pushing notification-service..."
docker push "$registry/notification-service:$tag"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ notification-service pushed successfully" -ForegroundColor Green
} else {
    Write-Host "❌ notification-service push failed" -ForegroundColor Red
}

Write-Host ""

# Push Community Service
Write-Host "2. Pushing community-service..."
docker push "$registry/community-service:$tag"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ community-service pushed successfully" -ForegroundColor Green
} else {
    Write-Host "❌ community-service push failed" -ForegroundColor Red
}

Write-Host ""

# Push Company Service
Write-Host "3. Pushing company-service..."
docker push "$registry/company-service:$tag"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ company-service pushed successfully" -ForegroundColor Green
} else {
    Write-Host "❌ company-service push failed" -ForegroundColor Red
}

Write-Host ""

# Verify all images are in registry
Write-Host "Verifying images in registry..."
az acr repository show --name skillsnapregistry --repository notification-service
az acr repository show --name skillsnapregistry --repository community-service
az acr repository show --name skillsnapregistry --repository company-service
```

### Step 3: Deploy to Azure Container Apps

Once images are in the registry, deploy them:

```powershell
$tag = "20260429132840"
$resourceGroup = "skillsnap-rg-2604282023"
$registry = "skillsnapregistry.azurecr.io"

Write-Host "Deploying services to Azure Container Apps..."
Write-Host ""

# Deploy Notification Service
Write-Host "1. Deploying notification-service..."
az containerapp update `
  --name notification-service `
  --resource-group $resourceGroup `
  --image "$registry/notification-service:$tag"

# Deploy Community Service
Write-Host "2. Deploying community-service..."
az containerapp update `
  --name community-service `
  --resource-group $resourceGroup `
  --image "$registry/community-service:$tag"

# Deploy Company Service
Write-Host "3. Deploying company-service..."
az containerapp update `
  --name company-service `
  --resource-group $resourceGroup `
  --image "$registry/company-service:$tag"

Write-Host ""
Write-Host "Waiting for deployments to complete (up to 5 minutes)..."
Start-Sleep -Seconds 120

# Check status
Write-Host ""
Write-Host "Checking deployment status..."
az containerapp show --name notification-service --resource-group $resourceGroup --query "properties.provisioningState"
az containerapp show --name community-service --resource-group $resourceGroup --query "properties.provisioningState"
az containerapp show --name company-service --resource-group $resourceGroup --query "properties.provisioningState"
```

### Step 4: Verify Services are Running

```powershell
$resourceGroup = "skillsnap-rg-2604282023"

Write-Host "Service Status:" -ForegroundColor Cyan
Write-Host ""

$services = @(
    "notification-service",
    "community-service",
    "company-service"
)

foreach ($svc in $services) {
    $status = az containerapp show --name $svc --resource-group $resourceGroup --query "properties.provisioningState" -o tsv
    
    $icon = if ($status -eq "Succeeded") { "✅" } else { "⏳" }
    Write-Host "$icon $svc: $status"
    
    # Get service URL
    $fqdn = az containerapp show --name $svc --resource-group $resourceGroup --query "properties.configuration.ingress.fqdn" -o tsv
    if ($fqdn) {
        Write-Host "   https://$fqdn" -ForegroundColor Gray
    }
}
```

### Step 5: Test JWT Token Validation

Run the automated test script:

```powershell
# Make sure you have valid JWT tokens from your Auth service
.\JWT_VALIDATION_TEST_SCRIPT.ps1 `
  -AuthServiceUrl "https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth" `
  -NotificationServiceUrl "https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/notifications" `
  -CommunityServiceUrl "https://community-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/community" `
  -CompanyServiceUrl "https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/company-posts"
```

Or test manually:

```bash
# 1. Get a valid JWT token from Auth service
TOKEN=$(curl -s -X POST https://auth-service.../api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' | jq -r '.token')

# 2. Test Notification Service
curl -X GET https://notification-service.../api/notifications \
  -H "Authorization: Bearer $TOKEN"

# 3. Test Community Service
curl -X GET https://community-service.../api/community/posts \
  -H "Authorization: Bearer $TOKEN"

# 4. Test Company Service
curl -X GET https://company-service.../api/company-posts \
  -H "Authorization: Bearer $TOKEN"

# Expected: All should return 200 or valid data (not 401 Unauthorized)
```

## Troubleshooting

### Issue: "403 Forbidden" when pushing to registry

**Solution:**
```powershell
# Re-authenticate to registry
az acr login --name skillsnapregistry --username <username> --password <password>

# Or use Service Principal
az login --service-principal -u <client-id> -p <client-secret> --tenant <tenant-id>
```

### Issue: "Invalid image" when deploying

**Solution:**
1. Verify image is in registry: `az acr repository show --name skillsnapregistry --repository <service>`
2. Check image tag: `az acr repository show-tags --name skillsnapregistry --repository <service>`
3. Verify permissions: `az role assignment list --resource-group skillsnap-rg-2604282023`

### Issue: "401 Unauthorized" when testing services

**Solution:**
This is expected and correct! It means JWT validation is working. You need a valid token from Auth service.

**Test without authorization header:**
```bash
curl -X GET https://notification-service.../api/notifications
# Should return: 401 Unauthorized (correct)
```

**Test with valid token:**
```bash
curl -X GET https://notification-service.../api/notifications \
  -H "Authorization: Bearer <valid-jwt-token>"
# Should return: 200 OK (if endpoint exists)
```

### Issue: Services stuck in "Provisioning" > 10 minutes

**Solution:**
1. Check container logs: `az containerapp logs show --name <service> --resource-group skillsnap-rg-2604282023 --tail 50`
2. Check events: `az containerapp replica list --name <service> --resource-group skillsnap-rg-2604282023`
3. Rollback to previous image: Use previous tag instead of 20260429132840

## Key Vault Configuration Check

Verify these secrets are in Key Vault `skillsnap-kv`:

```powershell
# Check JWT secrets
az keyvault secret list --vault-name skillsnap-kv --query "[?contains(name, 'JwtSettings')].name" -o table

# Should show:
# - JwtSettings--Secret
# - JwtSettings--Issuer  
# - JwtSettings--Audience
```

## Expected Success Criteria

✅ **Deployment Successful If:**
- All 3 services show "Succeeded" provisioning state
- Services stay running > 5 minutes without restarting
- Valid JWT tokens are accepted by all services
- Invalid/missing tokens get 401 Unauthorized

## Service URLs (After Deployment)

The services will be available at:
- **Notification**: `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
- **Community**: `https://community-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
- **Company**: `https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`

(Exact URLs will be displayed after deployment)

## Rollback Plan

If anything fails, use the previous working tag:

```powershell
$previousTag = "20260429132507"
$resourceGroup = "skillsnap-rg-2604282023"

az containerapp update --name notification-service --resource-group $resourceGroup --image "skillsnapregistry.azurecr.io/notification-service:$previousTag"
az containerapp update --name community-service --resource-group $resourceGroup --image "skillsnapregistry.azurecr.io/community-service:$previousTag"
az containerapp update --name company-service --resource-group $resourceGroup --image "skillsnapregistry.azurecr.io/company-service:$previousTag"
```

## Next Steps

1. ✅ Code is ready (fixed)
2. ✅ Docker images are built
3. ⏳ Push images to registry (Step 2)
4. ⏳ Deploy to Azure (Step 3)
5. ⏳ Test JWT validation (Step 5)
6. ⏳ Monitor for issues

## Documentation

All supporting documents are available:
- `JWT_KEY_SYNCHRONIZATION_FIX.md` - Technical details
- `JWT_FIX_DEPLOYMENT_GUIDE.md` - Full deployment guide
- `JWT_VALIDATION_TEST_SCRIPT.ps1` - Test script
- `JWT_DEPLOYMENT_TESTING_PLAN.md` - Testing procedures
