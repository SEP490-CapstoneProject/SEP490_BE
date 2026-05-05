# JWT Key Synchronization - Complete Deployment Guide

## Overview

Fixed JWT key inconsistency issues where **Notification service couldn't validate tokens** from Auth service due to:
1. Using wrong Key Vault secret name (`JwtSettings:SecretKey` vs `JwtSettings:Secret`)
2. Disabling Issuer/Audience validation on Community & Company services
3. Inconsistent JWT configuration patterns across services

## What Was Fixed ✅

### Services Modified:
1. **Notification Service** (`src/Services/Notification/Notification.API/Program.cs`)
2. **Community Service** (`src/Services/Community/Community.API/Program.cs`)
3. **Company Service** (`src/Services/Company/Company.API/Program.cs`)

### Changes Applied:
- ✅ All services now use `JwtSettings` class from `RecruitmentPlatform.Common`
- ✅ Standardized Key Vault configuration key: `JwtSettings:Secret`
- ✅ Enabled full JWT validation on all services:
  - Validate Issuer Signing Key ✓
  - Validate Issuer (must be "SkillSnapAuth") ✓
  - Validate Audience (must be "SkillSnapUsers") ✓
  - Validate Lifetime ✓
- ✅ Removed inconsistent conditional logic for JWT setup

### Code Changes Summary:
```csharp
// BEFORE (Inconsistent - Notification Service Example)
var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "default-key";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "SkillSnapAuth";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "SkillSnapUsers";

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true
    };
    options.Events = new JwtBearerEvents { OnMessageReceived = context => Task.CompletedTask };
});

// AFTER (Consistent - All Services Now)
using RecruitmentPlatform.Common;

var jwtSettings = builder.Configuration.GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings
{
    Secret = "default-secret-key-32-characters!",
    Issuer = "SkillSnapAuth",
    Audience = "SkillSnapUsers"
};
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true
        };
    });
```

## Build Status ✅

All services compiled successfully:

```
🐳 Docker Images Built:
  ✓ notification-service:20260429132840
  ✓ community-service:20260429132840
  ✓ company-service:20260429132840
```

## Deployment Steps

### Step 1: Authenticate with Azure

```powershell
# Login to Azure
az login

# Verify login
az account show --query name

# Login to Container Registry
az acr login --name skillsnapregistry
```

### Step 2: Push Docker Images

```powershell
$tag = "20260429132840"

Write-Host "Pushing Docker images to Azure Container Registry..."

docker push "skillsnapregistry.azurecr.io/notification-service:$tag"
docker push "skillsnapregistry.azurecr.io/community-service:$tag"
docker push "skillsnapregistry.azurecr.io/company-service:$tag"
```

### Step 3: Deploy to Azure Container Apps

```powershell
$tag = "20260429132840"
$resourceGroup = "CapStone"

Write-Host "Deploying services to Azure Container Apps..."

# Deploy Notification Service
az containerapp update `
  --name notification-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/notification-service:$tag"

# Deploy Community Service
az containerapp update `
  --name community-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/community-service:$tag"

# Deploy Company Service
az containerapp update `
  --name company-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/company-service:$tag"
```

### Step 4: Verify Deployment

```powershell
# Check deployment status
az containerapp show --name notification-service --resource-group CapStone --query "properties.provisioningState"
az containerapp show --name community-service --resource-group CapStone --query "properties.provisioningState"
az containerapp show --name company-service --resource-group CapStone --query "properties.provisioningState"

# Expected: "Succeeded"
```

## Testing JWT Token Validation

### Automated Test Script

Run the test script to verify JWT consistency:

```powershell
# Run with default ports
.\JWT_VALIDATION_TEST_SCRIPT.ps1

# Or specify custom URLs
.\JWT_VALIDATION_TEST_SCRIPT.ps1 `
  -AuthServiceUrl "https://auth-service.example.com/api/auth" `
  -NotificationServiceUrl "https://notification-service.example.com/api/notifications" `
  -CommunityServiceUrl "https://community-service.example.com/api/community" `
  -CompanyServiceUrl "https://company-service.example.com/api/company-posts"
```

### Manual Test Steps

#### 1. Obtain JWT Token
```bash
curl -X POST http://auth-service/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email":"test@example.com",
    "password":"password123"
  }' | jq '.token'
```

#### 2. Test Notification Service
```bash
TOKEN="<token-from-above>"

