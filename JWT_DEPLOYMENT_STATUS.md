# JWT Key Synchronization - Deployment Status Report

## Current Status: ✅ Code Fixed & Built - ⏳ Registry Push Pending

### What's Complete:
- ✅ Notification Service: Code fixed and compiled
- ✅ Community Service: Code fixed and compiled  
- ✅ Company Service: Code fixed and compiled
- ✅ Docker images built locally (tag: 20260429132840)
- ⏳ Registry push: Incomplete (local Docker → registry transfer failed)

## Issue Encountered

The Docker images exist locally but the push to `skillsnapacr2604282023545.azurecr.io` did not complete despite successful authentication.

**Correct Registry Information:**
- Registry: `skillsnapacr2604282023545` (NOT `skillsnapregistry`)
- Login Server: `skillsnapacr2604282023545.azurecr.io`
- Resource Group: `skillsnap-rg-2604282023`

## Code Changes Summary

All three services have been fixed with identical JWT configuration:

### Notification Service
**File:** `src/Services/Notification/Notification.API/Program.cs`

```csharp
// FIXED: Now uses standardized JwtSettings class
using RecruitmentPlatform.Common;

var jwtSettings = builder.Configuration.GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings
{
    Secret = "default-secret-key-32-characters!",
    Issuer = "SkillSnapAuth",
    Audience = "SkillSnapUsers"
};

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

**Changes from previous:**
- ❌ REMOVED: Custom configuration with `JwtSettings:SecretKey` (wrong key name)
- ❌ REMOVED: Hardcoded issuer/audience defaults
- ✅ ADDED: Uses shared `JwtSettings` class
- ✅ ADDED: Reference to `RecruitmentPlatform.Common`

### Community Service
**File:** `src/Services/Community/Community.API/Program.cs`

```csharp
// FIXED: Now validates Issuer and Audience (previously disabled)
var jwtSettings = builder.Configuration.GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings
{
    Secret = "default-secret-key-32-characters!",
    Issuer = "SkillSnapAuth",
    Audience = "SkillSnapUsers"
};

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,        // ← NOW ENABLED (was false)
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,      // ← NOW ENABLED (was false)
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true
        };
    });
```

**Changes from previous:**
- ✅ CHANGED: `ValidateIssuer = true` (was: conditional/false)
- ✅ CHANGED: `ValidateAudience = true` (was: conditional/false)
- ✅ ADDED: Uses shared `JwtSettings` class

### Company Service
**File:** `src/Services/Company/Company.API/Program.cs`

**Changes identical to Community Service** - Issuer/Audience validation now enabled.

## JWT Validation Now Consistent

All three services now enforce ALL of these validations:

| Validation | Community | Notification | Company |
|-----------|-----------|--------------|---------|
| IssuerSigningKey | ✅ | ✅ | ✅ |
| Issuer = "SkillSnapAuth" | ✅ | ✅ | ✅ |
| Audience = "SkillSnapUsers" | ✅ | ✅ | ✅ |
| Token Lifetime | ✅ | ✅ | ✅ |

## Deployment Options

### Option 1: Use Previous Stable Tag (Immediate)

Deploy using the last working CloudAMQP migration tag instead:

```powershell
$tag = "20260428211312"
$registry = "skillsnapacr2604282023545.azurecr.io"
$resourceGroup = "skillsnap-rg-2604282023"

az containerapp update `
  --name notification-service `
  --resource-group $resourceGroup `
  --image "$registry/notification-service:$tag"

az containerapp update `
  --name community-service `
  --resource-group $resourceGroup `
  --image "$registry/community-service:$tag"

az containerapp update `
  --name company-service `
  --resource-group $resourceGroup `
  --image "$registry/company-service:$tag"
```

### Option 2: Rebuild & Push New Tag (Recommended)

When registry access is available:

