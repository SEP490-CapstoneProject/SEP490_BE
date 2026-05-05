# Deployment & Testing Plan - JWT Key Synchronization

## Build Status ✅

Successfully built all 3 services with JWT key fixes:

### Docker Images Built:
- ✅ `skillsnapregistry.azurecr.io/notification-service:20260429132840`
- ✅ `skillsnapregistry.azurecr.io/community-service:20260429132840`
- ✅ `skillsnapregistry.azurecr.io/company-service:20260429132840`

## Next Steps: Azure Deployment

### 1. Authenticate with Azure Container Registry
```powershell
# Login to Azure
az login

# Login to Container Registry
az acr login --name skillsnapregistry

# Verify login
az acr repository list --name skillsnapregistry --output table
```

### 2. Push Images to Registry
```powershell
$tag = "20260429132840"

docker push "skillsnapregistry.azurecr.io/notification-service:$tag"
docker push "skillsnapregistry.azurecr.io/community-service:$tag"
docker push "skillsnapregistry.azurecr.io/company-service:$tag"
```

### 3. Deploy to Azure Container Apps
```powershell
$tag = "20260429132840"

# Deploy Notification Service
az containerapp update `
  --name notification-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/notification-service:$tag"

# Deploy Community Service
az containerapp update `
  --name community-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/community-service:$tag"

# Deploy Company Service
az containerapp update `
  --name company-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/company-service:$tag"
```

### 4. Wait for Deployment
```powershell
# Check status
az containerapp show --name notification-service --resource-group CapStone --query "properties.provisioningState"
az containerapp show --name community-service --resource-group CapStone --query "properties.provisioningState"
az containerapp show --name company-service --resource-group CapStone --query "properties.provisioningState"
```

## Testing Plan

### Test 1: JWT Token Validation (All Services)
**Objective**: Verify all services accept JWT tokens with consistent validation

```bash
# 1. Get Auth Token from Auth Service
curl -X POST http://auth-service/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}'

# Response: { "token": "eyJhbGc..." }

# 2. Use token to call Notification Service
curl -X GET http://notification-service/api/notifications \
  -H "Authorization: Bearer eyJhbGc..."

# Expected: 200 OK (or 401 Unauthorized if token invalid)

# 3. Use same token to call Community Service
curl -X GET http://community-service/api/community/posts \
  -H "Authorization: Bearer eyJhbGc..."

# Expected: 200 OK

# 4. Use same token to call Company Service
curl -X GET http://company-service/api/company-posts \
  -H "Authorization: Bearer eyJhbGc..."

# Expected: 200 OK
```

### Test 2: Issuer/Audience Validation
**Objective**: Verify invalid issuer/audience tokens are rejected

```bash
# Test with malformed token
curl -X GET http://notification-service/api/notifications \
  -H "Authorization: Bearer invalid.token.here"

# Expected: 401 Unauthorized
```

### Test 3: Create Post with Moderation (Community)
**Objective**: Verify community post moderation works with new JWT

```bash
# Create post with validated token
curl -X POST http://community-service/api/community/posts \
  -H "Authorization: Bearer <valid-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Post",
    "description": "This is a test post with quality content",
    "isAnonymous": false
  }'

# Expected: 201 Created or 202 Accepted (for pending review)
```

### Test 4: JWT Key Consistency Check
**Objective**: Verify all services use same JWT key from Key Vault

```powershell
# Check all services are reading from same Key Vault
$services = @("notification-service", "community-service", "company-service")

foreach ($service in $services) {
    Write-Host "Checking $service JWT config..."
    
    # Get service logs to verify key is loaded
    az containerapp logs show --name $service `
      --resource-group CapStone `
      --tail 50 | grep -i "jwt\|auth"
}
```

## Expected Results

### ✅ Success Criteria
1. All services deploy without errors
2. Services reach "Running" state in 2-5 minutes
3. Tokens from Auth service are accepted by all 3 services
4. No "401 Unauthorized" errors for valid tokens
5. Community/Company posts with moderation work correctly
6. Admin can approve/reject pending posts
7. Notifications are sent on post moderation events

### ❌ Failure Indicators
- Services stuck in "Provisioning" state > 10 minutes
- 401 Unauthorized errors when calling services with valid token
- JWT validation errors in service logs
- Inconsistent token acceptance across services

## Rollback Plan

If tests fail, restore previous deployment:

```powershell
$previousTag = "20260429132507"  # Or your previous stable tag

az containerapp update `
  --name notification-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/notification-service:$previousTag"

az containerapp update `
  --name community-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/community-service:$previousTag"

az containerapp update `
  --name company-service `
  --resource-group CapStone `
  --image "skillsnapregistry.azurecr.io/company-service:$previousTag"
```

## Key Changes Summary

| Service | Changes | Impact |
|---------|---------|--------|
| **Notification** | Uses `JwtSettings` class, full Issuer/Audience validation | ✅ Tokens now validated properly |
| **Community** | Uses `JwtSettings` class, enables Issuer/Audience validation | ✅ Consistent validation |
| **Company** | Uses `JwtSettings` class, enables Issuer/Audience validation | ✅ Consistent validation |

All services now:
- Read JWT Secret from Key Vault: `JwtSettings:Secret`
- Read JWT Issuer from Key Vault: `JwtSettings:Issuer`
- Read JWT Audience from Key Vault: `JwtSettings:Audience`
- Validate all fields: IssuerSigningKey + Issuer + Audience + Lifetime

## Time Estimate

- Push to Registry: 3-5 minutes
- Deploy to Azure: 2-5 minutes per service
- Health check: 1-2 minutes
- Full testing: 10-15 minutes
- **Total**: ~30 minutes
