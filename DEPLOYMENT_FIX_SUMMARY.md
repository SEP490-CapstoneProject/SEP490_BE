# Azure Redeploy Fix - Public Services Configuration
**Date:** 2026-04-28 22:06 UTC+7  
**Status:** ✅ Services Now Public & Connected  
**Environment ID:** `redmushroom-1d023c6a`  

---

## Problem Solved

**Issue:** Services were initially set to INTERNAL ingress, preventing gateway from reaching them. Gateway's YARP config pointed to old environment FQDNs (`grayforest-11aba44e`).

**Solution:** 
1. Converted all services to EXTERNAL ingress (public URLs)
2. Updated gateway YARP Clusters to point to new environment public FQDNs
3. Updated Key Vault ServiceUrls secrets with public service URLs

---

## Service Access Configuration

### Public Ingress Enabled
All 14 services now have **EXTERNAL ingress** enabled:

| Service | FQDN | Status |
|---------|------|--------|
| gateway | `gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| auth-service | `auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| userprofile-service | `userprofile-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| portfolio-service | `portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| company-service | `company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| connection-service | `connection-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| community-service | `community-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| subscription-service | `subscription-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| payment-service | `payment-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| notification-service | `notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| media-service | `media-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| application-service | `application-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| realtime-service | `realtime-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |
| interview-service | `interview-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` | ✅ External |

---

## YARP Gateway Configuration

### Updated Cluster Destinations
All YARP reverse proxy clusters in gateway now point to public service FQDNs:

```json
"auth-cluster": {
  "Destinations": {
    "destination1": {
      "Address": "https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"
    }
  }
}
```

**Format Pattern:**
```
https://{service-name}.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
```

All 12 service clusters updated (auth, userprofile, portfolio, company, connection, community, subscription, payment, notification, media, application, realtime).

---

## Key Vault ServiceUrls Secrets

### Updated Secrets (12 total)
All ServiceUrls secrets in Key Vault now reference public FQDNs:

```
ServiceUrls--AuthService: https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--UserProfileService: https://userprofile-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--PortfolioService: https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--CompanyService: https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--ConnectionService: https://connection-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--CommunityService: https://community-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--SubscriptionService: https://subscription-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--NotificationService: https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--RealtimeService: https://realtime-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--MediaService: https://media-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--ApplicationService: https://application-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
ServiceUrls--PaymentService: https://payment-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
```

These secrets are automatically picked up by services on next restart and override appsettings.json defaults.

---

## API Gateway URLs (via Gateway)

Users should access all APIs through the **gateway only** for load balancing and routing:

```
https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/{service}
```

### API Endpoints via Gateway
- **Auth:** `/api/auth`
- **User Profile:** `/api/userprofile`, `/api/employees`, `/api/companies`
- **Portfolio:** `/api/portfolio`, `/api/compliments`
- **Company:** `/api/company-posts`
- **Connection:** `/api/connection`, `/hubs/chat`
- **Community:** `/api/community`, `/api/posts`
- **Subscription:** `/api/subscription`, `/api/subscriptions`, `/api/plans`, `/api/admin/plans`, `/api/admin/subscriptions`, `/api/admin/users`, `/api/admin/analytics`
- **Notifications:** `/api/notifications`
- **Realtime:** `/hubs/realtime`, `/api/realtime`
- **Media:** `/api/media`, `/api/upload`
- **Application:** `/api/applications`
- **Payment:** `/api/payments`, `/api/admin/payments`

---

## Changes Made

### 1. Service Ingress Configuration
```bash
az containerapp ingress enable \
  --resource-group skillsnap-rg-2604282023 \
  --name {service-name} \
  --type external \
  --target-port 8080
```

Applied to all 13 services (excluding gateway which was already external).

### 2. Gateway appsettings.json
**File:** `src/ApiGateway/appsettings.json`

Updated all YARP ReverseProxy Clusters:
- Changed from: `https://{service}.grayforest-11aba44e.southeastasia.azurecontainerapps.io`
- Changed to: `https://{service}.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`

### 3. Key Vault Secrets
Updated 12 ServiceUrls secrets pointing services to public FQDNs.

### 4. Gateway Deployment
Rebuilt and redeployed gateway image with corrected configuration.

---

## Container App Details

| Resource | Value |
|----------|-------|
| **Resource Group** | `skillsnap-rg-2604282023` |
| **Container Apps Env** | `skillsnap-env-2604282023` |
| **Environment ID** | `redmushroom-1d023c6a` |
| **Region** | `southeastasia` |
| **Container Registry** | `skillsnapacr2604282023545.azurecr.io` |
| **Build Tag** | `20260428-public-services` (gateway) |

---

## Key Vault Secrets Inventory

**Total:** 33 secrets

### Configuration Secrets (21)
- `ConnectionStrings--DefaultConnection` (SQL)
- `JwtSettings--Secret` (JWT)
- `Redis--ConnectionString`
- `RabbitMQ--Host, Port, Username, Password`
- `Cloudinary--CloudName, ApiKey, ApiSecret`
- `VNPay--TmnCode, HashSecret`
- `MoMo--PartnerCode, AccessKey, SecretKey`
- `PayOS--ClientId, ApiKey, ChecksumKey`
- `Azure__KeyVault__Url`

### ServiceUrls Secrets (12)
- `ServiceUrls--{ServiceName}` (pointing to public FQDNs)

---

## Testing

### Gateway Health Check
```
✅ GET https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/health
Response: Service is running
```

### Service Connectivity
- ✅ Gateway can reach all public services via FQDN
- ✅ Services can reach each other via ServiceUrls from Key Vault
- ✅ All services report "Running" status

---

## Security Notes

**⚠️ All services now exposed publicly on internet**

This configuration allows external access to backend services. For production, consider:
1. Implementing API authentication/authorization on each service
2. Setting up Azure API Management for rate limiting and security
3. Restricting gateway ingress to specific IP ranges
4. Using managed identities and RBAC for Azure resource access
5. Implementing WAF (Web Application Firewall) on gateway

---

## Next Steps

1. ✅ **Services Online:** All services are now discoverable and accessible
2. 🔄 **Load Testing:** Verify services can handle concurrent requests
3. 🔄 **Notification Tests:** Test system and community notification flows
4. 🔄 **Realtime Tests:** Test WebSocket connections for realtime service
5. 🔄 **Integration Tests:** Run end-to-end business logic tests
6. 🔄 **Production Hardening:** Implement security best practices

---

## Recovery Commands

If services need to be restarted:

```bash
# Restart all services
az containerapp list -g skillsnap-rg-2604282023 --query "[].name" -o tsv | \
  xargs -I {} az containerapp revision list -g skillsnap-rg-2604282023 -n {} --query "[0].name" -o tsv | \
  xargs -I {} az containerapp revision restart -g skillsnap-rg-2604282023 -n {} --revision {}

# Update gateway with latest image
az containerapp update -g skillsnap-rg-2604282023 -n gateway \
  --image skillsnapacr2604282023545.azurecr.io/gateway:20260428-public-services

# Verify all are running
az containerapp list -g skillsnap-rg-2604282023 --query "[].{name:name, status:properties.runningStatus}" -o table
```

---

**Status:** 🎉 Platform is online and services are inter-connected via public FQDNs.
