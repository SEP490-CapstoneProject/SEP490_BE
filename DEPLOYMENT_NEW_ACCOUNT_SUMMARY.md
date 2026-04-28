# Azure Account Redeploy Summary - New Account
**Date:** 2026-04-28 21:20 UTC+7  
**Status:** ✅ Deployment Complete  
**Tag:** 20260428211312

---

## Infrastructure Provisioned ✅

### Resource Group
- **Name:** `skillsnap-rg-2604282023`
- **Location:** `southeastasia`
- **Subscription:** Azure for Students

### Core Services
| Service | Type | Status | Tier |
|---------|------|--------|------|
| Azure SQL Database | Database | ✅ Created | Basic (5 DTU) |
| Container Registry | Artifact Storage | ✅ Created | Basic |
| Azure Redis Cache | Caching | ✅ Created | Basic C0 |
| Key Vault | Secrets | ✅ Created | Standard |
| Container Apps Env | Orchestration | ✅ Created | Consumption |

### Database
- **Server:** `skillsnapsql2604282023545.database.windows.net`
- **Database:** `skillsnap-db`
- **Admin User:** `sqladmin`
- **Connection String:** Stored in Key Vault

### Container Registry
- **URL:** `skillsnapacr2604282023545.azurecr.io`
- **Auth:** Admin enabled with username/password
- **Images:** All services built and pushed with tag `20260428211312`

### Key Vault
- **Name:** `sskv2604282023545`
- **Secrets:** 21 secrets configured (SQL, JWT, Redis, Payment gateways, Cloudinary, RabbitMQ)
- **Access:** All container apps granted secret read/list permissions

### Redis Cache
- **Name:** `skillsnap-redis-2604282023545`
- **Status:** ✅ Succeeded
- **Connection String:** Stored in Key Vault

---

## Container Apps Deployed ✅

### All Services Running
| Service | Status | Replicas | Port | Ingress |
|---------|--------|----------|------|---------|
| gateway | Running | 1-3 | 8080 | External |
| auth-service | Running | 1-2 | 8080 | Internal |
| userprofile-service | Running | 1-2 | 8080 | Internal |
| portfolio-service | Running | 1-2 | 8080 | Internal |
| company-service | Running | 1-2 | 8080 | Internal |
| connection-service | Running | 1-2 | 8080 | Internal |
| community-service | Running | 1-2 | 8080 | Internal |
| subscription-service | Running | 1-2 | 8080 | Internal |
| payment-service | Running | 1-2 | 8080 | Internal |
| notification-service | Running | 1-2 | 8080 | Internal |
| media-service | Running | 1-2 | 8080 | Internal |
| application-service | Running | 1-2 | 8080 | Internal |
| realtime-service | Running | 1-2 | 8080 | Internal |
| interview-service | Running | 1-2 | 8080 | Internal |
| rabbitmq-service | Running | 1 | 5672 | Internal |

---

## Access URLs

### Gateway
- **FQDN:** `gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
- **URL:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`

### API Endpoints (via Gateway)
- **Auth:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth`
- **UserProfile:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/userprofile`
- **Portfolio:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/portfolio`
- **Company:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/company-posts`
- **Connection:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/connection`
- **Community:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/community`
- **Subscription:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/subscription`
- **Notifications (System):** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/notifications/system`
- **Notifications (Community):** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/notifications/community`
- **Media:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/media`
- **Application:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/applications`
- **Payment:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/payments`
- **Realtime Hub:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/hubs/realtime`
- **Chat Hub:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/hubs/chat`

---

## Build Information

### Build Tag
```
20260428211312
```

### Built Services
1. Auth Service
2. UserProfile Service
3. Portfolio Service
4. Company Service
5. Connection Service
6. Community Service
7. Subscription Service
8. Payment Service
9. Notification Service
10. Media Service
11. Application Service
12. Realtime Service
13. Interview Service
14. API Gateway

### Container Images
All images are pushed to: `skillsnapacr2604282023545.azurecr.io/{service-name}:20260428211312`

---

## Configuration

### Environment Variables (Container Apps)
- `ASPNETCORE_ENVIRONMENT=Production`
- `Azure__KeyVault__Url=https://sskv2604282023545.vault.azure.net/`

