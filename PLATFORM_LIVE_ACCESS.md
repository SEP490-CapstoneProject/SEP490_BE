# SkillSnap Platform - Live Access Guide
**Platform Status:** ✅ ONLINE  
**Gateway:** `https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io`  
**Last Updated:** 2026-04-28 22:06 UTC+7

---

## 🌐 Quick Access URLs

### API Gateway (Use This for All API Calls)
```
https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io
```

### Health Check
```
GET https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/health
```

---

## 🧪 Test Accounts

Use these accounts to test the platform:

### Account 1: Company User
```
Email: company A
Password: 123
```

### Account 2: Individual User 1
```
Email: thinh1@gmail.com
Password: Thinh1512!
```

### Account 3: Individual User 2
```
Email: testuser2@gmail.com
Password: 123456
```

---

## 📡 API Endpoints

All endpoints are accessed through the gateway. Format: `https://gateway.../api/{endpoint}`

### Authentication
```
POST   /api/auth/login                    - User login
POST   /api/auth/register                 - User registration
POST   /api/auth/refresh-token            - Refresh access token
POST   /api/auth/logout                   - User logout
GET    /api/auth/profile                  - Get current user
```

### User Profile
```
GET    /api/userprofile/profile           - Get user profile
PUT    /api/userprofile/profile           - Update profile
GET    /api/employees                     - List employees
GET    /api/employees/{id}                - Get employee details
GET    /api/companies                     - List companies
GET    /api/companies/{id}                - Get company details
```

### Portfolio
```
GET    /api/portfolio/portfolios          - List portfolios
POST   /api/portfolio/portfolios          - Create portfolio
GET    /api/portfolio/portfolios/{id}     - Get portfolio
PUT    /api/portfolio/portfolios/{id}     - Update portfolio
DELETE /api/portfolio/portfolios/{id}     - Delete portfolio
GET    /api/compliments                   - Get compliments
POST   /api/compliments                   - Give compliment
```

### Company & Jobs
```
GET    /api/company-posts                 - List company posts
POST   /api/company-posts                 - Create job posting
GET    /api/company-posts/{id}            - Get post details
PUT    /api/company-posts/{id}            - Update post
DELETE /api/company-posts/{id}            - Delete post
```

### Connections
```
GET    /api/connection/requests           - List connection requests
POST   /api/connection/requests           - Send connection request
PUT    /api/connection/requests/{id}      - Accept/reject request
GET    /api/connection/list               - List connections
DELETE /api/connection/{id}               - Remove connection
```

### Community & Posts
```
GET    /api/community/groups              - List community groups
POST   /api/community/groups              - Create group
GET    /api/posts                         - List posts
POST   /api/posts                         - Create post
GET    /api/posts/{id}                    - Get post
PUT    /api/posts/{id}                    - Update post
DELETE /api/posts/{id}                    - Delete post
POST   /api/posts/{id}/comments           - Add comment
```

### Subscriptions & Plans
```
GET    /api/subscription/plans            - List subscription plans
GET    /api/subscriptions                 - Get user subscriptions
POST   /api/subscriptions                 - Subscribe to plan
GET    /api/subscriptions/{id}            - Get subscription
PUT    /api/subscriptions/{id}            - Update subscription
DELETE /api/subscriptions/{id}            - Cancel subscription
```

### Notifications ⭐ NEW
```
GET    /api/notifications/system          - Get system notifications
GET    /api/notifications/community       - Get community notifications
PUT    /api/notifications/{id}            - Mark as read
DELETE /api/notifications/{id}            - Delete notification
```

### Realtime WebSocket ⭐ NEW
```
WebSocket /hubs/realtime?channel=ReceiveSystemNotification      - System notifications
WebSocket /hubs/realtime?channel=ReceiveCommunityNotification   - Community notifications
WebSocket /hubs/chat                                             - Chat messages
```