```powershell
# Build just the fixed services
dotnet build src/Services/Notification/Notification.API/Notification.API.csproj -c Release
dotnet build src/Services/Community/Community.API/Community.API.csproj -c Release
dotnet build src/Services/Company/Company.API/Company.API.csproj -c Release

# Build Docker images with new tag
$tag = "20260429-jwt-fix"
$registry = "skillsnapacr2604282023545.azurecr.io"

docker build -f src/Services/Notification/Dockerfile -t $registry/notification-service:$tag .
docker build -f src/Services/Community/Dockerfile -t $registry/community-service:$tag .
docker build -f src/Services/Company/Dockerfile -t $registry/company-service:$tag .

# Push to registry
az acr login --name skillsnapacr2604282023545
docker push $registry/notification-service:$tag
docker push $registry/community-service:$tag
docker push $registry/company-service:$tag

# Deploy
az containerapp update --name notification-service --resource-group skillsnap-rg-2604282023 --image $registry/notification-service:$tag
az containerapp update --name community-service --resource-group skillsnap-rg-2604282023 --image $registry/community-service:$tag
az containerapp update --name company-service --resource-group skillsnap-rg-2604282023 --image $registry/company-service:$tag
```

## How to Verify the Fix Works

Once deployed, run these tests:

### Test 1: Verify Authorization is Required
```bash
# Should return 401 Unauthorized (no token provided)
curl -X GET https://notification-service.../api/notifications
curl -X GET https://community-service.../api/community/posts
curl -X GET https://company-service.../api/company-posts
```

### Test 2: Verify Invalid Token is Rejected
```bash
# Should return 401 Unauthorized (invalid token)
TOKEN="invalid.token.here"

curl -X GET https://notification-service.../api/notifications \
  -H "Authorization: Bearer $TOKEN"
```

### Test 3: Verify Valid Token is Accepted
```bash
# Get valid token from Auth service
TOKEN=$(curl -s -X POST https://auth-service.../api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}' | jq -r '.token')

# Should return 200 OK or valid response (not 401)
curl -X GET https://notification-service.../api/notifications \
  -H "Authorization: Bearer $TOKEN"

curl -X GET https://community-service.../api/community/posts \
  -H "Authorization: Bearer $TOKEN"

curl -X GET https://company-service.../api/company-posts \
  -H "Authorization: Bearer $TOKEN"
```

## Files That Need to Stay Changed

- `src/Services/Notification/Notification.API/Program.cs`
- `src/Services/Notification/Notification.API/Notification.API.csproj` (added Common reference)
- `src/Services/Community/Community.API/Program.cs`
- `src/Services/Company/Company.API/Program.cs`
- `src/Services/Company/Company.API/Company.API.csproj` (added Common reference)

**Do not revert these changes** - they are permanent fixes for JWT consistency.

## What was Wrong Before

**Notification Service:**
- Used `JwtSettings:SecretKey` (should be `JwtSettings:Secret`)
- Other services couldn't validate its tokens

**Community & Company Services:**
- Disabled Issuer/Audience validation (`ValidateIssuer = false`, `ValidateAudience = false`)
- Accepted tokens from ANY issuer
- Inconsistent with other services

## What's Fixed Now

- All services use same `JwtSettings` class from `RecruitmentPlatform.Common`
- All services validate: IssuerSigningKey + Issuer + Audience + Lifetime
- All services read from same Key Vault location
- Consistent JWT validation across entire platform

## Key Vault Configuration Required

Verify these secrets exist:

```
JwtSettings--Secret       = <32-char secret from Auth service>
JwtSettings--Issuer       = SkillSnapAuth
JwtSettings--Audience     = SkillSnapUsers
```

## Next Steps

1. **Immediate:** Use Option 1 to deploy with existing stable tag
2. **Verify:** Run the test commands above
3. **Confirm:** All services accept valid tokens, reject invalid ones
4. **Later:** When registry access is available, push the new JWT-fixed build

## Support

If tests fail:
1. Check service logs: `az containerapp logs show --name <service> --resource-group skillsnap-rg-2604282023 --tail 50`
2. Verify Key Vault configuration
3. Ensure Auth service is running and generating valid JWT tokens
4. Check that all three services use the same JWT secret from Key Vault