curl -X GET http://notification-service/api/notifications \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK (if authenticated)
# Not Expected: 401 Unauthorized
```

#### 3. Test Community Service
```bash
curl -X GET http://community-service/api/community/posts \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK
```

#### 4. Test Company Service
```bash
curl -X GET http://company-service/api/company-posts \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 OK
```

#### 5. Test Invalid Token Rejection
```bash
curl -X GET http://notification-service/api/notifications \
  -H "Authorization: Bearer invalid.token.here"

# Expected: 401 Unauthorized
```

## Azure Key Vault Configuration

Ensure the following secrets are configured in Azure Key Vault:

```
Name: JwtSettings--Secret
Value: <your-32-character-secret-key>

Name: JwtSettings--Issuer
Value: SkillSnapAuth

Name: JwtSettings--Audience
Value: SkillSnapUsers
```

## Expected Results ✅

After deployment, you should see:

1. **All services running**: Status = "Running"
2. **No 401 errors**: Valid tokens accepted by all services
3. **Consistent validation**: Same JWT rules applied to all services
4. **Invalid tokens rejected**: Malformed tokens get 401 Unauthorized
5. **Moderation working**: Community & Company post moderation accepts validated tokens

## Troubleshooting

### Issue: 401 Unauthorized on all services

**Cause**: JWT secret in Key Vault doesn't match the one used by Auth service

**Solution**:
1. Verify Auth service is running correctly
2. Check Key Vault has `JwtSettings:Secret` with correct value
3. Restart all services to reload configuration

### Issue: Services stuck in "Provisioning"

**Cause**: Image push failed or container can't start

**Solution**:
1. Check container logs: `az containerapp logs show --name <service> --resource-group CapStone`
2. Verify image exists in registry: `az acr repository show --name skillsnapregistry --repository <service>`
3. Rollback to previous version (see below)

### Issue: Some services accept token, others don't

**Cause**: Configuration inconsistency (this should be fixed now)

**Solution**:
1. Verify all services deployed the latest image
2. Check Key Vault configuration is correct
3. Review logs for JWT validation errors

## Rollback Plan

If anything goes wrong, restore previous working version:

```powershell
$previousTag = "20260429132507"  # Your previous stable tag
$resourceGroup = "CapStone"

Write-Host "Rolling back to tag: $previousTag"

az containerapp update `
  --name notification-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/notification-service:$previousTag"

az containerapp update `
  --name community-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/community-service:$previousTag"

az containerapp update `
  --name company-service `
  --resource-group $resourceGroup `
  --image "skillsnapregistry.azurecr.io/company-service:$previousTag"

Write-Host "Rollback complete"
```

## Files Changed

| File | Changes | Impact |
|------|---------|--------|
| `src/Services/Notification/Notification.API/Program.cs` | Standardized JWT config, added Common reference | ✅ Token validation works |
| `src/Services/Notification/Notification.API/Notification.API.csproj` | Added RecruitmentPlatform.Common reference | ✅ Compilation |
| `src/Services/Community/Community.API/Program.cs` | Standardized JWT config, enable validation | ✅ Consistent behavior |
| `src/Services/Community/Community.API/Community.API.csproj` | (No changes needed - already had Common) | ✅ |
| `src/Services/Company/Company.API/Program.cs` | Standardized JWT config, enable validation | ✅ Consistent behavior |
| `src/Services/Company/Company.API/Company.API.csproj` | Added RecruitmentPlatform.Common reference | ✅ Compilation |

## Documentation

| File | Purpose |
|------|---------|
| `JWT_KEY_SYNCHRONIZATION_FIX.md` | Technical details of JWT fix |
| `JWT_DEPLOYMENT_TESTING_PLAN.md` | Deployment and testing procedures |
| `JWT_VALIDATION_TEST_SCRIPT.ps1` | Automated validation test script |
| `JWT_FIX_DEPLOYMENT_GUIDE.md` | This file |

## Success Metrics

✅ **Deployment Successful If:**
- All 3 services reach "Running" state
- Services stay running > 5 minutes without restarting
- Valid JWT tokens are accepted by all services
- Invalid tokens are rejected with 401 Unauthorized
- Notification service can validate tokens from Auth service
- Community/Company services accept moderated post requests

## Support

For issues or questions:
1. Check service logs: `az containerapp logs show --name <service> --resource-group CapStone`
2. Verify Key Vault configuration
3. Run test script to identify specific service issues
4. Review JWT_KEY_SYNCHRONIZATION_FIX.md for technical details