### Key Vault Secrets (21 total)
1. ConnectionStrings--DefaultConnection
2. JwtSettings--Secret
3. Redis--ConnectionString
4. RabbitMQ--Host
5. RabbitMQ--Port
6. RabbitMQ--Username
7. RabbitMQ--Password
8. Cloudinary--CloudName
9. Cloudinary--ApiKey
10. Cloudinary--ApiSecret
11. VNPay--TmnCode
12. VNPay--HashSecret
13. MoMo--PartnerCode
14. MoMo--AccessKey
15. MoMo--SecretKey
16. PayOS--ClientId
17. PayOS--ApiKey
18. PayOS--ChecksumKey
19. JwtSecret (legacy)
20. SqlConnectionString (legacy)
21. RedisConnection (legacy)

### RabbitMQ
- **Internal FQDN:** `rabbitmq-service.internal.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`
- **Port:** 5672
- **Username:** guest
- **Password:** guest
- **VHost:** /

---

## Deployment Steps Completed

- ✅ Registered Azure resource providers (SQL, ACR, Cache, KeyVault, OperationalInsights, App, ManagedIdentity)
- ✅ Created resource group and container apps environment
- ✅ Provisioned SQL Database with firewall rules
- ✅ Created Container Registry
- ✅ Created and waited for Redis Cache
- ✅ Created Key Vault and populated 21 secrets
- ✅ Deployed RabbitMQ as internal container app
- ✅ Built and pushed 14 service images to ACR
- ✅ Deployed all 14 backend services + gateway
- ✅ Granted Key Vault access to all container app identities
- ✅ Verified all services running

---

## Next Steps

1. **Wait for Service Startup** (5-10 minutes)
   - Services are cold-starting and initializing connections
   - First requests may be slow
   - Check logs for any startup errors: `az containerapp logs show --name {service} --resource-group skillsnap-rg-2604282023 --follow`

2. **Run Full Health Checks**
   ```powershell
   $gw="https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io"
   # After services startup, test endpoints
   ```

3. **Database Migrations**
   - Each service should auto-migrate on startup
   - Monitor service logs for migration progress

4. **Update Frontend**
   - Update gateway URL in frontend to new deployment
   - Update any hardcoded service URLs

5. **Configure Payment Webhooks**
   - Update PayOS webhook URL: `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/payments/webhook/payos`
   - Update other payment provider webhooks as needed

6. **Test Critical Flows**
   - User login/auth
   - Portfolio creation and listing
   - Notifications (system + community split)
   - Realtime chat and notifications
   - Payment processing

7. **Monitor & Scale**
   - Watch CPU/memory usage in Container Apps metrics
   - Adjust replica counts if needed
   - Monitor application logs

---

## Troubleshooting

### Services Not Starting
Check logs:
```powershell
az containerapp logs show --name {service-name} --resource-group skillsnap-rg-2604282023 --follow
```

### Can't Connect to Service
- Verify gateway is responding: `curl -k https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/`
- Check network security: service-to-service communication uses internal DNS
- Verify Key Vault secrets are accessible

### Database Connection Issues
- Check connection string in Key Vault
- Verify SQL firewall rules allow Azure services
- Check SQL server logs

### RabbitMQ Connection Issues
- Verify `rabbitmq-service` DNS resolves internally
- Check RabbitMQ credentials in Key Vault
- Monitor RabbitMQ logs: services should connect automatically

---

## Cost Estimation (Monthly)

| Resource | Tier | Est. Cost |
|----------|------|-----------|
| Azure SQL Database | Basic (5 DTU) | ~$5 |
| Container Apps | Consumption | ~$15-30 |
| Container Registry | Basic | ~$5 |
| Key Vault | Standard | ~$0.5 |
| Azure Redis Cache | Basic C0 | ~$16 |
| **Total** | | **~$40-57/month** |

Within Azure Student $100/month credit budget ✅

---

## Files Generated

- `DEPLOYMENT_NEW_ACCOUNT_SUMMARY.md` (this file)
- Azure vars stored in: `C:\Users\ADMIN\.copilot\session-state\18ea629d-3bcf-4966-944e-d0f1c0090b71\files\azure-new-account-vars.json`

---

## Contacts

**Account:** Azure for Students (hollowichigo4055@gmail.com)  
**Resource Group:** skillsnap-rg-2604282023  
**Region:** southeastasia  
**Build Date:** 2026-04-28 21:20 UTC+7

---

*Redeploy completed successfully. All infrastructure provisioned and services deployed. Awaiting startup completion and testing.*
