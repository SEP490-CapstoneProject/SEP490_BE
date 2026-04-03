# Deployed Services & Swagger URLs

**Last Updated**: 2026-04-03  
**Environment**: Azure Container Apps (Southeast Asia)  
**Base Domain**: `grayforest-11aba44e.southeastasia.azurecontainerapps.io`

---

## All Services

| Service | Base URL | Swagger URL | Status |
| --- | --- | --- | --- |
| api-gateway | https://api-gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://api-gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| auth-service | https://auth-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://auth-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| userprofile-service | https://userprofile-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://userprofile-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| portfolio-service | https://portfolio-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://portfolio-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| company-service | https://company-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://company-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| connection-service | https://connection-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://connection-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| community-service | https://community-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://community-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| subscription-service | https://subscription-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://subscription-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| notification-service | https://notification-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://notification-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| realtime-service | https://realtime-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://realtime-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| media-service | https://media-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://media-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |
| application-service | https://application-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io | https://application-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/swagger | ✅ Running |

---

## Special Endpoints

### API Gateway Routes
| Route | Target Service | Path |
| --- | --- | --- |
| Auth | auth-service | `/api/auth/*` |
| UserProfile | userprofile-service | `/api/userprofile/*`, `/api/employees/*`, `/api/companies/*` |
| Portfolio | portfolio-service | `/api/portfolio/*`, `/api/compliments/*` |
| Company | company-service | `/api/company-posts/*` |
| Connection | connection-service | `/api/connection/*` |
| Community | community-service | `/api/community/*`, `/api/posts/*` |
| Subscription | subscription-service | `/api/subscription/*`, `/api/subscriptions/*`, `/api/plans/*` |
| Notification | notification-service | `/api/notifications/*` |
| Media | media-service | `/api/media/*`, `/api/upload/*` |
| Application | application-service | `/api/applications/*` |
| **Realtime Hub** | realtime-service | `/hubs/realtime`, `/api/realtime/*` |

### SignalR Realtime Hub
| Endpoint | URL | Auth Required |
| --- | --- | --- |
| Via Gateway | `https://api-gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io/hubs/realtime` | ✅ Yes (JWT) |
| Direct | `https://realtime-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/hubs/realtime` | ✅ Yes (JWT) |

**SignalR Events**:
- `NewComment` - New comment on post
- `NewReply` - New reply to comment
- `NewNotification` - User notification

**Connection Example**:
```typescript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://api-gateway.../hubs/realtime", {
    accessTokenFactory: () => jwtToken
  })
  .withAutomaticReconnect()
  .build();
```

### Health Check Endpoints
| Service | Health URL |
| --- | --- |
| API Gateway | https://api-gateway.grayforest-11aba44e.southeastasia.azurecontainerapps.io/health |
| Realtime | https://realtime-service.grayforest-11aba44e.southeastasia.azurecontainerapps.io/api/realtime/health |

---

## CORS Configuration

**Allowed Origins**:
- `https://sep-490-web-fork.vercel.app` (Production)
- `http://localhost:3000` (Development)
- `http://localhost:5173` (Vite Development)

**All services configured with**:
- `AllowAnyMethod()`
- `AllowAnyHeader()`
- `AllowCredentials()` ✅

---

## Recent Deployments

| Date | Service | Tag | Changes |
| --- | --- | --- | --- |
| 2026-04-03 | api-gateway | 20260403195644 | Fixed 504 timeout, added realtime routes, updated all cluster FQDNs |
| 2026-04-03 | community-service | 20260403193026 | Fixed 500 error on /posts/user/{id}, added DTO response |
| 2026-04-03 | All services | 20260403191321 | CORS fix - AllowCredentials enabled |
| 2026-04-02 | community, notification, realtime | 20260402152022 | Production config with Key Vault |

---

## Notes

- All services use Azure Key Vault for secrets
- API Gateway uses YARP reverse proxy with public FQDNs
- SignalR requires JWT token (hub has `[Authorize]` attribute)
- Payment service exists in code but not deployed to Azure yet