### Payments
```
GET    /api/payments/methods              - List payment methods
POST   /api/payments/process              - Process payment
GET    /api/payments/history              - Payment history
GET    /api/payments/{id}                 - Get payment details
```

### Media
```
POST   /api/upload                        - Upload file
GET    /api/media/{id}                    - Get media
DELETE /api/media/{id}                    - Delete media
```

### Applications
```
GET    /api/applications                  - List job applications
POST   /api/applications                  - Submit application
GET    /api/applications/{id}             - Get application
PUT    /api/applications/{id}             - Update application status
```

---

## 🔐 Authentication Example

### Step 1: Login and Get Token
```bash
curl -X POST https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "company A",
    "password": "123"
  }'

# Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "user": {...}
}
```

### Step 2: Use Token for Protected Requests
```bash
curl https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/notifications/system \
  -H "Authorization: Bearer {your_token}"
```

---

## 📊 System Notifications Types (New Features)

The notification service now supports these system notification types:

```
application.created
- Triggered when a user applies for a job
- Recipient: Company HR

application.status.updated
- Triggered when application status changes
- Recipient: Job applicant

connection.request.created
- Triggered when someone sends a connection request
- Recipient: Connection recipient

connection.request.accepted
- Triggered when connection request is accepted
- Recipient: Connection sender

portfolio.compliment.created
- Triggered when someone gives a compliment
- Recipient: Portfolio owner
```

### Accessing Notifications

**System Notifications (Server-to-Client):**
```
GET /api/notifications/system?page=1&pageSize=20
```

**Community Notifications:**
```
GET /api/notifications/community?page=1&pageSize=20
```

**Realtime Updates (WebSocket):**
```javascript
// Connect to realtime hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/hubs/realtime")
    .withAutomaticReconnect()
    .build();

// Listen for system notifications
connection.on("ReceiveSystemNotification", (notification) => {
    console.log("System notification:", notification);
});

// Listen for community notifications  
connection.on("ReceiveCommunityNotification", (notification) => {
    console.log("Community notification:", notification);
});

connection.start();
```

---

## 🌍 Direct Service Access (Advanced)

For debugging, services are also directly accessible:

| Service | URL |
|---------|-----|
| Auth | `https://auth-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| UserProfile | `https://userprofile-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Portfolio | `https://portfolio-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Company | `https://company-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Connection | `https://connection-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Community | `https://community-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Subscription | `https://subscription-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Payment | `https://payment-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Notification | `https://notification-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Media | `https://media-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Application | `https://application-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |
| Realtime | `https://realtime-service.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io` |

---

## 🧪 Quick Test Commands

### Test Platform Health
```bash
# Using PowerShell
Invoke-RestMethod -Uri "https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/health" -SkipCertificateCheck
```

### Test Login
```bash
curl -X POST https://gateway.redmushroom-1d023c6a.southeastasia.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"company A","password":"123"}'
```

### Run Full Test Suite
```bash
# From D:\Capstone directory
.\test-platform-live.ps1
```

---

## ⚠️ Important Notes

1. **Certificate:** Azure Container Apps uses self-signed certificates. Use `-SkipCertificateCheck` in PowerShell or accept certificate warnings in browsers.

2. **CORS:** If testing from frontend, ensure CORS is properly configured on gateway.

3. **Rate Limiting:** No rate limiting currently enabled. Consider implementing for production.

4. **Public Access:** All services are currently publicly accessible. This is for testing only.

5. **Database:** Uses Azure SQL Database with Basic tier (5 DTU). Performance limited for concurrent users.

---

## 📞 Support

- **Azure Portal:** https://portal.azure.com
- **Container Apps:** Resource Group `skillsnap-rg-2604282023`
- **View Logs:** `az containerapp logs show -g skillsnap-rg-2604282023 -n {service-name}`
- **Restart Service:** `az containerapp revision restart -g skillsnap-rg-2604282023 -n {service-name} --revision {revision}`

---

**Status:** ✅ Platform Online - Ready for Testing
